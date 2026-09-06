using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TestAPI.Models;

namespace TestAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EventsController> _logger;

        public EventsController(AppDbContext context, ILogger<EventsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Batch ingest endpoint called by Android SDK WorkManager.
        /// </summary>
        [HttpPost("batch")]
        public async Task<IActionResult> IngestBatch([FromBody] BatchEventRequestDto request)
        {
            try
            {
                if (request == null || request.Events == null || request.Events.Count == 0)
                {
                    return BadRequest(new { success = false, message = "No events provided in batch request." });
                }

                var projectId = string.IsNullOrWhiteSpace(request.ProjectId) ? "default_project" : request.ProjectId;
                var sessionId = string.IsNullOrWhiteSpace(request.SessionId) ? Guid.NewGuid().ToString() : request.SessionId;

                var hasCrash = request.Events.Any(e => e != null && string.Equals(e.Type, "Crash", StringComparison.OrdinalIgnoreCase));

                // 1. Session tracking
                var session = await _context.SdkSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
                if (session == null)
                {
                    session = new SdkSession
                    {
                        SessionId = sessionId,
                        ProjectId = projectId,
                        UserId = request.UserId,
                        DeviceModel = request.DeviceModel,
                        AppVersion = request.AppVersion,
                        StartTimeUtc = DateTime.UtcNow,
                        LastActivityUtc = DateTime.UtcNow,
                        EventCount = request.Events.Count,
                        HasCrash = hasCrash
                    };
                    _context.SdkSessions.Add(session);
                }
                else
                {
                    session.LastActivityUtc = DateTime.UtcNow;
                    session.EventCount += request.Events.Count;
                    if (hasCrash)
                    {
                        session.HasCrash = true;
                    }
                    if (!string.IsNullOrEmpty(request.UserId)) session.UserId = request.UserId;
                }

                // 2. Process each event
                var nowUtc = DateTime.UtcNow;
                foreach (var evt in request.Events)
                {
                    if (evt == null) continue;

                    var eventId = string.IsNullOrWhiteSpace(evt.Id) ? Guid.NewGuid().ToString() : evt.Id;
                    var eventType = string.IsNullOrWhiteSpace(evt.Type) ? "Log" : evt.Type;
                    var timestamp = evt.Timestamp > 0 ? evt.Timestamp : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    var payloadJson = JsonSerializer.Serialize(evt);
                    var deviceInfoJson = evt.DeviceInfo != null ? JsonSerializer.Serialize(evt.DeviceInfo) : null;

                    // Save raw event
                    var sdkEvent = new SdkEvent
                    {
                        Id = eventId,
                        ProjectId = projectId,
                        SessionId = sessionId,
                        Type = eventType,
                        Timestamp = timestamp,
                        Payload = payloadJson,
                        DeviceInfo = deviceInfoJson,
                        UserId = request.UserId,
                        CreatedUtc = nowUtc
                    };
                    _context.SdkEvents.Add(sdkEvent);

                    // Specialized indexing for fast queries
                    if (string.Equals(eventType, "Crash", StringComparison.OrdinalIgnoreCase))
                    {
                        var crash = new SdkCrash
                        {
                            Id = Guid.NewGuid().ToString(),
                            ProjectId = projectId,
                            SessionId = sessionId,
                            ExceptionName = evt.Exception ?? "UnknownException",
                            ExceptionMessage = evt.Message ?? evt.Error ?? "No message provided",
                            StackTrace = evt.StackTrace ?? "No stacktrace available",
                            BreadcrumbsJson = evt.Breadcrumbs != null ? JsonSerializer.Serialize(evt.Breadcrumbs) : "[]",
                            DeviceInfoJson = deviceInfoJson ?? "{}",
                            Timestamp = timestamp,
                            CreatedUtc = nowUtc
                        };
                        _context.SdkCrashes.Add(crash);
                    }
                    else if (string.Equals(eventType, "Log", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(eventType, "Breadcrumb", StringComparison.OrdinalIgnoreCase))
                    {
                        var log = new SdkLog
                        {
                            Id = Guid.NewGuid().ToString(),
                            ProjectId = projectId,
                            SessionId = sessionId,
                            Level = string.IsNullOrWhiteSpace(evt.Level) ? "INFO" : evt.Level.ToUpper(),
                            Message = evt.Message ?? "Empty log message",
                            Tag = evt.Type,
                            Timestamp = timestamp,
                            CreatedUtc = nowUtc
                        };
                        _context.SdkLogs.Add(log);
                    }
                    else if (string.Equals(eventType, "Network", StringComparison.OrdinalIgnoreCase))
                    {
                        var net = new SdkNetwork
                        {
                            Id = Guid.NewGuid().ToString(),
                            ProjectId = projectId,
                            SessionId = sessionId,
                            Method = string.IsNullOrWhiteSpace(evt.Method) ? "GET" : evt.Method.ToUpper(),
                            Url = evt.Url ?? "N/A",
                            StatusCode = evt.StatusCode,
                            DurationMs = evt.DurationMs ?? 0,
                            ErrorMessage = evt.Error,
                            Timestamp = timestamp,
                            CreatedUtc = nowUtc
                        };
                        _context.SdkNetworks.Add(net);
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Processed batch of {Count} events for session {SessionId}", request.Events.Count, sessionId);

                return Ok(new
                {
                    success = true,
                    processedCount = request.Events.Count,
                    sessionId = sessionId,
                    timestamp = nowUtc
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing batch events ingestion.");
                return StatusCode(500, new { success = false, message = "Error processing batch events.", details = ex.Message });
            }
        }

        /// <summary>
        /// Fetch paginated events filtered by type, project, session ID, or level.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetEvents(
            [FromQuery] string? type,
            [FromQuery] string? projectId,
            [FromQuery] string? sessionId,
            [FromQuery] string? level,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var query = _context.SdkEvents.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(type))
                {
                    var typeLower = type.ToLower();
                    query = query.Where(e => e.Type != null && e.Type.ToLower() == typeLower);
                }

                if (!string.IsNullOrWhiteSpace(projectId))
                {
                    query = query.Where(e => e.ProjectId == projectId);
                }

                if (!string.IsNullOrWhiteSpace(sessionId))
                {
                    query = query.Where(e => e.SessionId == sessionId);
                }

                var totalCount = await query.CountAsync();
                var events = await query
                    .OrderByDescending(e => e.CreatedUtc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new
                {
                    totalCount,
                    page,
                    pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    items = events
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting events.");
                return StatusCode(500, new { success = false, message = "Error fetching events.", details = ex.Message });
            }
        }

        /// <summary>
        /// Dashboard analytics summary endpoint.
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetAnalyticsSummary()
        {
            try
            {
                var totalEvents = await _context.SdkEvents.CountAsync();
                var totalSessions = await _context.SdkSessions.CountAsync();
                var totalCrashes = await _context.SdkCrashes.CountAsync();
                var totalLogs = await _context.SdkLogs.CountAsync();
                var totalNetworkRequests = await _context.SdkNetworks.CountAsync();
                var networkErrors = await _context.SdkNetworks.CountAsync(n => n.StatusCode == null || n.StatusCode >= 400);

                double avgLatency = 0;
                if (totalNetworkRequests > 0)
                {
                    avgLatency = await _context.SdkNetworks.AverageAsync(n => n.DurationMs);
                }

                var logLevelList = await _context.SdkLogs
                    .GroupBy(l => l.Level)
                    .Select(g => new { Level = g.Key, Count = g.Count() })
                    .ToListAsync();

                var logLevelBreakdown = logLevelList
                    .ToDictionary(x => string.IsNullOrWhiteSpace(x.Level) ? "UNKNOWN" : x.Level, x => x.Count);

                var eventTypeList = await _context.SdkEvents
                    .GroupBy(e => e.Type)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToListAsync();

                var eventTypeBreakdown = eventTypeList
                    .ToDictionary(x => string.IsNullOrWhiteSpace(x.Type) ? "Unknown" : x.Type, x => x.Count);

                var recentCrashes = await _context.SdkCrashes
                    .AsNoTracking()
                    .OrderByDescending(c => c.CreatedUtc)
                    .Take(5)
                    .ToListAsync();

                return Ok(new AnalyticsSummaryDto
                {
                    TotalEvents = totalEvents,
                    TotalSessions = totalSessions,
                    TotalCrashes = totalCrashes,
                    TotalLogs = totalLogs,
                    TotalNetworkRequests = totalNetworkRequests,
                    NetworkErrors = networkErrors,
                    AverageLatencyMs = Math.Round(avgLatency, 2),
                    LogLevelBreakdown = logLevelBreakdown,
                    EventTypeBreakdown = eventTypeBreakdown,
                    RecentCrashes = recentCrashes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating analytics summary.");
                return StatusCode(500, new { success = false, message = "Error generating analytics summary.", details = ex.Message });
            }
        }

        /// <summary>
        /// Fetch detailed crash reports.
        /// </summary>
        [HttpGet("crashes")]
        public async Task<IActionResult> GetCrashes([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var totalCount = await _context.SdkCrashes.CountAsync();
                var crashes = await _context.SdkCrashes
                    .AsNoTracking()
                    .OrderByDescending(c => c.CreatedUtc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new { totalCount, page, pageSize, items = crashes });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching crashes.");
                return StatusCode(500, new { success = false, message = "Error fetching crashes.", details = ex.Message });
            }
        }

        /// <summary>
        /// Fetch network activity logs.
        /// </summary>
        [HttpGet("network")]
        public async Task<IActionResult> GetNetworkLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var totalCount = await _context.SdkNetworks.CountAsync();
                var networks = await _context.SdkNetworks
                    .AsNoTracking()
                    .OrderByDescending(n => n.CreatedUtc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new { totalCount, page, pageSize, items = networks });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching network logs.");
                return StatusCode(500, new { success = false, message = "Error fetching network logs.", details = ex.Message });
            }
        }

        /// <summary>
        /// Fetch sessions list.
        /// </summary>
        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var totalCount = await _context.SdkSessions.CountAsync();
                var sessions = await _context.SdkSessions
                    .AsNoTracking()
                    .OrderByDescending(s => s.LastActivityUtc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new { totalCount, page, pageSize, items = sessions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching sessions.");
                return StatusCode(500, new { success = false, message = "Error fetching sessions.", details = ex.Message });
            }
        }

        /// <summary>
        /// Test endpoint to clear SDK events for demo/reset purposes.
        /// </summary>
        [HttpPost("clear")]
        public async Task<IActionResult> ClearAllEvents()
        {
            try
            {
                _context.SdkEvents.RemoveRange(_context.SdkEvents);
                _context.SdkCrashes.RemoveRange(_context.SdkCrashes);
                _context.SdkLogs.RemoveRange(_context.SdkLogs);
                _context.SdkNetworks.RemoveRange(_context.SdkNetworks);
                _context.SdkSessions.RemoveRange(_context.SdkSessions);

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "All Debug SDK events cleared successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing SDK events.");
                return StatusCode(500, new { success = false, message = "Error clearing events.", details = ex.Message });
            }
        }
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TestAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<LogsController> _logger;

        public LogsController(AppDbContext context, ILogger<LogsController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        [HttpPost("upload")]
        public async Task<IActionResult> UploadLog([FromForm] LogUploadModel model)
        {
            if (model.File == null || model.File.Length == 0)
            {
                _logger.LogWarning("Upload attempt with empty file");
                return BadRequest("No file uploaded or file is empty.");
            }

            if (model.File.Length > 5 * 1024 * 1024)
            {
                _logger.LogWarning($"File too large: {model.File.Length} bytes");
                return BadRequest("File size exceeds limit (5MB).");
            }

            try
            {
                using var reader = new StreamReader(model.File.OpenReadStream());
                string content = await reader.ReadToEndAsync();

                var logEntry = new LogEntry
                {
                    FileName = model.File.FileName,
                    Content = content,
                    CreatedAt = DateTime.UtcNow,
                    DeviceInfo = ExtractDeviceInfo(content),
                    ExceptionType = ExtractExceptionType(content),
                    FileSize = model.File.Length // ✅ Don't forget this
                };

                await _context.LogEntries.AddAsync(logEntry);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Log saved: {model.File.FileName}, Size: {content.Length} chars");

                return Ok(new
                {
                    message = "Log saved successfully.",
                    logId = logEntry.Id,
                    timestamp = logEntry.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading log file");
                return StatusCode(500, new
                {
                    error = "Server error processing log file",
                    details = ex.Message
                });
            }
        }




        [HttpGet("all")]
        public async Task<IActionResult> GetAllLogs()
        {
            try
            {
                var logs = await _context.LogEntries // <- FIXED
                    .AsNoTracking()
                    .OrderByDescending(l => l.CreatedAt)
                    .Select(l => new LoginEntryViewModel // <- FIXED
                    {
                        Id = l.Id,
                        FileName = l.FileName,
                        CreatedAt = l.CreatedAt,
                        ExceptionType = l.ExceptionType ?? "Unknown",
                        DeviceInfo = l.DeviceInfo ?? "Unknown device",
                        FileSize = l.FileSize,
                        ContentPreview = string.IsNullOrEmpty(l.Content)
                            ? "[Empty content]"
                            : l.Content.Length > 100
                                ? l.Content.Substring(0, 100) + "..."
                                : l.Content
                    })
                    .ToListAsync();

                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all logs from database.");
                return StatusCode(500, new
                {
                    error = "Failed to retrieve logs",
                    details = ex.Message
                });
            }
        }


        private string ExtractDeviceInfo(string content)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content)) return "Unknown device";

                var manufacturer = Regex.Match(content, @"Manufacturer:\s*(.+)", RegexOptions.IgnoreCase).Groups[1].Value.Trim();
                var model = Regex.Match(content, @"Model:\s*(.+)", RegexOptions.IgnoreCase).Groups[1].Value.Trim();
                var version = Regex.Match(content, @"Version:\s*(.+)", RegexOptions.IgnoreCase).Groups[1].Value.Trim();

                if (!string.IsNullOrEmpty(manufacturer) && !string.IsNullOrEmpty(model))
                {
                    return $"{manufacturer} {model}" + (string.IsNullOrEmpty(version) ? "" : $" (Android {version})");
                }

                return "Unknown device";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract device info");
                return "Unknown device";
            }
        }

        private string ExtractExceptionType(string content)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content)) return "Unknown";

                var stacktraceStart = content.IndexOf("Stacktrace:", StringComparison.OrdinalIgnoreCase);
                if (stacktraceStart < 0) return "Unknown";

                var stackLines = content.Substring(stacktraceStart)
                    .Split('\n')
                    .Select(l => l.Trim())
                    .Where(l => l.StartsWith("at ") && l.Contains("Exception"))
                    .ToList();

                foreach (var line in stackLines)
                {
                    var match = Regex.Match(line, @"(\w+\.\w+Exception)");
                    if (match.Success) return match.Groups[1].Value;
                }

                return "Unknown";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract exception type");
                return "Unknown";
            }
        }
    }

    public class LogUploadModel
    {
        [Required(ErrorMessage = "Log file is required.")]
        public IFormFile File { get; set; }
    }



}

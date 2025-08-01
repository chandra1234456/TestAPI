using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
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
            _context = context;
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadLog([FromForm] LogUploadModel model)
        {
            if (model.File == null || model.File.Length == 0)
            {
                _logger.LogWarning("Upload attempt with empty file");
                return BadRequest("No file uploaded or file is empty.");
            }

            // Validate file size (e.g., 5MB max)
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
                    DeviceInfo = ExtractDeviceInfo(content), // Optional: parse device info
                    ExceptionType = ExtractExceptionType(content) // Optional: parse exception
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

        // Optional helper methods to parse crash log
        private string ExtractDeviceInfo(string content)
        {
            try
            {
                var manufacturerMatch = Regex.Match(content, @"Manufacturer:\s*(.+)");
                var modelMatch = Regex.Match(content, @"Model:\s*(.+)");
                var versionMatch = Regex.Match(content, @"Version:\s*(.+)");

                return $"{manufacturerMatch.Groups[1].Value} {modelMatch.Groups[1].Value} (Android {versionMatch.Groups[1].Value})";
            }
            catch
            {
                return "Unknown device";
            }
        }

        private string ExtractExceptionType(string content)
        {
            try
            {
                var stacktraceStart = content.IndexOf("Stacktrace:");
                if (stacktraceStart > 0)
                {
                    var firstLine = content.Substring(stacktraceStart)
                        .Split('\n')
                        .FirstOrDefault(line => line.Trim().StartsWith("at ") && line.Contains("Exception"));

                    if (firstLine != null)
                    {
                        var exceptionMatch = Regex.Match(firstLine, @"(\w+\.\w+Exception)");
                        if (exceptionMatch.Success)
                        {
                            return exceptionMatch.Groups[1].Value;
                        }
                    }
                }
                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }

    public class LogUploadModel
    {
        [Required]
        public IFormFile File { get; set; }
    }
}

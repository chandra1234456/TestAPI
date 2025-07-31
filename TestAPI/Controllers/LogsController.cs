using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.IO;
using System;

namespace TestAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LogsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadLog([FromForm] LogUploadModel model)
        {
            try
            {
                if (model.File == null || model.File.Length == 0)
                    return BadRequest("No file uploaded.");

                using var reader = new StreamReader(model.File.OpenReadStream());
                string content = await reader.ReadToEndAsync();

                var logEntry = new LogEntry
                {
                    FileName = model.File.FileName,
                    Content = content,
                    CreatedAt = DateTime.UtcNow
                };

                _context.LogEntries.Add(logEntry);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Log file saved: {model.File.FileName}");

                return Ok(new { message = "Log saved successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine("UploadLog error: " + ex.Message);
                return StatusCode(500, "Server error: " + ex.Message);
            }
        }
    }

    public class LogUploadModel
    {
        public IFormFile File { get; set; }
    }
}

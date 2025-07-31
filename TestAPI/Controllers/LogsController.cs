using Microsoft.AspNetCore.Mvc;
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
            if (model.File == null || model.File.Length == 0)
                return BadRequest("No file uploaded.");

            string content;
            using (var reader = new StreamReader(model.File.OpenReadStream()))
            {
                content = await reader.ReadToEndAsync();
            }

            var logEntry = new LogEntry
            {
                FileName = model.File.FileName,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _context.LogEntries.Add(logEntry);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Log saved successfully." });
        }
    }

    public class LogUploadModel
    {
        public Microsoft.AspNetCore.Http.IFormFile File { get; set; }
    }
}

using Microsoft.AspNetCore.Mvc;

namespace TestAPI.Controllers
{
    [ApiController]
    [Route("[controller]/api")]
    public class HomeController : ControllerBase
    {
        [HttpGet("ok")]
        public IActionResult OkResponse()
        {
            return Ok("Request processed successfully.");
        }

        [HttpPost("created")]
        public IActionResult CreatedResponse()
        {
            return Created("/home/api/created", new { Message = "Resource created successfully." });
        }

        [HttpDelete("nocontent")]
        public IActionResult NoContentResponse()
        {
            return NoContent(); // 204 No Content
        }

        [HttpGet("badrequest")]
        public IActionResult BadRequestResponse()
        {
            return BadRequest("Bad request error message.");
        }

        [HttpGet("unauthorized")]
        public IActionResult UnauthorizedResponse()
        {
            return Unauthorized("Unauthorized access.");
        }

        [HttpGet("forbidden")]
        public IActionResult ForbiddenResponse()
        {
            return Forbid(); // 403 Forbidden
        }

        [HttpGet("notfound")]
        public IActionResult NotFoundResponse()
        {
            return NotFound("The requested resource was not found.");
        }

        [HttpPost("conflict")]
        public IActionResult ConflictResponse()
        {
            return Conflict("Conflict occurred while processing the request.");
        }

        [HttpGet("servererror")]
        public IActionResult InternalServerErrorResponse()
        {
            return StatusCode(500, "Internal Server Error.");
        }

        [HttpGet("unavailable")]
        public IActionResult ServiceUnavailableResponse()
        {
            return StatusCode(503, "Service is currently unavailable. Try again later.");
        }


        [HttpGet("jsonsample")]
        public IActionResult GetHardcodedJson()
        {
            var data = new
            {
                Id = 1,
                Name = "Sample Item",
                Description = "This is a hardcoded JSON object returned by the API.",
                Tags = new[] { "sample", "json", "demo" },
                CreatedAt = DateTime.UtcNow
            };

            return Ok(data);
        }
        [HttpGet("image")]
        public IActionResult GetImageJson()
        {
            var imageData = new
            {
                Id = 101,
                Title = "Sample Image",
                Description = "This is a sample image with a public URL.",
                ImageUrl = "https://www.google.com/url?sa=i&url=https%3A%2F%2Fwww.pexels.com%2Fsearch%2Fbeautiful%2F&psig=AOvVaw0UutIxF_ZppS5ZDgaSwtPT&ust=1749468418867000&source=images&cd=vfe&opi=89978449&ved=0CBEQjRxqFwoTCOi6ssfb4Y0DFQAAAAAdAAAAABAE"
            };

            return Ok(imageData);
        }
        [HttpGet("test")]
        public IActionResult TestResponse()
        {
            var response = new
            {
                status = "success",
                message = "Connected to backend",
                appVersion = "1.0.3",
                serverTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC"
            };

            return Ok(response);
        }
        [HttpGet("download-apk")]
        public IActionResult DownloadApk([FromQuery] string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return BadRequest("Version required");

            var apkPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "APK", $"app_{version}.apk");

            if (!System.IO.File.Exists(apkPath))
                return NotFound("APK not found");

            var bytes = System.IO.File.ReadAllBytes(apkPath);
            return File(bytes, "application/vnd.android.package-archive", $"app_{version}.apk");
        }




    }

}


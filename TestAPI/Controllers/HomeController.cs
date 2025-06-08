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

    }

}


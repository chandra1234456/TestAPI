namespace TestAPI.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    namespace YourAppNamespace.Controllers
    {
        [ApiController]
        [Route("[controller]")]
        public class QuotesController : ControllerBase
        {
            private static readonly List<Quote> Quotes = GenerateDummyQuotes();

            [HttpGet]
            public IActionResult GetPaginatedQuotes(int page = 1, int limit = 20)
            {
                if (page < 1 || limit < 1)
                    return BadRequest("Page number and limit must be greater than 0.");

                var skip = (page - 1) * limit;
                var pagedQuotes = Quotes.Skip(skip).Take(limit).ToList();
                var totalCount = Quotes.Count;

                var response = new QuoteResponse
                {
                    Count = pagedQuotes.Count,
                    TotalCount = totalCount,
                    Page = page,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)limit),
                    LastItemIndex = skip + pagedQuotes.Count,
                    Results = pagedQuotes
                };

                return Ok(response);
            }

            private static List<Quote> GenerateDummyQuotes()
            {
                return new List<Quote>
            {
                new Quote
                {
                    Id = "An5NAXPrbN",
                    Author = "Thomas Edison",
                    Content = "Hell, there are no rules here-- we're trying to accomplish something.",
                    AuthorSlug = "thomas-edison",
                    Length = 69,
                    DateAdded = "2023-04-14",
                    DateModified = "2023-04-14",
                    Tags = new List<string>()
                },
                new Quote
                {
                    Id = "bfNpGC2NI",
                    Author = "Thomas Edison",
                    Content = "As a cure for worrying, work is better than whisky.",
                    AuthorSlug = "thomas-edison",
                    Length = 51,
                    DateAdded = "2023-04-14",
                    DateModified = "2023-04-14",
                    Tags = new List<string> { "Humorous" }
                },
                // ➕ Add the rest of your quotes here
            };
            }
        }
    }

}

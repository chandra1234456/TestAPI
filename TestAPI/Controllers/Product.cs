namespace TestAPI.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("[controller]")]
    public class ProductsController : ControllerBase
    {
        private static readonly List<Product> Products = GenerateDummyProducts();

        [HttpGet("paginated")]
        public IActionResult GetPaginatedProducts(int pageNumber = 1, int pageSize = 10)
        {
            if (pageNumber < 1 || pageSize < 1)
            {
                return BadRequest("Page number and page size must be greater than 0.");
            }

            var pagedData = Products
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var totalItems = Products.Count;

            var response = new
            {
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                Items = pagedData
            };

            return Ok(response);
        }

        private static List<Product> GenerateDummyProducts()
        {
            var items = new List<Product>();
            for (int i = 1; i <= 50; i++)
            {
                items.Add(new Product
                {
                    Id = i,
                    Name = $"Product {i}"
                });
            }
            return items;
        }
    }

}

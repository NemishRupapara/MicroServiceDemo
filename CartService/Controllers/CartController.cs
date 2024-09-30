using CartService.Data;
using CartService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace CartService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly CartDbContext _context;
        private readonly HttpClient _httpClient;

        public CartController(CartDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClient = httpClientFactory.CreateClient("ProductService");
        }

        [HttpPost("{productId}")]
        public async Task<IActionResult> AddToCart(int productId)
        {
            var response = await _httpClient.GetStringAsync($"api/product/{productId}");
            var product = JsonConvert.DeserializeObject<Product>(response);
            var addProduct = new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Price = product.Price,
                Quantity = 1,
            };
            _context.CartItems.Add(addProduct);
            await _context.SaveChangesAsync();

            return Ok(await _context.CartItems.ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var cartItems = await _context.CartItems.ToListAsync();
            return Ok(cartItems);
        }
    }
}

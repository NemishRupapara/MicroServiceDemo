using ProductService.Data;
using ProductService.Models;

namespace ProductService.Services
{
    public class ProductServiceClass
    {
        private readonly ProductDbContext _context;

        public ProductServiceClass(ProductDbContext context)
        {
            _context = context;
        }

        public Product GetById(int id)
        {
            return _context.Products.FirstOrDefault(x => x.Id == id);
        }
    }
}

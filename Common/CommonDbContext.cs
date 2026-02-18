using Common.Models;

using Microsoft.EntityFrameworkCore;

namespace Common
{
    public class CommonDbContext: DbContext
    {
        public DbSet<Product> Products { get; set; }
    }
}

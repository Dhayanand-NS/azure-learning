using Microsoft.EntityFrameworkCore;

namespace azure_app_dhay.Data
{
    public class AppDBContext: DbContext

    {
        public AppDBContext(DbContextOptions<AppDBContext> options) : base(options)
        {
            
        }

        public DbSet<Product> Products { get; set; }
    }
}

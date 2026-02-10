using Microsoft.EntityFrameworkCore;
using phpMVC.Models;

namespace phpMVC.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Service> Services { get; set; }
      //  public DbSet<PostJob> PostJobs { get; set; }
    }
}
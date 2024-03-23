using comp4870project.Model;
using Microsoft.EntityFrameworkCore;

namespace DockerMVC.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<SavedMessage> Messages { get; set; }
}

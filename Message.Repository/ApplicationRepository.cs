using Microsoft.EntityFrameworkCore;

namespace Message.Repository;

public class ApplicationRepository(
    DbContextOptions<ApplicationRepository> options) : DbContext(options)
{
    public DbSet<Domain.Entities.Message> Messages { get; set; }
}
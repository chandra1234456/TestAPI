using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TestAPI.Controllers;

namespace TestAPI
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Add your entities here
        // public DbSet<CrashLog> CrashLogs { get; set; }
        public DbSet<LogEntry> LogEntries { get; set; }
        public DbSet<LoginEntryViewModel> LoginEntryViewModel { get; set; }
    }
}

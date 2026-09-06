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

        // Debug SDK DbSets
        public DbSet<TestAPI.Models.SdkEvent> SdkEvents { get; set; }
        public DbSet<TestAPI.Models.SdkSession> SdkSessions { get; set; }
        public DbSet<TestAPI.Models.SdkCrash> SdkCrashes { get; set; }
        public DbSet<TestAPI.Models.SdkLog> SdkLogs { get; set; }
        public DbSet<TestAPI.Models.SdkNetwork> SdkNetworks { get; set; }
    }
}

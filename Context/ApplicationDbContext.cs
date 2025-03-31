using LinkedInEngagement.Models;
using Microsoft.EntityFrameworkCore;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace LinkedInEngagement.Context
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Define DbSets for your entities
        public DbSet<Settings> Settings { get; set; }
        public DbSet<ClientDetail> ClientDetails { get; set; }
        public DbSet<LinkedInPost> LinkedInPosts { get; set; }
        public DbSet<LinkedInPostsEngagement> LinkedInPostsEngagements { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Configure entity properties and relationships here
        }
    }
}

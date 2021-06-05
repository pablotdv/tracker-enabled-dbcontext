using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using TrackerEnabledDbContext.Common.Testing;
using TrackerEnabledDbContext.Common.Testing.Models;

namespace TrackerEnabledDbContext.Identity.IntegrationTests
{
    public class TestTrackerIdentityContext : TrackerIdentityContext<IdentityUser, string>, ITestDbContext
    {
        public TestTrackerIdentityContext([NotNull] DbContextOptions options) : base(options)
        {
        }

        public TestTrackerIdentityContext() : base()
        {
        }

        public DbSet<NormalModel> NormalModels { get; set; }
        public DbSet<ParentModel> ParentModels { get; set; }
        public DbSet<ChildModel> Children { get; set; }
        public DbSet<ModelWithCompositeKey> ModelsWithCompositeKey { get; set; }
        public DbSet<ModelWithConventionalKey> ModelsWithConventionalKey { get; set; }
        public DbSet<ModelWithSkipTracking> ModelsWithSkipTracking { get; set; }
        public DbSet<POCO> POCOs { get; set; }
        public DbSet<TrackedModelWithMultipleProperties> TrackedModelsWithMultipleProperties { get; set; }
        public DbSet<TrackedModelWithCustomTableAndColumnNames> TrackedModelsWithCustomTableAndColumnNames { get; set; }
        public DbSet<SoftDeletableModel> SoftDeletableModels { get; set; }
        public DbSet<ModelWithComplexType> ModelsWithComplexType { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ModelWithCompositeKey>().HasKey(x => new { x.Key1, x.Key2 });
            base.OnModelCreating(modelBuilder);
        }
    }
}
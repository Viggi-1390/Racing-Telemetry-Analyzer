using Microsoft.EntityFrameworkCore;
using RacingTelemetryAnalyzer.Models;

namespace RacingTelemetryAnalyzer.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Track> Tracks { get; set; }
        public DbSet<TrackSection> TrackSections { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<SessionCondition> SessionConditions { get; set; }
        public DbSet<VehicleSetup> VehicleSetups { get; set; }
        public DbSet<Lap> Laps { get; set; }
        public DbSet<TelemetryPoint> TelemetryPoints { get; set; }
        public DbSet<AnalysisResult> AnalysisResults { get; set; }
        public DbSet<SessionNote> SessionNotes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Session>()
                .HasOne(s => s.Condition)
                .WithOne(c => c.Session)
                .HasForeignKey<SessionCondition>(c => c.SessionId);

            modelBuilder.Entity<Session>()
                .HasOne(s => s.Setup)
                .WithOne(v => v.Session)
                .HasForeignKey<VehicleSetup>(v => v.SessionId);
                
            // Precision for telemetry distance and time
            modelBuilder.Entity<TelemetryPoint>()
                .Property(t => t.Timestamp)
                .HasConversion(v => v.Ticks, v => new TimeSpan(v));
                
            modelBuilder.Entity<Lap>()
                .Property(l => l.LapTime)
                .HasConversion(v => v.Ticks, v => new TimeSpan(v));
        }
    }
}

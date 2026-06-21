using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MomOi.API.Models.Health;
using MomOi.API.Models.Identity;
using MomOi.API.Models.Nutrition;
using System;

namespace MomOi.API.Data
{
    /// <summary>
    /// Database context for the MomOi application, inheriting from IdentityDbContext to support authentication.
    /// </summary>
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<MomHealthProfile> MomHealthProfiles { get; set; } = null!;
        public DbSet<BabyProfile> BabyProfiles { get; set; } = null!;
        public DbSet<EpdsAssessment> EpdsAssessments { get; set; } = null!;
        public DbSet<CycleLog> CycleLogs { get; set; } = null!;
        public DbSet<PregnancyLog> PregnancyLogs { get; set; } = null!;
        public DbSet<PostpartumLog> PostpartumLogs { get; set; } = null!;
        public DbSet<GrowthRecord> GrowthRecords { get; set; } = null!;
        public DbSet<MealLog> MealLogs { get; set; } = null!;
        public DbSet<FoodAllergyRecord> FoodAllergyRecords { get; set; } = null!;
        public DbSet<CriticalAlertLog> CriticalAlertLogs { get; set; } = null!;
        public DbSet<ExerciseLog> ExerciseLogs { get; set; } = null!;
        public DbSet<BabyFoodLog> BabyFoodLogs { get; set; } = null!;

        // --- New Entities (migrated from Node.js MongoDB) ---
        public DbSet<ChatSession> ChatSessions { get; set; } = null!;
        public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
        public DbSet<DailyMonitoringLog> DailyMonitoringLogs { get; set; } = null!;
        public DbSet<MedicationSchedule> MedicationSchedules { get; set; } = null!;
        public DbSet<MedicationAdherenceLog> MedicationAdherenceLogs { get; set; } = null!;
        public DbSet<LifestyleEntry> LifestyleEntries { get; set; } = null!;
        public DbSet<LifestyleAlert> LifestyleAlerts { get; set; } = null!;
        public DbSet<SymptomLog> SymptomLogs { get; set; } = null!;
        public DbSet<NotificationAlert> NotificationAlerts { get; set; } = null!;
        public DbSet<Recipe> Recipes { get; set; } = null!;
        public DbSet<DietPlan> DietPlans { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // PostgreSQL natively supports arrays (string[], int[]) — no ValueConverter needed.

            // Establish 1-to-1 relationship between AppUser and MomHealthProfile
            builder.Entity<MomHealthProfile>()
                .HasOne(m => m.User)
                .WithOne(u => u.HealthProfile)
                .HasForeignKey<MomHealthProfile>(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index UserId in health profile, baby profile, logs for performance
            builder.Entity<MomHealthProfile>()
                .HasIndex(m => m.UserId)
                .IsUnique();

            builder.Entity<BabyProfile>()
                .HasIndex(b => b.UserId);

            builder.Entity<PregnancyLog>()
                .HasIndex(p => p.UserId);

            builder.Entity<PostpartumLog>()
                .HasIndex(p => p.UserId);

            builder.Entity<MealLog>()
                .HasIndex(m => m.UserId);

            builder.Entity<FoodAllergyRecord>()
                .HasIndex(f => f.UserId);

            builder.Entity<CriticalAlertLog>()
                .HasIndex(c => c.UserId);

            builder.Entity<ExerciseLog>()
                .HasIndex(e => e.UserId);

            builder.Entity<BabyFoodLog>()
                .HasIndex(b => b.BabyProfileId);

            // --- Relationships for new entities ---

            // ChatSession 1-to-many ChatMessages
            builder.Entity<ChatMessage>()
                .HasOne(m => m.Session)
                .WithMany(s => s.Messages)
                .HasForeignKey(m => m.ChatSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // MedicationSchedule 1-to-many AdherenceLogs
            builder.Entity<MedicationAdherenceLog>()
                .HasOne(a => a.Schedule)
                .WithMany(s => s.AdherenceLogs)
                .HasForeignKey(a => a.MedicationScheduleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for new entities
            builder.Entity<ChatSession>().HasIndex(c => c.UserId);
            builder.Entity<DailyMonitoringLog>().HasIndex(d => d.UserId);
            builder.Entity<DailyMonitoringLog>().HasIndex(d => new { d.UserId, d.Date }).IsUnique();
            builder.Entity<MedicationSchedule>().HasIndex(m => m.UserId);
            builder.Entity<LifestyleEntry>().HasIndex(l => l.UserId);
            builder.Entity<LifestyleEntry>().HasIndex(l => new { l.UserId, l.Date }).IsUnique();
            builder.Entity<LifestyleAlert>().HasIndex(l => l.UserId);
            builder.Entity<SymptomLog>().HasIndex(s => s.UserId);
            builder.Entity<NotificationAlert>().HasIndex(n => n.UserId);
            builder.Entity<Recipe>().HasIndex(r => r.UserId);
            builder.Entity<DietPlan>().HasIndex(d => d.UserId);
        }
    }
}

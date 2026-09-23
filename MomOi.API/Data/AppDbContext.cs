using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using MomOi.API.Models;
using MomOi.API.Models.Health;
using MomOi.API.Models.Identity;
using MomOi.API.Models.Nutrition;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MomOi.API.Data
{
    /// <summary>
    /// Database context for the MomOi application, inheriting from IdentityDbContext to support authentication.
    /// </summary>
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        /// <summary>
        /// Các entity được yêu cầu XOÁ VĨNH VIỄN trong lần SaveChanges sắp tới.
        /// Dùng so sánh theo tham chiếu để không phụ thuộc vào Equals/GetHashCode của entity.
        /// </summary>
        private readonly HashSet<object> _permanentDeletions = new(ReferenceEqualityComparer.Instance);

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// Yêu cầu xoá thật một entity vốn có xoá mềm — dùng khi người dùng thực hiện
        /// quyền yêu cầu xoá dữ liệu cá nhân (Nghị định 13/2023/NĐ-CP).
        /// Gọi qua <c>IGenericRepository.RemovePermanently</c>, không gọi trực tiếp.
        /// </summary>
        internal void MarkPermanentDeletion(object entity) => _permanentDeletions.Add(entity);

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

        // --- Sprint 3: Advanced Admin ---
        public DbSet<BusinessRule> BusinessRules { get; set; } = null!;
        public DbSet<UsdaFoodItem> UsdaFoodItems { get; set; } = null!;

        // --- Phase 4 ---
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; } = null!;
        public DbSet<VaccinationRecord> VaccinationRecords { get; set; } = null!;

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // ── Bước 1: chuyển lệnh XOÁ thành lệnh CẬP NHẬT cờ, với entity có xoá mềm.
            // Phải chạy TRƯỚC vòng lặp dấu thời gian bên dưới, để bản ghi vừa đổi sang
            // trạng thái Modified cũng được cập nhật UpdatedAt.
            foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
            {
                if (entry.State != EntityState.Deleted) continue;

                // Được yêu cầu xoá thật => để nguyên lệnh DELETE đi xuống database.
                if (_permanentDeletions.Contains(entry.Entity)) continue;

                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = DateTime.UtcNow;
            }

            // ── Bước 2: dấu thời gian kiểm toán (audit fields).
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = DateTime.UtcNow;
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        break;
                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        break;
                }
            }

            var affected = await base.SaveChangesAsync(cancellationToken);

            // Danh sách xoá vĩnh viễn chỉ có hiệu lực cho đúng lần lưu này.
            _permanentDeletions.Clear();

            return affected;
        }

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

            // VaccinationRecord to BabyProfile
            builder.Entity<VaccinationRecord>()
                .HasOne(v => v.BabyProfile)
                .WithMany()
                .HasForeignKey(v => v.BabyProfileId)
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
            builder.Entity<PaymentTransaction>().HasIndex(p => p.UserId);
            // Mã đơn phải duy nhất: cổng thanh toán dùng nó làm khoá đối chiếu, và IPN
            // tra cứu theo mã này nên trùng mã là ghi nhận nhầm giao dịch.
            builder.Entity<PaymentTransaction>().HasIndex(p => p.OrderCode).IsUnique();

            // KHOÁ LẠC QUAN cho giao dịch thanh toán.
            // xmin là cột hệ thống PostgreSQL tự duy trì, đổi giá trị sau MỖI lần dòng
            // được cập nhật. EF sẽ tự thêm "AND xmin = <giá trị lúc đọc>" vào câu UPDATE;
            // nếu có tiến trình khác vừa sửa dòng này thì UPDATE khớp 0 dòng và EF ném
            // DbUpdateConcurrencyException — nhờ đó hai IPN về cùng lúc không thể cùng
            // ghi nhận một đơn hàng. Đây là cột có sẵn, KHÔNG làm đổi schema.
            // API này bị đánh dấu Obsolete ở Npgsql 9, nhưng bản thay thế
            // (.Property<uint>("xmin").IsRowVersion()) lại sinh ra migration
            // ADD COLUMN "xmin" — trong khi xmin là CỘT HỆ THỐNG mà PostgreSQL đã
            // tạo sẵn cho mọi bảng, nên migration đó sẽ lỗi khi chạy.
            // UseXminAsConcurrencyToken hiểu điều đó và không đụng vào schema.
            // => Tắt cảnh báo một cách có chủ đích, kèm lý do.
#pragma warning disable CS0618
            builder.Entity<PaymentTransaction>().UseXminAsConcurrencyToken();
#pragma warning restore CS0618
            builder.Entity<VaccinationRecord>().HasIndex(v => v.BabyProfileId);

            builder.Entity<Recipe>().HasIndex(r => r.Status);
            builder.Entity<Recipe>().HasIndex(r => r.ProfileStage);
            builder.Entity<CriticalAlertLog>().HasIndex(c => c.IsResolved);
            builder.Entity<NotificationAlert>().HasIndex(n => n.Status);
            builder.Entity<SymptomLog>().HasIndex(s => s.AlertFlag);
            builder.Entity<UsdaFoodItem>().HasIndex(u => u.FdcId).IsUnique();

            // ── GLOBAL QUERY FILTER cho xoá mềm ──────────────────────────────
            // Mọi entity cài ISoftDeletable tự động được thêm "AND is_deleted = false"
            // vào MỌI truy vấn. Dò bằng reflection thay vì liệt kê tay, để sau này thêm
            // một entity vào cơ chế xoá mềm chỉ cần đổi lớp cha, không phải sửa file này.
            //
            // Cách bỏ qua bộ lọc khi cần xem cả bản ghi đã xoá: .IgnoreQueryFilters()
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType)) continue;

                // Dựng biểu thức:  e => !e.IsDeleted
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var isDeleted = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
                var notDeleted = Expression.Not(isDeleted);

                builder.Entity(entityType.ClrType)
                       .HasQueryFilter(Expression.Lambda(notDeleted, parameter));
            }
        }
    }
}

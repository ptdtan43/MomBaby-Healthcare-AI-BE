using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MomOi.API.Repositories
{
    /// <summary>
    /// Repository tổng quát cho một entity T.
    ///
    /// Nguyên tắc thiết kế: repository chỉ ĐÁNH DẤU thay đổi, KHÔNG commit.
    /// Việc commit thuộc về <see cref="IUnitOfWork.SaveChangesAsync"/> — xem lý do ở đó.
    /// </summary>
    public interface IGenericRepository<T> where T : class
    {
        // ─────────────────────────────────────────────────────────────────────
        // ĐỌC — MỘT BẢN GHI
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Tìm theo khoá chính. Luôn tracking (EF cần vậy để cache theo khoá).</summary>
        Task<T?> GetByIdAsync(object id);

        /// <summary>
        /// Lấy bản ghi đầu tiên khớp điều kiện.
        /// Đặt <paramref name="asNoTracking"/> = true khi CHỈ ĐỌC để bỏ qua change tracking.
        /// </summary>
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, bool asNoTracking = false);

        // ─────────────────────────────────────────────────────────────────────
        // ĐỌC — NHIỀU BẢN GHI
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Lọc danh sách. Khác bản cũ ở chỗ sắp xếp và giới hạn số lượng được đẩy
        /// XUỐNG DATABASE (thành ORDER BY / LIMIT) thay vì làm trong RAM.
        /// </summary>
        /// <param name="orderBy">Ví dụ: <c>q =&gt; q.OrderByDescending(x =&gt; x.CreatedAt)</c></param>
        /// <param name="take">Giới hạn số bản ghi. Null = không giới hạn (cân nhắc kỹ).</param>
        Task<IReadOnlyList<T>> FindAsync(
            Expression<Func<T, bool>> predicate,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            int? take = null,
            bool asNoTracking = false);

        /// <summary>
        /// Truy vấn có phân trang. Sinh ra 2 câu SQL: một COUNT(*) và một SELECT ... LIMIT/OFFSET.
        /// LUÔN truyền <paramref name="orderBy"/> — phân trang mà không sắp xếp thì thứ tự
        /// bản ghi do database tự quyết, trang 2 có thể lặp lại phần tử của trang 1.
        /// </summary>
        Task<PagedResult<T>> GetPagedAsync(
            int page,
            int pageSize,
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            bool asNoTracking = true);

        // ─────────────────────────────────────────────────────────────────────
        // ĐỌC — TỔNG HỢP (chạy trên DB, không kéo dữ liệu về)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Đếm bằng COUNT(*) trên database — không tải bản ghi nào về RAM.</summary>
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

        /// <summary>Kiểm tra tồn tại bằng EXISTS — dừng ngay khi thấy dòng đầu tiên.</summary>
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

        // ─────────────────────────────────────────────────────────────────────
        // GHI — chỉ đánh dấu vào ChangeTracker, chưa chạm database
        // ─────────────────────────────────────────────────────────────────────

        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        void Update(T entity);
        /// <summary>
        /// Xoá entity. Với entity cài <c>ISoftDeletable</c>, AppDbContext sẽ tự chuyển
        /// thành cập nhật cờ IsDeleted thay vì DELETE thật.
        /// </summary>
        void Remove(T entity);

        void RemoveRange(IEnumerable<T> entities);

        /// <summary>
        /// XOÁ THẬT khỏi database, bỏ qua cơ chế xoá mềm.
        ///
        /// Chỉ dùng khi người dùng thực hiện quyền yêu cầu xoá dữ liệu cá nhân theo
        /// Nghị định 13/2023/NĐ-CP. Với thao tác xoá thông thường hãy dùng
        /// <see cref="Remove"/> để còn khôi phục được.
        /// </summary>
        void RemovePermanently(T entity);

        // ─────────────────────────────────────────────────────────────────────
        // LỐI THOÁT HIỂM
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Trả về IQueryable thô cho các truy vấn phức tạp mà các hàm trên không diễn đạt nổi
        /// (Include nhiều cấp, GroupBy, Select projection, join...).
        ///
        /// ĐÂY LÀ LEAKY ABSTRACTION CÓ CHỦ ĐÍCH: nơi nào gọi Query() là nơi đó buộc phải
        /// hiểu EF Core. Hãy dùng như lối thoát hiểm, không dùng như cửa chính.
        /// </summary>
        IQueryable<T> Query(bool asNoTracking = false);
    }
}

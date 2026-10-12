using Microsoft.EntityFrameworkCore;
using MomOi.API.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MomOi.API.Repositories
{
    /// <summary>
    /// Triển khai repository tổng quát trên EF Core.
    ///
    /// Điểm mấu chốt của lớp này: mọi hàm đọc đều xây dựng một IQueryable rồi mới
    /// "vật chất hoá" (ToListAsync / FirstOrDefaultAsync / CountAsync) ở dòng cuối.
    /// Nhờ vậy Where + OrderBy + Skip + Take được EF dịch thành MỘT câu SQL duy nhất,
    /// thay vì tải hết về RAM rồi lọc bằng LINQ-to-Objects như bản cũ.
    /// </summary>
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        // ─── LỐI THOÁT HIỂM ───────────────────────────────────────────────────

        public IQueryable<T> Query(bool asNoTracking = false)
            => asNoTracking ? _dbSet.AsNoTracking() : _dbSet;

        // ─── ĐỌC: MỘT BẢN GHI ─────────────────────────────────────────────────

        public async Task<T?> GetByIdAsync(object id)
            => await _dbSet.FindAsync(id);

        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, bool asNoTracking = false)
            => await Query(asNoTracking).FirstOrDefaultAsync(predicate);

        // ─── ĐỌC: NHIỀU BẢN GHI ───────────────────────────────────────────────

        public async Task<IReadOnlyList<T>> FindAsync(
            Expression<Func<T, bool>> predicate,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            int? take = null,
            bool asNoTracking = false)
        {
            // Tới đây CHƯA có câu SQL nào chạy — chỉ đang dựng cây biểu thức.
            IQueryable<T> query = Query(asNoTracking).Where(predicate);

            if (orderBy != null)
                query = orderBy(query);          // => ORDER BY trong SQL

            if (take is > 0)
                query = query.Take(take.Value);  // => LIMIT trong SQL

            return await query.ToListAsync();    // <- CHÍNH DÒNG NÀY mới gửi SQL đi
        }

        public async Task<PagedResult<T>> GetPagedAsync(
            int page,
            int pageSize,
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            bool asNoTracking = true)
        {
            // Chặn tham số bẩn từ query string: ?page=-5&limit=999999
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 200) pageSize = 200;

            IQueryable<T> query = Query(asNoTracking);

            if (predicate != null)
                query = query.Where(predicate);

            // SQL #1: COUNT(*) — đếm trên database, không kéo bản ghi nào về.
            // Phải đếm TRƯỚC khi áp Skip/Take, vì ta cần tổng của mọi trang.
            var totalItems = await query.CountAsync();

            if (orderBy != null)
                query = orderBy(query);

            // SQL #2: SELECT ... ORDER BY ... LIMIT @pageSize OFFSET @skip
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<T>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };
        }

        // ─── ĐỌC: TỔNG HỢP ────────────────────────────────────────────────────

        public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
            => predicate == null
                ? await _dbSet.CountAsync()
                : await _dbSet.CountAsync(predicate);

        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
            => await _dbSet.AnyAsync(predicate);

        // ─── GHI: chỉ đánh dấu vào ChangeTracker, chưa chạm database ───────────

        public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

        public async Task AddRangeAsync(IEnumerable<T> entities) => await _dbSet.AddRangeAsync(entities);

        public void Update(T entity) => _dbSet.Update(entity);

        public void Remove(T entity) => _dbSet.Remove(entity);

        public void RemovePermanently(T entity)
        {
            // Báo cho AppDbContext biết entity này KHÔNG được chuyển sang xoá mềm,
            // rồi mới phát lệnh xoá như bình thường.
            _context.MarkPermanentDeletion(entity!);
            _dbSet.Remove(entity);
        }

        public void RemoveRange(IEnumerable<T> entities) => _dbSet.RemoveRange(entities);
    }
}

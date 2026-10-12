using Microsoft.EntityFrameworkCore;
using MomOi.API.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MomOi.API.Repositories
{
    /// <summary>
    /// Triển khai Unit of Work — nơi DUY NHẤT trong Repository layer
    /// được phép inject và biết về AppDbContext.
    ///
    /// Lớp này cố ý KHÔNG implement IDisposable. Xem giải thích ở IUnitOfWork:
    /// context là tài nguyên do DI container sở hữu, không phải của lớp này.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private readonly Dictionary<Type, object> _repositories = new();

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy hoặc tạo mới repository cho entity T.
        /// Mỗi loại entity chỉ có một repository instance trong một request.
        /// </summary>
        public IGenericRepository<T> Repository<T>() where T : class
        {
            var type = typeof(T);
            if (!_repositories.ContainsKey(type))
            {
                _repositories[type] = new GenericRepository<T>(_context);
            }
            return (IGenericRepository<T>)_repositories[type];
        }

        /// <summary>
        /// Lưu tất cả thay đổi xuống database.
        /// </summary>
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation)
        {
            // CreateExecutionStrategy() trả về chiến lược thử lại đã cấu hình ở
            // ServiceCollectionExtensions (EnableRetryOnFailure). Phải mở transaction
            // BÊN TRONG nó, vì khi phải thử lại thì cả transaction phải được làm lại
            // từ đầu — không thể thử lại nửa chừng một transaction đã hỏng.
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                var result = await operation();

                await transaction.CommitAsync();
                return result;

                // Không cần khối catch: nếu operation ném ngoại lệ, transaction chưa được
                // commit và "await using" sẽ Dispose nó -> database tự ROLLBACK.
            });
        }

        public Task ExecuteInTransactionAsync(Func<Task> operation)
            => ExecuteInTransactionAsync<object?>(async () =>
            {
                await operation();
                return null;
            });
    }
}

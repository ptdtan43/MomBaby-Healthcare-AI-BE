using MomOi.API.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MomOi.API.Repositories
{
    /// <summary>
    /// Triển khai Unit of Work — nơi DUY NHẤT trong Repository layer
    /// được phép inject và biết về AppDbContext.
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

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}

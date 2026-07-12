using System;
using System.Threading.Tasks;

namespace MomOi.API.Repositories
{
    /// <summary>
    /// Unit of Work pattern — tổng hợp tất cả repositories và quản lý
    /// một DbContext duy nhất trong vòng đời của một HTTP request.
    /// Service layer chỉ cần biết IUnitOfWork, không biết AppDbContext.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Lấy repository generic cho entity T.
        /// </summary>
        IGenericRepository<T> Repository<T>() where T : class;

        /// <summary>
        /// Lưu tất cả thay đổi xuống database trong một transaction.
        /// </summary>
        Task<int> SaveChangesAsync();
    }
}

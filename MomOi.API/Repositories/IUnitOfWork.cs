using System;
using System.Threading.Tasks;

namespace MomOi.API.Repositories
{
    /// <summary>
    /// Unit of Work pattern — tổng hợp tất cả repositories và quản lý
    /// một DbContext duy nhất trong vòng đời của một HTTP request.
    /// Service layer chỉ cần biết IUnitOfWork, không biết AppDbContext.
    ///
    /// KHÔNG kế thừa IDisposable: AppDbContext do DI container tạo ra và sở hữu
    /// (đăng ký Scoped), nên container mới là nơi chịu trách nhiệm huỷ nó khi
    /// request kết thúc. UnitOfWork chỉ "mượn" context qua constructor — mượn
    /// thì không được huỷ, vì huỷ sớm sẽ giết context mà các service khác trong
    /// cùng scope vẫn đang dùng.
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Lấy repository generic cho entity T.
        /// </summary>
        IGenericRepository<T> Repository<T>() where T : class;

        /// <summary>
        /// Lưu tất cả thay đổi xuống database.
        ///
        /// Bản thân một lần gọi SaveChanges đã là nguyên tử: EF gói mọi lệnh INSERT/UPDATE/
        /// DELETE sinh ra trong MỘT transaction ngầm. Nhưng nếu một nghiệp vụ cần NHIỀU lần
        /// lưu, hoặc cần gọi thêm thứ khác ngoài DbContext (ví dụ UserManager), thì phải
        /// bọc tất cả bằng <see cref="ExecuteInTransactionAsync{TResult}"/>.
        /// </summary>
        Task<int> SaveChangesAsync();

        /// <summary>
        /// Chạy một nghiệp vụ trong MỘT transaction tường minh: thành công thì commit,
        /// có ngoại lệ thì rollback toàn bộ.
        ///
        /// ⚠️ Vì chuỗi kết nối bật EnableRetryOnFailure, KHÔNG được tự gọi
        /// BeginTransactionAsync — EF sẽ ném lỗi "The configured execution strategy does
        /// not support user-initiated transactions". Hàm này đã xử lý đúng bằng
        /// execution strategy, nên hãy luôn dùng nó thay vì mở transaction bằng tay.
        ///
        /// ⚠️ Khi kết nối chập chờn, <paramref name="operation"/> có thể được chạy LẠI
        /// từ đầu. Vì vậy nó chỉ nên chứa thao tác database, không nên gửi email, gọi API
        /// bên ngoài hay đẩy thông báo — những việc đó không rollback được.
        /// </summary>
        Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation);

        /// <summary>Bản không trả về giá trị của <see cref="ExecuteInTransactionAsync{TResult}"/>.</summary>
        Task ExecuteInTransactionAsync(Func<Task> operation);
    }
}

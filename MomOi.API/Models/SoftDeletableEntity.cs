using System;

namespace MomOi.API.Models
{
    /// <summary>
    /// Đánh dấu entity dùng cơ chế XOÁ MỀM: lệnh xoá được AppDbContext chuyển thành
    /// lệnh cập nhật cờ, và mọi truy vấn tự động ẩn các bản ghi đã đánh dấu.
    ///
    /// Đây là interface đánh dấu (marker interface) — AppDbContext dò nó bằng reflection
    /// để gắn global query filter, nên thêm một entity vào cơ chế này chỉ cần đổi lớp cha.
    /// </summary>
    public interface ISoftDeletable
    {
        bool IsDeleted { get; set; }
        DateTime? DeletedAt { get; set; }
    }

    /// <summary>
    /// Entity có xoá mềm.
    ///
    /// CHỈ dùng cho dữ liệu DO NGƯỜI DÙNG NHẬP hoặc có ý nghĩa y tế — thứ mà xoá nhầm
    /// là mất mát thật và cần khôi phục được.
    ///
    /// KHÔNG dùng cho dữ liệu hệ thống tự sinh (cảnh báo, thông báo) vì chúng được
    /// xoá và tạo lại liên tục; xoá mềm sẽ khiến bảng phình vô hạn.
    ///
    /// Lưu ý pháp lý: xoá mềm KHÔNG phải là xoá. Khi người dùng thực hiện quyền yêu cầu
    /// xoá dữ liệu cá nhân theo Nghị định 13/2023/NĐ-CP, phải dùng
    /// <see cref="MomOi.API.Repositories.IGenericRepository{T}.RemovePermanently"/>.
    /// </summary>
    public abstract class SoftDeletableEntity : BaseEntity, ISoftDeletable
    {
        /// <summary>Bản ghi đã bị xoá mềm hay chưa. Mọi truy vấn tự động lọc theo cờ này.</summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>Thời điểm bị xoá mềm. Null nghĩa là chưa bị xoá.</summary>
        public DateTime? DeletedAt { get; set; }
    }
}

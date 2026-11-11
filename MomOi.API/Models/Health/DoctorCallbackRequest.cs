using MomOi.API.Models.Identity;
using System;

namespace MomOi.API.Models.Health
{
    public enum DoctorCallbackStatus
    {
        /// <summary>Mẹ vừa gửi, đang chờ bác sĩ gọi.</summary>
        Pending = 0,
        /// <summary>Bác sĩ đã gọi và tư vấn xong. Chỉ trạng thái này phát sinh phí.</summary>
        Completed = 1,
        /// <summary>Bác sĩ gọi nhưng không liên lạc được. Không tính phí.</summary>
        Unreachable = 2,
        /// <summary>Mẹ tự huỷ trước khi bác sĩ gọi. Không tính phí.</summary>
        Cancelled = 3
    }

    /// <summary>
    /// Yêu cầu "bác sĩ gọi lại" của hội viên Super Mom VIP.
    /// Số điện thoại bác sĩ không bao giờ lộ cho mẹ: mẹ để lại số của mình,
    /// bác sĩ chủ động gọi. Phí chỉ phát sinh khi bác sĩ đánh dấu đã tư vấn xong.
    /// </summary>
    public class DoctorCallbackRequest : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;

        /// <summary>Số điện thoại mẹ muốn được gọi tới.</summary>
        public string ContactPhone { get; set; } = string.Empty;

        /// <summary>Mô tả ngắn điều mẹ muốn hỏi, để bác sĩ chuẩn bị trước khi gọi.</summary>
        public string Concern { get; set; } = string.Empty;

        /// <summary>Khung giờ mẹ tiện nghe máy (tuỳ chọn), ví dụ "Sau 19h".</summary>
        public string? PreferredTime { get; set; }

        public DoctorCallbackStatus Status { get; set; } = DoctorCallbackStatus.Pending;

        /// <summary>Tài khoản chuyên gia đã xử lý yêu cầu.</summary>
        public string? HandledByUserId { get; set; }
        public DateTime? HandledAt { get; set; }

        /// <summary>Ghi chú nội bộ của bác sĩ sau cuộc gọi.</summary>
        public string? DoctorNote { get; set; }

        /// <summary>Phí chốt tại thời điểm hoàn tất (0 nếu không tính phí).</summary>
        public decimal Fee { get; set; }

        /// <summary>Mã đơn thanh toán VietQR cho khoản phí, sinh khi mẹ bấm thanh toán.</summary>
        public string? FeeOrderCode { get; set; }

        /// <summary>Thời điểm phí được xác nhận đã thanh toán. Null = chưa trả.</summary>
        public DateTime? FeePaidAt { get; set; }
    }
}

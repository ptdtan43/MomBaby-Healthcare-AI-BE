using System;
using System.ComponentModel.DataAnnotations;

namespace MomOi.API.DTOs.DoctorCallback
{
    public class CreateDoctorCallbackDto
    {
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [MaxLength(20)]
        public string ContactPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng mô tả điều mẹ muốn hỏi.")]
        [MinLength(10, ErrorMessage = "Mô tả cần ít nhất 10 ký tự.")]
        [MaxLength(1000)]
        public string Concern { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? PreferredTime { get; set; }
    }

    public class ResolveDoctorCallbackDto
    {
        [MaxLength(1000)]
        public string? Note { get; set; }
    }

    /// <summary>Thông tin trả cho mẹ. Không có số điện thoại bác sĩ.</summary>
    public class DoctorCallbackDto
    {
        public int Id { get; set; }
        public string ContactPhone { get; set; } = string.Empty;
        public string Concern { get; set; } = string.Empty;
        public string? PreferredTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal Fee { get; set; }
        public bool FeeDue { get; set; }
        public DateTime? FeePaidAt { get; set; }
        public string? FeeOrderCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? HandledAt { get; set; }
    }

    /// <summary>Thông tin trả cho bác sĩ / chuyên gia trong hàng đợi.</summary>
    public class DoctorCallbackQueueItemDto : DoctorCallbackDto
    {
        public string MomName { get; set; } = string.Empty;
        public string? MomEmail { get; set; }
        public string? DoctorNote { get; set; }
    }
}

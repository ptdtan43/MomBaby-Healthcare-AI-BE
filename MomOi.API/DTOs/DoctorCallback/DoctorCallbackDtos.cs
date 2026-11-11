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

        /// <summary>Lần kiểm tra triệu chứng muốn đính kèm (phải thuộc về chính mẹ).</summary>
        public int? SymptomLogId { get; set; }
    }

    /// <summary>Tóm tắt lần kiểm tra triệu chứng đính kèm, để bác sĩ xem trước khi gọi.</summary>
    public class AttachedSymptomDto
    {
        public int Id { get; set; }
        public string TextDescription { get; set; } = string.Empty;
        /// <summary>Ảnh mẹ chụp (data URL hoặc đường dẫn), null nếu không có ảnh.</summary>
        public string? ImageUrl { get; set; }
        public string? UrgencyLevel { get; set; }
        public bool ShouldSeeDoctor { get; set; }
        public string? SpecialistType { get; set; }
        /// <summary>JSON kết quả AI (cùng định dạng /api/symptoms) để FE hiển thị các khả năng.</summary>
        public string? AnalysisJson { get; set; }
        public DateTime CreatedAt { get; set; }
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
        public int? SymptomLogId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? HandledAt { get; set; }
    }

    /// <summary>Thông tin trả cho bác sĩ / chuyên gia trong hàng đợi.</summary>
    public class DoctorCallbackQueueItemDto : DoctorCallbackDto
    {
        public string MomName { get; set; } = string.Empty;
        public string? MomEmail { get; set; }
        public string? DoctorNote { get; set; }
        /// <summary>Chỉ có với yêu cầu đang chờ, để hàng đợi không phải tải ảnh của các yêu cầu cũ.</summary>
        public AttachedSymptomDto? Symptom { get; set; }
    }
}

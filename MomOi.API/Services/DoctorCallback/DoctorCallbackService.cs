using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MomOi.API.Constants;
using MomOi.API.DTOs;
using MomOi.API.DTOs.DoctorCallback;
using MomOi.API.Models.Health;
using MomOi.API.Models.Identity;
using MomOi.API.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MomOi.API.Services.DoctorCallback
{
    public interface IDoctorCallbackService
    {
        Task<ApiResponse<DoctorCallbackDto>> CreateAsync(string userId, CreateDoctorCallbackDto dto);
        Task<ApiResponse<IReadOnlyList<DoctorCallbackDto>>> GetMineAsync(string userId);
        Task<ApiResponse<DoctorCallbackDto>> CancelAsync(string userId, int id);

        Task<ApiResponse<IReadOnlyList<DoctorCallbackQueueItemDto>>> GetQueueAsync();
        Task<ApiResponse<DoctorCallbackQueueItemDto>> CompleteAsync(string expertId, int id, string? note);
        Task<ApiResponse<DoctorCallbackQueueItemDto>> MarkUnreachableAsync(string expertId, int id, string? note);
    }

    /// <summary>
    /// "Bác sĩ gọi lại": mẹ (Super Mom VIP) để lại số và câu hỏi, bác sĩ chủ động gọi.
    /// Phí <see cref="DoctorCallbackPricing.FeePerCall"/> chỉ phát sinh khi bác sĩ xác nhận
    /// đã tư vấn xong; gọi không được hoặc mẹ huỷ thì không mất tiền.
    /// </summary>
    public class DoctorCallbackService : IDoctorCallbackService
    {
        private static readonly Regex VietnamPhone = new(@"^(0|84)(3|5|7|8|9)\d{8}$", RegexOptions.Compiled);

        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<DoctorCallbackService> _logger;

        public DoctorCallbackService(
            IUnitOfWork unitOfWork,
            UserManager<AppUser> userManager,
            ILogger<DoctorCallbackService> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _logger = logger;
        }

        private IGenericRepository<DoctorCallbackRequest> Repo => _unitOfWork.Repository<DoctorCallbackRequest>();

        // ─── Phía mẹ ─────────────────────────────────────────────────────────────

        public async Task<ApiResponse<DoctorCallbackDto>> CreateAsync(string userId, CreateDoctorCallbackDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return ApiResponse<DoctorCallbackDto>.FailureResult("Không tìm thấy người dùng.");

            if (user.EffectiveTier < SubscriptionTier.SuperMomVip)
                return ApiResponse<DoctorCallbackDto>.FailureResult(
                    "Tính năng bác sĩ gọi lại dành cho hội viên Super Mom VIP.",
                    errorCode: "TIER_REQUIRED");

            var phone = NormalizePhone(dto.ContactPhone);
            if (phone == null)
                return ApiResponse<DoctorCallbackDto>.FailureResult(
                    "Số điện thoại chưa đúng. Vui lòng nhập số di động Việt Nam, ví dụ 0912345678.",
                    errorCode: "INVALID_PHONE");

            var concern = dto.Concern?.Trim() ?? string.Empty;
            if (concern.Length < 10)
                return ApiResponse<DoctorCallbackDto>.FailureResult(
                    "Mẹ mô tả thêm một chút (ít nhất 10 ký tự) để bác sĩ chuẩn bị trước nhé.",
                    errorCode: "CONCERN_TOO_SHORT");

            if (await Repo.ExistsAsync(r => r.UserId == userId && r.Status == DoctorCallbackStatus.Pending))
                return ApiResponse<DoctorCallbackDto>.FailureResult(
                    "Mẹ đang có một yêu cầu chờ bác sĩ gọi. Vui lòng chờ hoặc huỷ yêu cầu đó trước.",
                    errorCode: "CALLBACK_ALREADY_PENDING");

            if (await Repo.ExistsAsync(r => r.UserId == userId
                                            && r.Status == DoctorCallbackStatus.Completed
                                            && r.Fee > 0
                                            && r.FeePaidAt == null))
                return ApiResponse<DoctorCallbackDto>.FailureResult(
                    "Mẹ còn phí cuộc tư vấn trước chưa thanh toán. Vui lòng thanh toán để gửi yêu cầu mới.",
                    errorCode: "CALLBACK_FEE_UNPAID");

            var entity = new DoctorCallbackRequest
            {
                UserId = userId,
                ContactPhone = phone,
                Concern = concern,
                PreferredTime = string.IsNullOrWhiteSpace(dto.PreferredTime) ? null : dto.PreferredTime.Trim(),
                Status = DoctorCallbackStatus.Pending
            };

            await Repo.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Doctor callback {Id} requested by user {UserId}.", entity.Id, userId);
            return ApiResponse<DoctorCallbackDto>.SuccessResult(ToDto(entity),
                "Đã gửi yêu cầu. Bác sĩ sẽ gọi lại cho mẹ sớm nhất có thể.");
        }

        public async Task<ApiResponse<IReadOnlyList<DoctorCallbackDto>>> GetMineAsync(string userId)
        {
            var items = await Repo.FindAsync(
                r => r.UserId == userId,
                orderBy: q => q.OrderByDescending(r => r.CreatedAt),
                take: 30,
                asNoTracking: true);

            return ApiResponse<IReadOnlyList<DoctorCallbackDto>>.SuccessResult(
                items.Select(ToDto).ToList());
        }

        public async Task<ApiResponse<DoctorCallbackDto>> CancelAsync(string userId, int id)
        {
            var entity = await Repo.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
            if (entity == null)
                return ApiResponse<DoctorCallbackDto>.FailureResult("Không tìm thấy yêu cầu.", errorCode: "CALLBACK_NOT_FOUND");

            if (entity.Status != DoctorCallbackStatus.Pending)
                return ApiResponse<DoctorCallbackDto>.FailureResult(
                    "Yêu cầu này đã được bác sĩ xử lý, không huỷ được nữa.",
                    errorCode: "CALLBACK_NOT_PENDING");

            entity.Status = DoctorCallbackStatus.Cancelled;
            entity.HandledAt = DateTime.UtcNow;
            Repo.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<DoctorCallbackDto>.SuccessResult(ToDto(entity), "Đã huỷ yêu cầu.");
        }

        // ─── Phía bác sĩ / chuyên gia ────────────────────────────────────────────

        public async Task<ApiResponse<IReadOnlyList<DoctorCallbackQueueItemDto>>> GetQueueAsync()
        {
            var since = DateTime.UtcNow.AddDays(-30);
            var items = await Repo.Query(asNoTracking: true)
                .Include(r => r.User)
                .Where(r => r.Status == DoctorCallbackStatus.Pending || r.CreatedAt >= since)
                .OrderBy(r => r.Status == DoctorCallbackStatus.Pending ? 0 : 1)
                .ThenBy(r => r.Status == DoctorCallbackStatus.Pending ? r.CreatedAt : DateTime.MinValue)
                .ThenByDescending(r => r.HandledAt)
                .Take(100)
                .ToListAsync();

            return ApiResponse<IReadOnlyList<DoctorCallbackQueueItemDto>>.SuccessResult(
                items.Select(ToQueueDto).ToList());
        }

        public Task<ApiResponse<DoctorCallbackQueueItemDto>> CompleteAsync(string expertId, int id, string? note) =>
            ResolveAsync(expertId, id, note, DoctorCallbackStatus.Completed);

        public Task<ApiResponse<DoctorCallbackQueueItemDto>> MarkUnreachableAsync(string expertId, int id, string? note) =>
            ResolveAsync(expertId, id, note, DoctorCallbackStatus.Unreachable);

        private async Task<ApiResponse<DoctorCallbackQueueItemDto>> ResolveAsync(
            string expertId, int id, string? note, DoctorCallbackStatus outcome)
        {
            var entity = await Repo.Query()
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (entity == null)
                return ApiResponse<DoctorCallbackQueueItemDto>.FailureResult("Không tìm thấy yêu cầu.", errorCode: "CALLBACK_NOT_FOUND");

            if (entity.Status != DoctorCallbackStatus.Pending)
                return ApiResponse<DoctorCallbackQueueItemDto>.FailureResult(
                    "Yêu cầu này đã được xử lý hoặc mẹ đã huỷ.",
                    errorCode: "CALLBACK_NOT_PENDING");

            entity.Status = outcome;
            entity.HandledByUserId = expertId;
            entity.HandledAt = DateTime.UtcNow;
            entity.DoctorNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            // Chỉ cuộc tư vấn thành công mới phát sinh phí.
            entity.Fee = outcome == DoctorCallbackStatus.Completed ? DoctorCallbackPricing.FeePerCall : 0m;

            Repo.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Doctor callback {Id} marked {Status} by {ExpertId}.", id, outcome, expertId);
            return ApiResponse<DoctorCallbackQueueItemDto>.SuccessResult(ToQueueDto(entity),
                outcome == DoctorCallbackStatus.Completed
                    ? "Đã ghi nhận cuộc tư vấn thành công."
                    : "Đã ghi nhận không liên lạc được. Mẹ không bị tính phí.");
        }

        // ─── Tiện ích ────────────────────────────────────────────────────────────

        /// <summary>Bỏ khoảng trắng, dấu chấm, gạch; đổi +84 về dạng chuẩn. Trả null nếu không hợp lệ.</summary>
        public static string? NormalizePhone(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var digits = Regex.Replace(raw, @"[^\d]", string.Empty);
            if (!VietnamPhone.IsMatch(digits)) return null;
            return digits.StartsWith("84") ? "0" + digits[2..] : digits;
        }

        private static DoctorCallbackDto ToDto(DoctorCallbackRequest r) => Fill(new DoctorCallbackDto(), r);

        private static DoctorCallbackQueueItemDto ToQueueDto(DoctorCallbackRequest r)
        {
            var dto = Fill(new DoctorCallbackQueueItemDto(), r);
            dto.MomName = string.IsNullOrWhiteSpace(r.User?.FullName) ? "Mẹ (chưa đặt tên)" : r.User.FullName;
            dto.MomEmail = r.User?.Email;
            dto.DoctorNote = r.DoctorNote;
            return dto;
        }

        private static T Fill<T>(T dto, DoctorCallbackRequest r) where T : DoctorCallbackDto
        {
            dto.Id = r.Id;
            dto.ContactPhone = r.ContactPhone;
            dto.Concern = r.Concern;
            dto.PreferredTime = r.PreferredTime;
            dto.Status = r.Status.ToString();
            dto.Fee = r.Fee;
            dto.FeeDue = r.Status == DoctorCallbackStatus.Completed && r.Fee > 0 && r.FeePaidAt == null;
            dto.FeePaidAt = r.FeePaidAt;
            dto.FeeOrderCode = r.FeeOrderCode;
            dto.CreatedAt = r.CreatedAt;
            dto.HandledAt = r.HandledAt;
            return dto;
        }
    }
}

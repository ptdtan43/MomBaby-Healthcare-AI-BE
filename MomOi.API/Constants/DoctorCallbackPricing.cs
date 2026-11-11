namespace MomOi.API.Constants
{
    /// <summary>Bảng giá dịch vụ "bác sĩ gọi lại".</summary>
    public static class DoctorCallbackPricing
    {
        /// <summary>Phí mỗi cuộc tư vấn thành công (VND).</summary>
        public const decimal FeePerCall = 5_000m;

        /// <summary>
        /// PlanCode ghi vào PaymentTransaction để PaymentService biết đây là phí cuộc gọi,
        /// không phải mua gói: xác nhận thanh toán sẽ KHÔNG đổi tier của người dùng.
        /// </summary>
        public const string FeePlanCode = "CALLBACK_FEE";

        public const string FeePlanName = "Phi bac si goi lai tu van";
    }
}

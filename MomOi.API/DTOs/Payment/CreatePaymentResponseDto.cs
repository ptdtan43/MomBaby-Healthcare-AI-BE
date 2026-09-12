namespace MomOi.API.DTOs.Payment
{
    public class CreatePaymentResponseDto
    {
        public string OrderCode { get; set; } = string.Empty;
        public string PayUrl { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
    }
}

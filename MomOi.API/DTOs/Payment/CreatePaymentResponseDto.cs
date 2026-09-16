using System;

namespace MomOi.API.DTOs.Payment
{
    public class CreatePaymentResponseDto
    {
        public string OrderCode { get; set; } = string.Empty;
        public string PayUrl { get; set; } = string.Empty;
        public string QrUrl { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public string PlanCode { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string TransferMemo { get; set; } = string.Empty;
        public BankTransferInfoDto? BankInfo { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class BankTransferInfoDto
    {
        public string BankId { get; set; } = string.Empty;
        public string Bin { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string BankShortName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountHolderDisplay { get; set; } = string.Empty;
    }
}

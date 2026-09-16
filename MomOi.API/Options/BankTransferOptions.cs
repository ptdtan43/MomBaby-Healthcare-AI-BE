namespace MomOi.API.Options
{
    public class BankTransferOptions
    {
        public const string SectionName = "Payment:BankTransfer";

        public string BankId { get; set; } = "MB";
        public string Bin { get; set; } = "970422";
        public string BankName { get; set; } = "MB Bank";
        public string BankShortName { get; set; } = "MB Bank";
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountHolderDisplay { get; set; } = string.Empty;
        public string VietQrTemplate { get; set; } = "compact2";
        public string SepayApiToken { get; set; } = string.Empty;
        public string SepayTransactionsUrl { get; set; } = "https://my.sepay.vn/userapi/transactions/list?limit=25";
    }
}

namespace MomOi.API.DTOs.Payment
{
    /// <summary>
    /// Payload MoMo gửi tới ipnUrl bằng POST JSON. Tên trường phải khớp đúng tài liệu MoMo
    /// vì chúng tham gia trực tiếp vào chuỗi kiểm chữ ký.
    /// </summary>
    public class MoMoIpnDto
    {
        public string PartnerCode { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public long Amount { get; set; }
        public string OrderInfo { get; set; } = string.Empty;
        public string OrderType { get; set; } = string.Empty;
        public long TransId { get; set; }
        public int ResultCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string PayType { get; set; } = string.Empty;
        public long ResponseTime { get; set; }
        public string ExtraData { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
    }
}

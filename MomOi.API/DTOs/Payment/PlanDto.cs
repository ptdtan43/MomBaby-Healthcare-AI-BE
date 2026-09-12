namespace MomOi.API.DTOs.Payment
{
    public class PlanDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Months { get; set; }
        public string Tier { get; set; } = string.Empty;
    }
}

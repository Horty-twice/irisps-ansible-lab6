namespace NumismaticClub.Models
{
    public class CreateCoinDto
    {
        public string Country { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Nominal { get; set; } = string.Empty;
        public string Metal { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
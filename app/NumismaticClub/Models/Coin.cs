namespace NumismaticClub.Models
{
    public class Coin
    {
        public int Id { get; set; }
        public string Country { get; set; } = string.Empty;   // Страна
        public int Year { get; set; }                          // Год чеканки
        public string Nominal { get; set; } = string.Empty;    // Номинал (например, "10 рублей")
        public string Metal { get; set; } = string.Empty;      // Металл (золото, серебро)
        public decimal Price { get; set; }                     // Оценочная стоимость
        public string OwnerName { get; set; } = string.Empty;  // Имя владельца в клубе
        public string Description { get; set; } = string.Empty;// Описание (состояние, редкость)
    }
}
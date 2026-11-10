using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NumismaticClub.Models
{
    public class MongoCoin
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string Country { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Nominal { get; set; } = string.Empty;
        public string Metal { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public string? UserId { get; set; }// Кто подтвердил
        public string ConfirmationStatus { get; set; } = "pending";// pending / confirmed
        public DateTime? ConfirmedAt { get; set; }// Время подтверждения
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
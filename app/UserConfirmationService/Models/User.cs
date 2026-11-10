using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UserConfirmationService.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int RegisteredObjects { get; set; } = 0;
        public string PasswordHash { get; set; } = string.Empty;
    }
}
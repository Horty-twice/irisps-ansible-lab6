using MongoDB.Driver;
using UserConfirmationService.Models;

namespace UserConfirmationService.Services
{
    public class MongoDbService
    {
        private readonly IMongoCollection<User> _usersCollection;

        public MongoDbService(IConfiguration configuration)
        {
            var connectionString = configuration["MongoDB:ConnectionString"];
            var databaseName = configuration["MongoDB:DatabaseName"];
            var collectionName = configuration["MongoDB:CollectionName"];

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _usersCollection = database.GetCollection<User>(collectionName);
        }

        public IMongoCollection<User> Users => _usersCollection;
    }
}
using MongoDB.Driver;
using NumismaticClub.Models;

namespace NumismaticClub.Services
{
    public class MongoDbService
    {
        private readonly IMongoCollection<MongoCoin> _coinsCollection;

        public MongoDbService(IConfiguration configuration)
        {
            var connectionString = configuration["MongoDB:ConnectionString"];
            var databaseName = configuration["MongoDB:DatabaseName"];
            var collectionName = configuration["MongoDB:CollectionName"];

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _coinsCollection = database.GetCollection<MongoCoin>(collectionName);
        }

        public IMongoCollection<MongoCoin> Coins => _coinsCollection;
    }
}
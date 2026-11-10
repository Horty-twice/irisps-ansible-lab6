using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using NumismaticClub.Models;
using NumismaticClub.Services;
using System.Security.Claims;

namespace NumismaticClub.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CoinsController : ControllerBase
    {
        private readonly IMongoCollection<MongoCoin> _coinsCollection;
        private readonly MetricsService _metricsService;
        private readonly RedisCacheService _cacheService;
        private readonly KafkaProducerService _kafkaProducerService;
        private readonly ConfirmationMetricsService _confirmationMetrics;

        public CoinsController(MongoDbService mongoDbService, MetricsService metricsService, RedisCacheService cacheService, KafkaProducerService kafkaProducerService, ConfirmationMetricsService confirmationMetrics)
        {
            _coinsCollection = mongoDbService.Coins;
            _metricsService = metricsService;
            _cacheService = cacheService;
            _kafkaProducerService = kafkaProducerService;
            _confirmationMetrics = confirmationMetrics;
        }
        
        // GET: api/Coins
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CoinDto>>> GetCoins()
        {
            const string cacheKey = "coins_list";

            // 1. Сначала проверяем кэш
            var cachedCoins = await _cacheService.GetAsync<List<CoinDto>>(cacheKey);
            if (cachedCoins != null)
            {
                return Ok(cachedCoins);
            }

            // 2. В кэше нет — считаем количество монет в базе
            var count = await _coinsCollection.CountDocumentsAsync(_ => true);

            List<MongoCoin> coins;

            if (count > 1000)
            {
                // Если больше 1000 — выбираем 1000 случайных через агрегацию $sample
                coins = await _coinsCollection.Aggregate()
                    .Sample(1000)
                    .ToListAsync();
            }
            else
            {
                // Если 1000 или меньше — возвращаем все
                coins = await _coinsCollection.Find(_ => true).ToListAsync();
            }

            // 3. Преобразуем в DTO
            var coinDtos = coins.Select(c => new CoinDto
            {
                Id = c.Id!,
                Country = c.Country,
                Year = c.Year,
                Nominal = c.Nominal,
                Metal = c.Metal,
                Price = c.Price,
                OwnerName = c.OwnerName,
                Description = c.Description,
                UserId = c.UserId,
                ConfirmationStatus = c.ConfirmationStatus,
                ConfirmedAt = c.ConfirmedAt,
                CreatedAt = c.CreatedAt
            }).ToList();

            // 4. Сохраняем в кэш на 5 минут
            await _cacheService.SetAsync(cacheKey, coinDtos, TimeSpan.FromMinutes(5));

            return Ok(coinDtos);
        }

        // POST: api/Coins
        [HttpPost]
        public async Task<ActionResult<CoinDto>> PostCoin(CreateCoinDto createCoinDto)
        {
            // 1. Берём userId из JWT-токена
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Не удалось определить пользователя из токена.");
            }

            // 2. Создаём монету, userId — из токена
            var coin = new MongoCoin
            {
                Country = createCoinDto.Country,
                Year = createCoinDto.Year,
                Nominal = createCoinDto.Nominal,
                Metal = createCoinDto.Metal,
                Price = createCoinDto.Price,
                OwnerName = createCoinDto.OwnerName,
                Description = createCoinDto.Description,
                UserId = userId,
                ConfirmationStatus = "pending",
                ConfirmedAt = null,
                CreatedAt = DateTime.UtcNow
            };

            await _coinsCollection.InsertOneAsync(coin);
            _metricsService.RecordCoinAdded();

            // 3. Отправляем запрос на подтверждение в Kafka
            await _kafkaProducerService.SendConfirmationRequestAsync(coin.Id!, userId);
            _confirmationMetrics.RecordConfirmationRequest();

            var coinDto = new CoinDto
            {
                Id = coin.Id!,
                Country = coin.Country,
                Year = coin.Year,
                Nominal = coin.Nominal,
                Metal = coin.Metal,
                Price = coin.Price,
                OwnerName = coin.OwnerName,
                Description = coin.Description,
                UserId = coin.UserId,
                ConfirmationStatus = coin.ConfirmationStatus,
                ConfirmedAt = coin.ConfirmedAt,
                CreatedAt = coin.CreatedAt
            };

            // 4. Кэш: удаляем список монет (устарел)
            await _cacheService.RemoveAsync("coins_list");

            return CreatedAtAction(nameof(GetCoin), new { id = coin.Id }, coinDto);
        }

        // GET: api/Coins/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CoinDto>> GetCoin(string id)
        {
            // Проверяем, является ли id корректным ObjectId
            if (!ObjectId.TryParse(id, out _))
            {
                return BadRequest("Некорректный формат идентификатора. Id должен быть 24-символьной шестнадцатеричной строкой.");
            }

            // 1. Сначала проверяем кэш
            var cacheKey = $"coin_{id}";
            var cachedCoin = await _cacheService.GetAsync<CoinDto>(cacheKey);

            if (cachedCoin != null)
            {
                return Ok(cachedCoin);
            }

            // 2. В кэше нет — идём в MongoDB
            var coin = await _coinsCollection.Find(c => c.Id == id).FirstOrDefaultAsync();
            if (coin == null)
            {
                return NotFound();
            }

            var coinDto = new CoinDto
            {
                Id = coin.Id!,
                Country = coin.Country,
                Year = coin.Year,
                Nominal = coin.Nominal,
                Metal = coin.Metal,
                Price = coin.Price,
                OwnerName = coin.OwnerName,
                Description = coin.Description,
                UserId = coin.UserId,
                ConfirmationStatus = coin.ConfirmationStatus,
                ConfirmedAt = coin.ConfirmedAt,
                CreatedAt = coin.CreatedAt
            };

            // 3. Сохраняем в кэш на 5 минут
            await _cacheService.SetAsync(cacheKey, coinDto, TimeSpan.FromMinutes(5));

            return Ok(coinDto);
        }

        // PUT: api/Coins/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCoin(string id, CreateCoinDto updateCoinDto)
        {
            if (!ObjectId.TryParse(id, out _))
            {
                return BadRequest("Некорректный формат идентификатора.");
            }

            var existingCoin = await _coinsCollection.Find(c => c.Id == id).FirstOrDefaultAsync();
            if (existingCoin == null)
            {
                return NotFound();
            }

            var updatedCoin = new MongoCoin
            {
                Id = id,
                Country = updateCoinDto.Country,
                Year = updateCoinDto.Year,
                Nominal = updateCoinDto.Nominal,
                Metal = updateCoinDto.Metal,
                Price = updateCoinDto.Price,
                OwnerName = updateCoinDto.OwnerName,
                Description = updateCoinDto.Description,
                // Сохраняем поля, которых нет в DTO:
                UserId = existingCoin.UserId,
                ConfirmationStatus = existingCoin.ConfirmationStatus,
                ConfirmedAt = existingCoin.ConfirmedAt,
                CreatedAt = existingCoin.CreatedAt
            };

            await _coinsCollection.ReplaceOneAsync(c => c.Id == id, updatedCoin);
            // Удаляем кэш, так как данные изменились
            await _cacheService.RemoveAsync($"coin_{id}");
            await _cacheService.RemoveAsync("coins_list");
            return NoContent();
        }

        // DELETE: api/Coins/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCoin(string id)
        {
            if (!ObjectId.TryParse(id, out _))
            {
                return BadRequest("Некорректный формат идентификатора.");
            }

            var result = await _coinsCollection.DeleteOneAsync(c => c.Id == id);
            if (result.DeletedCount == 0)
            {
                return NotFound();
            }

            // Удаляем кэш, так как данные изменились
            await _cacheService.RemoveAsync($"coin_{id}");
            await _cacheService.RemoveAsync("coins_list");
            return NoContent();
        }

        // GET: api/Coins/filter?country=Россия&year=1897&metal=Золото
        [HttpGet("filter")]
        public async Task<ActionResult<IEnumerable<CoinDto>>> FilterCoins(
            [FromQuery] string? country,
            [FromQuery] int? year,
            [FromQuery] string? metal,
            [FromQuery] string? ownerName)
        {
            // Строим ключ кэша из параметров
            var cacheKey = $"coins_filter_{country ?? "any"}_{year?.ToString() ?? "any"}_{metal ?? "any"}_{ownerName ?? "any"}";

            // Проверяем кэш
            var cachedCoins = await _cacheService.GetAsync<List<CoinDto>>(cacheKey);
            if (cachedCoins != null)
            {
                return Ok(cachedCoins);
            }

            // Строим фильтр для MongoDB
            var filterBuilder = Builders<MongoCoin>.Filter;
            var filters = new List<FilterDefinition<MongoCoin>>();

            if (!string.IsNullOrEmpty(country))
                filters.Add(filterBuilder.Eq(c => c.Country, country));
            if (year.HasValue)
                filters.Add(filterBuilder.Eq(c => c.Year, year.Value));
            if (!string.IsNullOrEmpty(metal))
                filters.Add(filterBuilder.Eq(c => c.Metal, metal));
            if (!string.IsNullOrEmpty(ownerName))
                filters.Add(filterBuilder.Eq(c => c.OwnerName, ownerName));

            var finalFilter = filters.Any() ? filterBuilder.And(filters) : filterBuilder.Empty;
            var coins = await _coinsCollection.Find(finalFilter).ToListAsync();

            var coinDtos = coins.Select(c => new CoinDto
            {
                Id = c.Id!,
                Country = c.Country,
                Year = c.Year,
                Nominal = c.Nominal,
                Metal = c.Metal,
                Price = c.Price,
                OwnerName = c.OwnerName,
                Description = c.Description,
                UserId = c.UserId,
                ConfirmationStatus = c.ConfirmationStatus,
                ConfirmedAt = c.ConfirmedAt,
                CreatedAt = c.CreatedAt
            }).ToList();

            // Сохраняем в кэш на 5 минут
            await _cacheService.SetAsync(cacheKey, coinDtos, TimeSpan.FromMinutes(5));

            return Ok(coinDtos);
        }

        // DELETE: api/Coins/all
        [HttpDelete("all")]
        public async Task<IActionResult> DeleteAllCoins()
        {
            var deleteResult = await _coinsCollection.DeleteManyAsync(_ => true);

            if (deleteResult.DeletedCount == 0)
            {
                return NotFound("В базе нет монет для удаления.");
            }

            await _cacheService.RemoveAsync("coins_list");

            return NoContent();
        }
    }
}
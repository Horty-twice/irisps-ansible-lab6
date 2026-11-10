using Microsoft.AspNetCore.Mvc;
using NumismaticClub.Lab1.Models;

namespace NumismaticClub.Lab1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoinsController : ControllerBase
    {
        // Временное хранилище (в памяти)
        private static List<Coin> _coins = new List<Coin>
        {
            new Coin
            {
                Id = 1,
                Country = "Российская Империя",
                Year = 1897,
                Nominal = "7 рублей 50 копеек",
                Metal = "Золото",
                Price = 150000,
                OwnerName = "Иванов",
                Description = "Золотая монета Николая II"
            },
            new Coin
            {
                Id = 2,
                Country = "СССР",
                Year = 1965,
                Nominal = "1 рубль",
                Metal = "Медно-никель",
                Price = 500,
                OwnerName = "Петров",
                Description = "Юбилейный рубль '20 лет Победы'"
            },
            new Coin
            {
                Id = 3,
                Country = "США",
                Year = 1921,
                Nominal = "1 доллар",
                Metal = "Серебро",
                Price = 30000,
                OwnerName = "Сидоров",
                Description = "Моргановский доллар"
            }
        };

        // GET: api/Coins
        [HttpGet]
        public ActionResult<IEnumerable<Coin>> GetCoins()
        {
            return Ok(_coins);
        }

        // GET: api/Coins/5
        [HttpGet("{id}")]
        public ActionResult<Coin> GetCoin(int id)
        {
            var coin = _coins.FirstOrDefault(c => c.Id == id);

            if (coin == null)
            {
                return NotFound();
            }

            return Ok(coin);
        }

        // POST: api/Coins
        [HttpPost]
        public ActionResult<Coin> PostCoin(CreateCoinDto createDto)
        {
            var newId = _coins.Max(c => c.Id) + 1;

            var coin = new Coin
            {
                Id = newId,
                Country = createDto.Country,
                Year = createDto.Year,
                Nominal = createDto.Nominal,
                Metal = createDto.Metal,
                Price = createDto.Price,
                OwnerName = createDto.OwnerName,
                Description = createDto.Description
            };

            _coins.Add(coin);
            return CreatedAtAction(nameof(GetCoin), new { id = coin.Id }, coin);
        }

        // PUT: api/Coins/5
        [HttpPut("{id}")]
        public IActionResult PutCoin(int id, CreateCoinDto updateDto)
        {
            var existingCoin = _coins.FirstOrDefault(c => c.Id == id);
            if (existingCoin == null)
            {
                return NotFound();
            }

            existingCoin.Country = updateDto.Country;
            existingCoin.Year = updateDto.Year;
            existingCoin.Nominal = updateDto.Nominal;
            existingCoin.Metal = updateDto.Metal;
            existingCoin.Price = updateDto.Price;
            existingCoin.OwnerName = updateDto.OwnerName;
            existingCoin.Description = updateDto.Description;

            return NoContent();
        }

        // DELETE: api/Coins/5
        [HttpDelete("{id}")]
        public IActionResult DeleteCoin(int id)
        {
            var coin = _coins.FirstOrDefault(c => c.Id == id);
            if (coin == null)
            {
                return NotFound();
            }

            _coins.Remove(coin);
            return NoContent();
        }
    }
}
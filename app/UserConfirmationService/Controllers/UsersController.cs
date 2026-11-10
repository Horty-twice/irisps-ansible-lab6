using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using UserConfirmationService.Models;
using UserConfirmationService.Services;

namespace UserConfirmationService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IMongoCollection<User> _usersCollection;

        public UsersController(MongoDbService mongoDbService)
        {
            _usersCollection = mongoDbService.Users;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserDto createDto)
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(createDto.Password);

            var user = new User
            {
                Name = createDto.Name,
                PasswordHash = passwordHash,
                RegisteredObjects = 0
            };

            await _usersCollection.InsertOneAsync(user);

            var response = new
            {
                user.Id,
                user.Name,
                user.RegisteredObjects
            };

            return CreatedAtAction(nameof(GetById), new { id = user.Id }, response);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _usersCollection.Find(_ => true).ToListAsync();
            var response = users.Select(u => new { u.Id, u.Name, u.RegisteredObjects });
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var user = await _usersCollection.Find(u => u.Id == id).FirstOrDefaultAsync();
            if (user == null) return NotFound();
            return Ok(new { user.Id, user.Name, user.RegisteredObjects });
        }
    }
}
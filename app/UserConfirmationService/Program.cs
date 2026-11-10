using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using System.Text;
using UserConfirmationService.Models;
using UserConfirmationService.Services;


var builder = WebApplication.CreateBuilder(args);

// Добавляем контроллеры
builder.Services.AddControllers();

// Добавляем Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Введите JWT-токен без слова Bearer."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Регистрируем фоновый Kafka consumer
builder.Services.AddHostedService<KafkaConsumerService>();

//Регистрируем MongoDbService
builder.Services.AddSingleton<MongoDbService>();

// Настройка JWT аутентификации
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuerSigningKey = true
    };
});

var app = builder.Build();

// Seed: создать пользователя admin при первом запуске
using (var scope = app.Services.CreateScope())
{
    var mongoDbService = scope.ServiceProvider.GetRequiredService<MongoDbService>();
    var users = mongoDbService.Users;

    var adminExists = await users.Find(u => u.Name == "admin").AnyAsync();
    if (!adminExists)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("admin123");
        await users.InsertOneAsync(new User
        {
            Name = "admin",
            PasswordHash = passwordHash,
            RegisteredObjects = 0
        });
        Console.WriteLine(">>> Seed: пользователь admin создан (admin/admin123)");
    }
}

// Swagger только в разработке
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAuthorization();
app.MapControllers();

app.Run();
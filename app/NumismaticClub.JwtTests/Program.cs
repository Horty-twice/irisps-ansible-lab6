using System.Text;
using System.Text.Json;

namespace NumismaticClub.JwtTests
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=== JWT Тесты для микросервисов ===\n");

            var userServiceUrl = "https://localhost:7140";
            var coinsServiceUrl = "https://localhost:7230";

            // 1. Получаем токен (логин)
            Console.WriteLine("1. Получение JWT токена...");
            var token = await GetTokenAsync(userServiceUrl, "admin", "admin123");
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("ОШИБКА: Не удалось получить токен. Проверьте логин/пароль и что UserConfirmationService запущен.");
                return;
            }
            Console.WriteLine($"Токен: {token}\n");

            // 2. Тест без токена (ожидаем 401)
            Console.WriteLine("2. Проверка доступа к GET /api/Coins без токена...");
            var statusNoToken = await GetStatusCodeAsync($"{coinsServiceUrl}/api/Coins", null);
            var noTokenPass = statusNoToken == 401;
            Console.WriteLine(noTokenPass
                ? "   УСПЕХ: 401 Unauthorized (доступ запрещён)"
                : $"   ОШИБКА: Ожидался 401, получен {statusNoToken}");

            // 3. Тест с валидным токеном (ожидаем 200)
            Console.WriteLine("\n3. Проверка доступа к GET /api/Coins с валидным токеном...");
            var statusValid = await GetStatusCodeAsync($"{coinsServiceUrl}/api/Coins", token);
            var validPass = statusValid == 200;
            Console.WriteLine(validPass
                ? "   УСПЕХ: 200 OK (доступ разрешён)"
                : $"   ОШИБКА: Ожидался 200, получен {statusValid}");

            // 4. Тест с фиктивным токеном (ожидаем 401)
            Console.WriteLine("\n4. Проверка доступа к GET /api/Coins с фиктивным токеном...");
            var fakeToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
            var statusFake = await GetStatusCodeAsync($"{coinsServiceUrl}/api/Coins", fakeToken);
            var fakePass = statusFake == 401;
            Console.WriteLine(fakePass
                ? "   УСПЕХ: 401 Unauthorized (фиктивный токен отвергнут)"
                : $"   ОШИБКА: Ожидался 401, получен {statusFake}");

            // Итог
            Console.WriteLine("\n=== РЕЗУЛЬТАТЫ ТЕСТОВ ===");
            Console.WriteLine($"Без токена: {(noTokenPass ? "ПРОЙДЕН" : "НЕ ПРОЙДЕН")}");
            Console.WriteLine($"С валидным токеном: {(validPass ? "ПРОЙДЕН" : "НЕ ПРОЙДЕН")}");
            Console.WriteLine($"С фиктивным токеном: {(fakePass ? "ПРОЙДЕН" : "НЕ ПРОЙДЕН")}");

            if (noTokenPass && validPass && fakePass)
                Console.WriteLine("\nУСПЕХ: Все тесты пройдены!");
            else
                Console.WriteLine("\nВНИМАНИЕ: Некоторые тесты не пройдены. Проверьте настройки JWT.");
        }

        static async Task<string?> GetTokenAsync(string baseUrl, string username, string password)
        {
            using var client = new HttpClient();
            var loginData = new { username, password };
            var content = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync($"{baseUrl}/api/Auth/login", content);
                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.GetProperty("token").GetString();
            }
            catch
            {
                return null;
            }
        }

        static async Task<int> GetStatusCodeAsync(string url, string? token)
        {
            using var client = new HttpClient();
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            try
            {
                var response = await client.GetAsync(url);
                return (int)response.StatusCode;
            }
            catch
            {
                return 0;
            }
        }
    }
}
using System.Text;
using System.Text.Json;

var apiBaseUrl = "https://localhost:7230/api/Coins";
using var httpClient = new HttpClient();

// Счётчики ошибок
int totalErrors = 0;

// ============================================================
// 1. Тест добавления 100 элементов
// ============================================================
Console.WriteLine("Тест 1: Добавление 100 элементов...");
int errors1 = 0;

for (int i = 1; i <= 100; i++)
{
    var newCoin = new
    {
        country = $"ТестоваяСтрана_{i}",
        year = 2000 + i % 24,
        nominal = $"{i} рублей",
        metal = i % 2 == 0 ? "Золото" : "Серебро",
        price = 1000 + i,
        ownerName = $"ТестовыйВладелец_{i}",
        description = $"Тестовая монета {i}"
    };

    var json = JsonSerializer.Serialize(newCoin);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await httpClient.PostAsync(apiBaseUrl, content);

    // ПРОВЕРКА: статус должен быть 201 Created
    if (response.StatusCode != System.Net.HttpStatusCode.Created)
    {
        errors1++;
        Console.WriteLine($"  Ошибка при добавлении монеты {i}: статус {response.StatusCode}");
    }

    if (i % 10 == 0)
        Console.WriteLine($"Добавлено {i} монет... (ошибок: {errors1})");
}

Console.WriteLine($"Тест 1 завершён. Успешно: {100 - errors1}/100, Ошибок: {errors1}");
if (errors1 > 0) totalErrors += errors1;
Console.WriteLine("Нажмите Enter для следующего теста.");
Console.ReadLine();

// ============================================================
// 2. Тест добавления 100 000 элементов (выборочная проверка)
// ============================================================
Console.WriteLine("\nТест 2: Добавление 100 000 элементов...");
Console.WriteLine("Это займёт некоторое время...");
int errors2 = 0;
int successCount = 0;

for (int i = 1; i <= 100000; i++)
{
    var newCoin = new
    {
        country = $"Страна_{i % 50}",
        year = 1900 + i % 125,
        nominal = $"{i % 100} копеек",
        metal = (i % 3) switch
        {
            0 => "Золото",
            1 => "Серебро",
            _ => "Бронза"
        },
        price = i % 10000,
        ownerName = $"Владелец_{i % 100}",
        description = $"Массовый тест {i}"
    };

    var json = JsonSerializer.Serialize(newCoin);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await httpClient.PostAsync(apiBaseUrl, content);

    // ПРОВЕРЯЕМ КАЖДЫЙ ЗАПРОС
    if (response.StatusCode == System.Net.HttpStatusCode.Created)
    {
        successCount++;
    }
    else
    {
        errors2++;
        // Выводим только первые 10 ошибок, чтобы не завалить консоль
        if (errors2 <= 10)
        {
            Console.WriteLine($"  Ошибка при добавлении монеты {i}: статус {response.StatusCode}");
        }
    }

    // Прогресс каждые 10000
    if (i % 10000 == 0)
    {
        Console.WriteLine($"Добавлено {i} монет... (успешно: {successCount}, ошибок: {errors2})");
    }
}

Console.WriteLine($"Тест 2 завершён. Успешно: {successCount}/100000, Ошибок: {errors2}");
Console.WriteLine("Нажмите Enter для следующего теста.");
Console.ReadLine();

// ============================================================
// 3. Тест удаления всех элементов
// ============================================================
Console.WriteLine("\nТест 3: Удаление всех элементов...");

try
{
    var deleteAllResponse = await httpClient.DeleteAsync($"{apiBaseUrl}/all");

    // ПРОВЕРКА: статус должен быть 204 No Content
    if (deleteAllResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
    {
        Console.WriteLine("✓ Все монеты успешно удалены из базы данных (статус 204).");
    }
    else if (deleteAllResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        Console.WriteLine("! База данных уже была пуста (статус 404).");
    }
    else
    {
        errors2++;
        Console.WriteLine($"✗ Ошибка при удалении: статус {deleteAllResponse.StatusCode}");
    }
}
catch (Exception ex)
{
    errors2++;
    Console.WriteLine($"✗ Исключение при удалении: {ex.Message}");
}

Console.WriteLine($"Тест 3 завершён.");

// ============================================================
// ИТОГОВЫЙ ОТЧЁТ
// ============================================================
Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine("ИТОГОВЫЙ ОТЧЁТ ПО ТЕСТАМ");
Console.WriteLine(new string('=', 50));

if (totalErrors == 0)
{
    Console.WriteLine("✅ ВСЕ ТЕСТЫ ПРОЙДЕНЫ УСПЕШНО!");
    Console.WriteLine("   - 100 добавлений: успешно");
    Console.WriteLine("   - 100 000 добавлений: успешно");
    Console.WriteLine("   - Массовое удаление: успешно");
}
else
{
    Console.WriteLine($"❌ ТЕСТЫ ЗАВЕРШЕНЫ С {totalErrors} ОШИБКАМИ");
}

Console.WriteLine("\nНажмите любую клавишу для выхода...");
Console.ReadKey();
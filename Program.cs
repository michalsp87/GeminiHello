using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Serilog;

// 0. Logger: zapis do pliku tekstowego (logs/gemini-YYYYMMDD.log), tylko ważne zdarzenia
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(
        path: Path.Combine("logs", "gemini-.log"),
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Start aplikacji GeminiHello");

    // 1. Klucz ze zmiennej środowiskowej, nigdy z kodu (sam klucz nie trafia do logu)
    var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
    if (string.IsNullOrWhiteSpace(apiKey))
    {
        throw new InvalidOperationException("Brak zmiennej GEMINI_API_KEY");
    }
    Log.Information("Klucz API wczytany ze zmiennej środowiskowej");

    // 2. Adres endpointu – nazwę modelu skopiuj z AI Studio
    const string model = "gemini-3.5-flash";
    var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";

    using var http = new HttpClient();
    http.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);

    // 3. Treść zapytania: contents -> parts -> text
    var body = new
    {
        contents = new[]
        {
            new { parts = new[] { new { text = "Wyjaśnij w dwóch zdaniach, czym jest REST API." } } }
        }
    };

    // 4. Wysłanie i odczyt odpowiedzi
    Log.Information("Wysyłanie zapytania do modelu {Model}", model);
    var stopwatch = Stopwatch.StartNew();
    var response = await http.PostAsJsonAsync(url, body);
    var json = await response.Content.ReadAsStringAsync();
    Log.Information("Odpowiedź API: {StatusCode} w {ElapsedMs} ms", (int)response.StatusCode, stopwatch.ElapsedMilliseconds);

    if (!response.IsSuccessStatusCode)
    {
        Log.Error("Błąd API {StatusCode}: {Body}", (int)response.StatusCode, json);
        Console.WriteLine($"Błąd {(int)response.StatusCode}: {json}");
        return;
    }

    // 5. Wyciągnięcie tekstu z odpowiedzi
    var text = JsonNode.Parse(json)?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>();
    if (string.IsNullOrEmpty(text))
    {
        Log.Warning("Odpowiedź API nie zawiera tekstu");
    }
    else
    {
        Log.Information("Otrzymano odpowiedź modelu: {Text}", text);
    }
    Console.WriteLine(text);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplikacja zakończyła się błędem");
    Console.WriteLine($"Błąd: {ex.Message}");
}
finally
{
    Log.Information("Koniec aplikacji");
    Log.CloseAndFlush();
}

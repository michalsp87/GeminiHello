using System.Net.Http.Json;
using System.Text.Json.Nodes;

// 1. Klucz ze zmiennej środowiskowej, nigdy z kodu
var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
    ?? throw new InvalidOperationException("Brak zmiennej GEMINI_API_KEY");

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
var response = await http.PostAsJsonAsync(url, body);
var json = await response.Content.ReadAsStringAsync();

if (!response.IsSuccessStatusCode)
{
    Console.WriteLine($"Błąd {(int)response.StatusCode}: {json}");
    return;
}

// 5. Wyciągnięcie tekstu z odpowiedzi
var text = JsonNode.Parse(json)?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>();
Console.WriteLine(text);
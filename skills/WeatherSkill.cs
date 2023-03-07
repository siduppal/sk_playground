using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.SemanticKernel.SkillDefinition;

public class WeatherSkill
{
    private const string baseUrl = "https://atlas.microsoft.com/weather/currentConditions/json";
    private readonly string apiKey = "YOUR_API_KEY"; // Replace with your actual API key

    public WeatherSkill(string key)
    {
        this.apiKey = key;
    }

    [SKFunction("Search for weather at specified location.")]
    public async Task<string> GetWeather(string location)
    {
        // TODO: The location has to be a lat,long string.

        // Make sure to URL encode the location string.
        location = Uri.EscapeDataString(location);
        string url = $"{baseUrl}?api-version=1.1&query={location}&subscription-key={apiKey}";

        using (HttpClient client = new HttpClient())
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                JObject json = JObject.Parse(responseBody);
                JToken temperatureToken = json.SelectToken("results[0].temperature.value");
                JToken phraseToken = json.SelectToken("results[0].phrase");

                double temperature = temperatureToken.Value<double>();
                string phrase = phraseToken.Value<string>();

                return $"The weather in {location} is {phrase} with a temperature of {temperature}°C.";
            }
            catch (HttpRequestException e)
            {
                return $"Failed to retrieve weather information: {e.Message}";
            }
            catch (Exception e)
            {
                return $"An error occurred: {e.Message}";
            }
        }
    }
}

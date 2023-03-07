using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using Microsoft.SemanticKernel.SkillDefinition;

namespace SK_Playground.Skills
{
    public class YouTubeSearchSkill
    {
        private readonly HttpClient httpClient;
        private readonly string apiKey;

        public YouTubeSearchSkill(string key)
        {
            httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/");
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            this.apiKey = key;
        }

        [SKFunction("Search for videos on YouTube.")]
        public string Search(string query)
        {
            string url = $"search?part=snippet&maxResults=10&q={query}&key={apiKey}";

            HttpResponseMessage response = httpClient.GetAsync(url).Result;

            if (!response.IsSuccessStatusCode)
            {
                return $"Failed to search for videos: {response.StatusCode}";
            }

            JObject jsonResponse = JObject.Parse(response.Content.ReadAsStringAsync().Result);
            JArray items = (JArray)jsonResponse["items"];

            if (items.Count == 0)
            {
                return "No videos found.";
            }

            string result = "";

            foreach (JToken item in items)
            {
                string videoId = item["id"]["videoId"].ToString();
                string videoTitle = item["snippet"]["title"].ToString();
                result += $"{videoTitle}: https://www.youtube.com/watch?v={videoId}\n\n";
            }

            return result.Trim();
        }
    }
}

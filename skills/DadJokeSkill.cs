using Microsoft.SemanticKernel.SkillDefinition;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SK_Playground.Skills
{
    /// <summary>
    /// DadJokeSkill provides a function to retrieve a random dad joke from the icanhazdadjoke API.
    /// </summary>
    /// <example>
    /// Usage: kernel.ImportSkill("dadjoke", new DadJokeSkill());
    ///
    /// Example:
    /// {{dadjoke.getJoke}}
    /// </example>
    public class DadJokeSkill
    {
        /// <summary>
        /// Retrieve a random dad joke from the icanhazdadjoke API.
        /// </summary>
        /// <example>
        /// {{dadjoke.getJoke}}
        /// </example>
        /// <returns> A string containing a dad joke. </returns>
        [SKFunction("Retrieve a random dad joke from the icanhazdadjoke API.")]
        public async Task<string> GetJoke()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.GetAsync("https://icanhazdadjoke.com/");

                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();

                var jokeObject = JsonConvert.DeserializeObject<Joke>(responseContent);

                return jokeObject.joke;
            }
        }

        private class Joke
        {
            public string id { get; set; }
            public string joke { get; set; }
            public int status { get; set; }
        }
    }
}

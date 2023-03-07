using Microsoft.Extensions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Configuration;
using Microsoft.SemanticKernel.SemanticFunctions;
using Microsoft.SemanticKernel.KernelExtensions;
using Microsoft.SemanticKernel.CoreSkills;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Orchestration.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Skills.Web;
using Microsoft.SemanticKernel.Skills.Web.Bing;
using System.IO;
using System;
using SK_Playground;
using SK_Playground.Skills;

namespace SK_Playground
{
    class Program
    {

        private static IKernel BuildNewKernel()
        {

            var kernel = Kernel.Builder
            .WithLogger(ConsoleLogger.Log)
            .Configure(c => 
            {
                c.AddOpenAICompletionBackend(
                "davinci-backend",                   // Alias used by the kernel
                "text-davinci-003",                  // OpenAI *Model ID*
                Environment.GetEnvironmentVariable("OPENAI_API_KEY") // OpenAI *Key*
            );
            }).Build();

            return kernel;
        }

        private static async Task<string> HandleUserUtterance(string userInput)
        {
            var kernel = BuildNewKernel();

            // Load native skill into the kernel registry, sharing its functions with prompt templates
            var planner = kernel.ImportSkill(new PlannerSkill(kernel));

            var skillsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "skills");
            kernel.ImportSemanticSkillFromDirectory(skillsDirectory, "SummarizeSkill");
            kernel.ImportSemanticSkillFromDirectory(skillsDirectory, "WriterSkill");

            // Load custom skill into the kernel registry, sharing its functions with prompt templates            
            kernel.ImportSkill(new DadJokeSkill());
            kernel.ImportSkill(new YouTubeSearchSkill(Environment.GetEnvironmentVariable("YOUTUBE_API_KEY")));
            kernel.ImportSkill(new WeatherSkill(Environment.GetEnvironmentVariable("BINGMAPS_API_KEY")));

            using var bingConnector = new BingConnector(apiKey: Environment.GetEnvironmentVariable("BING_API_KEY"));
            var webSearchEngineSkill = new WebSearchEngineSkill(bingConnector);
            var web = kernel.ImportSkill(webSearchEngineSkill);

            var originalPlan = await kernel.RunAsync(userInput, planner["CreatePlan"]);

            Console.WriteLine("Original plan:\n");
            Console.WriteLine(originalPlan.Variables.ToPlan().PlanString);

            var executionResults = originalPlan;

            int step = 1;
            int maxSteps = 10;
            while (!executionResults.Variables.ToPlan().IsComplete && step < maxSteps)
            {
                var results = await kernel.RunAsync(executionResults.Variables, planner["ExecutePlan"]);
                if (results.Variables.ToPlan().IsSuccessful)
                {
                    Console.WriteLine($"Step {step} - Execution results:\n");
                    Console.WriteLine(results.Variables.ToPlan().PlanString);

                    if (results.Variables.ToPlan().IsComplete)
                    {
                        Console.WriteLine($"Step {step} - COMPLETE!");
                        Console.WriteLine(results.Variables.ToPlan().Result);
                        break;
                    }
                }
                else
                {
                    Console.WriteLine($"Step {step} - Execution failed:");
                    Console.WriteLine(results.Variables.ToPlan().Result);
                    break;
                }
                
                executionResults = results;
                step++;
                Console.WriteLine("");
            }

            return executionResults.Variables.ToPlan().Result;

        }

        private static async Task<string> SummarizeText(string textToSummarize)
        {

            var kernel = BuildNewKernel();

            string skPrompt = @"
            {{$input}}

            Give me the TLDR in 5 words.
            ";

            var tldrFunction = kernel.CreateSemanticFunction(skPrompt);

            var summary = await kernel.RunAsync(textToSummarize, tldrFunction);

            return summary.ToString();
        }

        static void TestSummarization()
        {
            string textToSummarize = @"
1) A robot may not injure a human being or, through inaction,
allow a human being to come to harm.

2) A robot must obey orders given it by human beings except where
such orders would conflict with the First Law.

3) A robot must protect its own existence as long as such protection
does not conflict with the First or Second Law.
";

            var summary = SummarizeText(textToSummarize).Result;

            Console.WriteLine(summary);
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to the Semantic Kernel demo! Type 'exit' to quit.");


            //var ask = "Tomorrow is Valentine's day. I need to come up with a few date ideas and e-mail them to my significant other.";
            //var ask = "Tell me a dad joke.";
            // var ask = "Search for videos about last night's laker's game";
           // var ask = "Weather in Seattle, WA";
            //var result = HandleUserUtterance(ask).Result;
            //Console.WriteLine(result);

            while (true) 
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.Write("Human: ");
                Console.ResetColor();
                var userInput = Console.ReadLine();

                if (! string.IsNullOrWhiteSpace(userInput)) {
                    if (userInput == "exit") {
                        break;
                    }
                    var result = HandleUserUtterance(userInput).Result;
                    Console.WriteLine(result);
                }
            }

        }
    }
}
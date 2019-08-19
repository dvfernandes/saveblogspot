using System;
using System.Threading.Tasks;
using BlogspotToHtmlBook.Infrastructure;
using BlogspotToStaticWeb.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace BlogspotToStaticWeb
{
    class Program
    {
        public async static Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                              .AddJsonFile("appsettings.json")
                              .Build();

            var outputFolder = configuration["OutputFolder"];
            var blogspotUrl = configuration["BlogspotUrl"];

            if(String.IsNullOrEmpty(outputFolder) || String.IsNullOrEmpty(blogspotUrl)){
                Console.WriteLine("appsettings.json must have OutputFolder and BlogspotUrl set.");
                return;
            }

            var job = new Job(new ConsoleLogger(), new FileSystem(outputFolder));

            await job.Work(blogspotUrl);
        }
    }
}

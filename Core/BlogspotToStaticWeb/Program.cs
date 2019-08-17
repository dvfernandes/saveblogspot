using System;
using BlogspotToHtmlBook.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace BlogspotToStaticWeb
{
    class Program
    {
        static void Main(string[] args)
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

            var job = Job.GetInstance();

            job.Work(outputFolder, blogspotUrl, new Logger());
        }
    }
}

using System;
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
            var bloggerRssFeed = configuration["BloggerRssFeed"];

            if(String.IsNullOrEmpty(outputFolder) || String.IsNullOrEmpty(bloggerRssFeed)){
                Console.WriteLine("appsettings.json must have OutputFolder and BloggerRssFeed set.");
                return;
            }

            var job = Job.GetInstance();

            job.Work(outputFolder, bloggerRssFeed);
        }
    }
}

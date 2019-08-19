using System;

namespace BlogspotToHtmlBook.Infrastructure {
    public class ConsoleLogger : ILogger {
        public void Debug(string message) {
            Console.WriteLine(message);
        }
    }
}

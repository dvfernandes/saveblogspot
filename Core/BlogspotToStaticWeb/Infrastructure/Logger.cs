using System;

namespace BlogspotToHtmlBook.Infrastructure {
    public class Logger : ILogger {
        public void Debug(string message) {
            Console.WriteLine(message);
        }
    }
}

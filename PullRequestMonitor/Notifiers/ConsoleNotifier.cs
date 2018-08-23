using System;
using System.Threading.Tasks;

namespace PullRequestMonitor.Notifiers
{
    public class ConsoleNotifier : IPullRequestNotifier
    {
        public Task Notify(PullRequestNotification notification)
        {
            ConsoleColor color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"[{DateTime.Now:T}] ");
            Console.ForegroundColor = color;
            Console.WriteLine(notification);
            return Task.CompletedTask;
        }
    }
}

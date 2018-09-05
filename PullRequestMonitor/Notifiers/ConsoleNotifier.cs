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
            Console.Write($"[{notification.UpdatedOn:G}] ");
            Console.ForegroundColor = color;
            Console.WriteLine(notification);
            return Task.CompletedTask;
        }
    }
}

using System.Threading.Tasks;

namespace PullRequestMonitor
{
    public class ConsoleNotifier : IPullRequestNotifier
    {
        public Task Notify(PullRequestNotification notification)
        {
            System.Console.WriteLine(notification.ToString());
            return Task.CompletedTask;
        }
    }
}

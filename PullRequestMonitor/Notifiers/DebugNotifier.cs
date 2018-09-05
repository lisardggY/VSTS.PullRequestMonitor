using System.Diagnostics;
using System.Threading.Tasks;

namespace PullRequestMonitor.Notifiers
{
    public class DebugNotifier : IPullRequestNotifier
    {
        public Task Notify(PullRequestNotification notification)
        {
            Debug.WriteLine(notification.ToString());
            return Task.CompletedTask;
        }
    }
}

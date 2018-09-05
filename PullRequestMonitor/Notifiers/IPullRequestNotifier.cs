using System.Threading.Tasks;

namespace PullRequestMonitor.Notifiers
{
    public interface IPullRequestNotifier
    {
        Task Notify(PullRequestNotification notification);
    }
}

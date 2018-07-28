using System.Threading.Tasks;

namespace PullRequestMonitor
{
    public interface IPullRequestNotifier
    {
        Task Notify(PullRequestNotification notification);
    }
}

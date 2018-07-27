using System;

namespace PullRequestMonitor
{
    internal class PullRequestNotification
    {
        public string Title {get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedOn { get; set; }
        public string NotificationType { get; set; }

        public override string ToString()
        {
            return $"{NotificationType} Pull Request By {CreatedBy}: {Title}";
        }
    }
}
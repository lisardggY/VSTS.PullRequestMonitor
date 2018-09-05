using System;

namespace PullRequestMonitor
{
    public class PullRequestNotification
    {
        public int PullRequestId {get;set;}
        public string Title {get; set; }
        public string CreatedBy { get; set; }
        public string ChangedBy { get; set; }
        public string Repository { get; set; }
        public DateTime UpdatedOn { get; set; }
        public string NotificationType { get; set; }

        public override string ToString()
        {
            switch (NotificationType)
            {
                case NotificationTypes.Approved:
                    return $"{ChangedBy} approved {CreatedBy}'s Pull Request {Title} on {Repository}";
                default:
                    return $"{NotificationType} Pull Request On {Repository} By {CreatedBy}: {Title}";

            }

        }
    }

    public static class NotificationTypes
    {
        public const string Approved = nameof(Approved);
        public const string New = nameof(New);
        public const string Updated = nameof(Updated);
    }
}
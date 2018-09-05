using System;
using System.Collections.Generic;
using VSTS.Net.Models.Identity;

namespace PullRequestMonitor.Model
{
    public class MonitorSettings
    {
        public string Instance { get; set; }
        public string Project { get; set; }
        public string[] Repositories { get; set; }
        public TimeSpan PollingInterval { get; set; }

        public UserNameFormat UserNameFormat { get; set; }
        public Func<IdentityReference, string> CustomUserNameFormat { get; set; }
        public HashSet<string> IgnoredReviewers { get; set; }
    }
}
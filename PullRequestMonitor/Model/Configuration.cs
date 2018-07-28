using System;
using System.Configuration;

namespace PullRequestMonitor
{
    public class ConnectionSettings
    {
        public string Instance { get; set; }
        public string Project { get; set; }
        public string Repository { get; set; }
        public TimeSpan PollingInterval { get; set; }
    }
}
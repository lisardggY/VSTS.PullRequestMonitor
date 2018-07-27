using System;
using System.Configuration;

namespace PullRequestMonitor
{
    internal static class Configuration
    {
        public static string Instance => ConfigurationManager.AppSettings["instance"];
        public static string Project => ConfigurationManager.AppSettings["project"];
        public static string Repository => ConfigurationManager.AppSettings["repository"];

        public static TimeSpan PollingInterval =>
            TimeSpan.FromSeconds(int.Parse(ConfigurationManager.AppSettings["pollingIntervalSec"] ?? "30"));
        
        public static DateTime LastChecked
        {
            get => Settings.Default.LastChecked;
            set
            {
                Settings.Default.LastChecked = value;
                Settings.Default.Save();
            }
        }

        
    }
}
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using Windows.UI.Notifications;
using VSTS.Net;
using VSTS.Net.Models.PullRequests;
using VSTS.Net.Models.Request;
using Windows.Data.Xml.Dom;

namespace VSTSMonitor
{
    public static class Program
    {
        static void Main(string[] args)
        {
            if (Debugger.IsAttached)
                AllocConsole();

            if (args.FirstOrDefault() == "all") 
                LastChecked = DateTime.MinValue;
            
            var execution = Task.Run(ConnectAndMonitor);
            execution.Wait();

        }

        private static string Instance => ConfigurationManager.AppSettings["instance"];
        private static string Project => ConfigurationManager.AppSettings["project"];

        private static DateTime LastChecked
        {
            get => VSTSMonitor.Default.LastChecked;
            set
            {
                VSTSMonitor.Default.LastChecked = value;
                VSTSMonitor.Default.Save();
            }
        }

        static async Task ConnectAndMonitor()
        {
            
            while (true)
            {
                var client = VstsClient.Get(Instance, await Authentication.GetAccessToken());
                await client.MonitorPullRequests(LastChecked);

                LastChecked = DateTime.UtcNow;
                
                await Task.Delay(TimeSpan.FromSeconds(30));
            }
            


        }

        public static async Task MonitorPullRequests(this VstsClient client, DateTime lastChecked)
        {
            var newPullRequests = await client.GetPullRequestsAsync(Project, Project, new PullRequestQuery
            {
                CreatedAfter = lastChecked
            });

            Notify(newPullRequests);

         
        }

        private static ToastNotifier _notifier = ToastNotificationManager.CreateToastNotifier("0");
        private static readonly SpeechSynthesizer _speechSynthesizer = new System.Speech.Synthesis.SpeechSynthesizer();

        private const string NotificationTemplate = "<toast>"
                                                      + "<visual>"
                                                      + "<binding template='ToastText02'>"
                                                      + "<text id='1'>{0}</text>"
                                                      + "<text id='2'>{1}</text>"
                                                      + "</binding>"
                                                      + "</visual>"
                                                      + "</toast>";

        private static void Notify(IEnumerable<PullRequest> prs)
        {
            if (prs == null)
                return;

            foreach (var pr in prs)
            {
                _speechSynthesizer.Speak($"New Pull Request By {pr.CreatedBy.DisplayName}: {pr.Title}");
            }
        }

        private static ToastNotification BuildNotification(PullRequest pr)
        {
            var notificationData = string.Format(NotificationTemplate, pr.Title, pr.CreatedBy.DisplayName);
            var notificationBlob = new XmlDocument();
            notificationBlob.LoadXml(notificationData);
            return new ToastNotification(notificationBlob) {Tag = pr.PullRequestId.ToString()};
        }

        
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
    }
}

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
            #if DEBUG
              var s = AllocConsole();
            #endif

            Console.WriteLine($"VSTSMonitor v{Assembly.GetExecutingAssembly().GetName().Version.ToString(3)}");

            if (args.FirstOrDefault() == "all") 
                LastChecked = DateTime.MinValue;
            
            var execution = Task.Run(ConnectAndMonitor);
            execution.Wait();

        }

        private static string Instance => ConfigurationManager.AppSettings["instance"];
        private static string Project => ConfigurationManager.AppSettings["project"];
        private static string Repository => ConfigurationManager.AppSettings["repository"];

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
            var (authToken, userInfo) = await Authentication.GetAccessToken();
            var client = VstsClient.Get(Instance, authToken);
            Console.WriteLine($"Connecting to https://{Instance}.visualstudio.com/{Project} as {userInfo.DisplayableId}" );
            while (true)
            {
                try
                {
                    await client.MonitorPullRequests(LastChecked);
                    LastChecked = DateTime.UtcNow;

                    await Task.Delay(TimeSpan.FromSeconds(30));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        public static async Task MonitorPullRequests(this VstsClient client, DateTime lastChecked)
        {
            var pullRequests = await client.GetPullRequestsAsync(Project, Repository, new PullRequestQuery());

            var notifications = await FilterPullRequests(pullRequests, client);
            
            Notify(notifications);

         
        }

        private static async Task<IEnumerable<PullRequestNotification>> FilterPullRequests(IEnumerable<PullRequest> pullRequests, VstsClient client)
        {
            List<PullRequestNotification> notifications = new List<PullRequestNotification>();
            foreach (var pr in pullRequests)
            {
                if (pr.CreationDate >= LastChecked)
                {
                    notifications.Add( new PullRequestNotification
                    {
                        Title = pr.Title,
                        CreatedBy = pr.CreatedBy.DisplayName,
                        UpdatedOn = pr.CreationDate,
                        NotificationType = "New"
                    });

                }
                else
                {
                    var iterations = await client.GetPullRequestIterationsAsync(Project, Repository, pr.PullRequestId);
                    var newest = iterations.OrderByDescending(iteration => iteration.CreatedDate).FirstOrDefault();
                    if (newest?.CreatedDate >= LastChecked)
                    {
                        notifications.Add(new PullRequestNotification
                        {
                            Title = pr.Title,
                            CreatedBy = newest.Author.DisplayName,
                            UpdatedOn = newest.CreatedDate,
                            NotificationType = "Updated"
                        });
                    }
                }
            }

            return notifications;
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

        private static void Notify(IEnumerable<PullRequestNotification> notifications)
        {
            if (notifications == null)
                return;

            foreach (var pr in notifications)
            {
                if (VSTSMonitor.Default.TextToSpeech)
                {
                    _speechSynthesizer.Speak($"{pr.NotificationType} Pull Request By {pr.CreatedBy}: {pr.Title}");
                }
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

    internal class PullRequestNotification
    {
        public string Title {get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedOn { get; set; }
        public string NotificationType { get; set; }
    }
}

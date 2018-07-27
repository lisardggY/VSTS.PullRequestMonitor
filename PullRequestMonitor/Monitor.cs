using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using VSTS.Net;
using VSTS.Net.Models.PullRequests;
using VSTS.Net.Models.Request;

namespace PullRequestMonitor
{
    public class Monitor
    {
        public static async Task ConnectAndMonitor()
        {
            while (true)
            {
                try
                {
                    var (authToken, userInfo) = await Authentication.GetAccessToken();
                    var client = VstsClient.Get(Configuration.Instance, authToken);
                    await MonitorPullRequests(client, Configuration.LastChecked);
                    Configuration.LastChecked = DateTime.UtcNow;

                    await Task.Delay(Configuration.PollingInterval);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public static async Task MonitorPullRequests(VstsClient client, DateTime lastChecked)
        {
            var pullRequests = await client.GetPullRequestsAsync(Configuration.Project, Configuration.Repository, new PullRequestQuery());

            var notifications = await FilterPullRequests(pullRequests, client);

            Notify(notifications);

         
        }

        private static async Task<IEnumerable<PullRequestNotification>> FilterPullRequests(IEnumerable<PullRequest> pullRequests, VstsClient client)
        {
            List<PullRequestNotification> notifications = new List<PullRequestNotification>();
            foreach (var pr in pullRequests)
            {
                if (pr.CreationDate >= System.Configuration.Configuration.LastChecked)
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
                    var iterations = await client.GetPullRequestIterationsAsync(System.Configuration.Configuration.Project, System.Configuration.Configuration.Repository, pr.PullRequestId);
                    var newest = iterations.OrderByDescending(iteration => iteration.CreatedDate).FirstOrDefault();
                    if (newest?.CreatedDate >= System.Configuration.Configuration.LastChecked)
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
                Console.WriteLine(pr.ToString());
                if (Settings.Default.TextToSpeech)
                {
                    _speechSynthesizer.Speak(pr.ToString());
                }
                
                
            }
        }

        private static ToastNotification BuildNotification(PullRequest pr)
        {
            var notificationData = String.Format(NotificationTemplate, pr.Title, pr.CreatedBy.DisplayName);
            var notificationBlob = new XmlDocument();
            notificationBlob.LoadXml(notificationData);
            return new ToastNotification(notificationBlob) {Tag = pr.PullRequestId.ToString()};
        }
    }
}

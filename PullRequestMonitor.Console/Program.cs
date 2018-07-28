using System;
using System.Configuration;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PullRequestMonitor.Console
{
    public static class Program
    {
        private static IPullRequestNotifier[] _notifiers;

        private static async Task Main(string[] args)
        {
            #if DEBUG
              AllocConsole();
            #endif

            System.Console.WriteLine($"VSTSMonitor v{Assembly.GetExecutingAssembly().GetName().Version.ToString(3)}");

            _notifiers = InitializeNotifiers();

            var settings = new ConnectionSettings
            {
                Instance = ConfigurationManager.AppSettings["instance"],
                Project = ConfigurationManager.AppSettings["project"],
                Repository = ConfigurationManager.AppSettings["repository"],
                PollingInterval =
                    TimeSpan.FromSeconds(int.Parse(ConfigurationManager.AppSettings["pollingIntervalSec"] ?? "30"))};
            var monitor = new Monitor(settings);
            monitor.OnNotification.Subscribe(Notify);
            await monitor.StartMonitoring();
        }

        private static IPullRequestNotifier[] InitializeNotifiers()
        {
            return new IPullRequestNotifier[]
            {
                new ConsoleNotifier(),
                new TextToSpeechNotifier()
            };
        }

        private static void Notify(PullRequestNotification notification)
        {
            foreach (var notifier in _notifiers)
            {
                notifier.Notify(notification);
            }
        }


        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
    }
}

﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using PullRequestMonitor.Model;
using PullRequestMonitor.Notifiers;

namespace PullRequestMonitor.Console
{
    public static class Program
    {
        private static IPullRequestNotifier[] _notifiers;

        private static async Task Main()
        {
            #if DEBUG
              AllocConsole();
            #endif

             System.Console.WriteLine($"PullRequestMonitor.Console v{Assembly.GetExecutingAssembly().GetName().Version.ToString(3)}");

            _notifiers = InitializeNotifiers();

            var settings = new MonitorSettings
            {
                Instance = ConfigurationManager.AppSettings["instance"],
                Project = ConfigurationManager.AppSettings["project"],
                Repositories = ConfigurationManager.AppSettings["repository"].Split(';'),
                PollingInterval =
                    TimeSpan.FromSeconds(int.Parse(ConfigurationManager.AppSettings["pollingIntervalSec"] ?? "30")),
                UserNameFormat = UserNameFormat.FirstName,
                IgnoredReviewers = new HashSet<string>(ConfigurationManager.AppSettings["ignoredReviewers"].Split(';'))
            };

            var monitor = new Monitor(settings);
            monitor.OnNotification.Subscribe(Notify);
            monitor.OnError.Subscribe(OnError);
            //HandleConsoleInterrupt(() => monitor.StopMonitoring());
            await monitor.StartMonitoring();
        }

        private static void HandleConsoleInterrupt(Action onSignalReceived)
        {
            SetConsoleCtrlHandler(sig =>
            {
                onSignalReceived();
                return true;
            }, true);
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

        private static void OnError(Exception exception)
        {
            System.Console.Error.WriteLine(exception.Message);
        }


        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandlerDelegate handler, bool add);
        private delegate bool ConsoleCtrlHandlerDelegate(int sig);

    }
}

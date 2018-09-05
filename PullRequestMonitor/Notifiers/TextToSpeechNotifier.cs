using System.Speech.Synthesis;
using System.Threading.Tasks;

namespace PullRequestMonitor.Notifiers
{
    public class TextToSpeechNotifier : IPullRequestNotifier
    {
        public SpeechSynthesizer Synthesizer { get; } = new SpeechSynthesizer();

        public Task Notify(PullRequestNotification notification)
        {
            Synthesizer.Speak(notification.ToString());

            return Task.CompletedTask;
        }
    }
}

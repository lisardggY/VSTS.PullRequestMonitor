using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using PullRequestMonitor.Extensions;
using PullRequestMonitor.Model;
using VSTS.Net;
using VSTS.Net.Models.Identity;
using VSTS.Net.Models.PullRequests;
using VSTS.Net.Models.Request;
using VSTS.Net.Types;

namespace PullRequestMonitor
{
    public class Monitor
    {
        private readonly MonitorSettings _configuration;
        private readonly Subject<PullRequestNotification> _notifier;
        private readonly Subject<Exception> _errorNotifier;
        private CancellationTokenSource _cts;

        public Monitor(MonitorSettings monitorSettings)
        {
            _configuration = monitorSettings;
            _notifier = new Subject<PullRequestNotification>();
            _errorNotifier = new Subject<Exception>();
        }

        private DateTime LastChecked
        {
            get => Settings.Default.LastChecked;
            set
            {
                Settings.Default.LastChecked = value;
                Settings.Default.Save();
            }
        }

        public IObservable<PullRequestNotification> OnNotification => _notifier.AsObservable();
        public IObservable<Exception> OnError => _errorNotifier.AsObservable();

        public async Task StartMonitoring()
        {
            // If monitoring has already started, do nothing
            if (_cts != null && !_cts.IsCancellationRequested)
                return;

            _cts = new CancellationTokenSource();
            var cancellationToken = _cts.Token;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var (authToken, _) = await Authentication.GetAccessToken();
                    var client = VstsClient.Get(new OnlineUrlBuilderFactory(_configuration.Instance), authToken);
                    await MonitorPullRequests(client, LastChecked, cancellationToken);
                    LastChecked = DateTime.UtcNow;

                    await Task.Delay(_configuration.PollingInterval, cancellationToken);
                }
                catch (Exception ex)
                {
                    _errorNotifier.OnNext(ex);
                }
            }
        }

        public void StopMonitoring()
        {
            _cts?.Cancel();
        }

        public async Task MonitorPullRequests(VstsClient client, DateTime lastChecked,
            CancellationToken cancellationToken)
        {
            foreach (var repository in _configuration.Repositories)
            {
            var allOpenPullRequests = (await client.GetPullRequestsAsync(_configuration.Project,
                repository, new PullRequestQuery(), cancellationToken)).ToArray();
            var newAndUpdated = await GetNewAndUpdatedPullRequests(client, repository, allOpenPullRequests, cancellationToken);
            var closedPullRequests = await GetClosedPullRequests(client, repository, cancellationToken);
            
            foreach (var notification in newAndUpdated.Concat(closedPullRequests))
                _notifier.OnNext(notification);
            }
        }

        private async Task<IEnumerable<PullRequestNotification>> GetNewAndUpdatedPullRequests(VstsClient client, string repository, PullRequest[] allOpenPullRequests, CancellationToken cancellationToken)
        {
            var newPullRequests = allOpenPullRequests.Where(pr => pr.CreationDate >= LastChecked).ToArray();
            var newPullRequestNotifications = newPullRequests
                .Select(pr =>
                    new PullRequestNotification
                    {
                        Repository = repository,
                        NotificationType = NotificationTypes.New,
                        PullRequestId = pr.PullRequestId,
                        Title = pr.Title,
                        CreatedBy = GetName(pr.CreatedBy),
                        UpdatedOn = pr.CreationDate
                    });

            var updatedPullRequestNotifications = await
                GetUpdatedPullRequests(client, repository, allOpenPullRequests.Except(newPullRequests), cancellationToken);
            return newPullRequestNotifications.Concat(updatedPullRequestNotifications);
        }
        
        private async Task<IEnumerable<PullRequestNotification>> GetUpdatedPullRequests(VstsClient client,
            string repository,
            IEnumerable<PullRequest> pullRequests,
            CancellationToken cancellationToken)
        {
            var prsAndPushes = await pullRequests
                                     .Select(async pr => (pr, await GetLatestIteration(client, repository, pr.PullRequestId, cancellationToken)))
                                     .WhenAll();
            return prsAndPushes
                   .Where (data => data.Item2.CreatedDate >= LastChecked)
                   .Select(data => BuildNotification(data.Item1, data.Item2));


            PullRequestNotification BuildNotification(PullRequest pr, PullRequestIteration push)
            {
                return new PullRequestNotification
                {
                    Repository = repository,
                    Title = pr.Title,
                    CreatedBy = GetName(pr.CreatedBy),
                    ChangedBy = GetName(push.Author),
                    UpdatedOn = push.CreatedDate,
                    NotificationType = NotificationTypes.Updated
                };
            }
        }

        private async Task<IEnumerable<PullRequestNotification>> GetClosedPullRequests(VstsClient client, string repository, CancellationToken cancellationToken)
        {
            var closedPullRequests = await client.GetPullRequestsAsync(_configuration.Project,
                repository,
                new PullRequestQuery()
                {
                    Status = "completed",
                    CustomFilter = pr => pr.ClosedDate.GetValueOrDefault() >= LastChecked
                },
                cancellationToken);

            return closedPullRequests.Select(pr => new PullRequestNotification
            {
                Repository = repository,
                PullRequestId = pr.PullRequestId,
                CreatedBy = GetName(pr.CreatedBy),
                ChangedBy = GetApprovers(pr.Reviewers),
                UpdatedOn = pr.ClosedDate.GetValueOrDefault(),
                Title = pr.Title,
                NotificationType = NotificationTypes.Approved
            });
        }

        private string GetApprovers(IEnumerable<IdentityReferenceWithVote> reviewers)
        {
            var realReviewers = reviewers.Where(reviewer => !reviewer.IsContainer).ToArray();
            if (realReviewers.Length == 1)
                return GetName(realReviewers.First());
            else
                return string.Join(" and ", realReviewers.Where(reviewer =>
                    !_configuration.IgnoredReviewers.Overlaps(new[]
                    {
                        reviewer.Name,
                        reviewer.DisplayName,
                        reviewer.UniqueName,
                        reviewer.Id.ToString()
                    })).Select(GetName));
        }


        private async Task<PullRequestIteration> GetLatestIteration(VstsClient client, string repository, int prId,
            CancellationToken cancellationToken)
        {
            var pushes = await client.GetPullRequestIterationsAsync(_configuration.Project, 
                repository,
                prId, cancellationToken);
            return pushes.OrderByDescending(iteration => iteration.CreatedDate).First();
        }

        private string GetName(IdentityReference identity)
        {
            switch (_configuration.UserNameFormat)
            {
                case UserNameFormat.DisplayName:
                    return identity?.DisplayName;
                case UserNameFormat.FirstName:
                    return identity?.DisplayName?.Split(' ').FirstOrDefault();
                case UserNameFormat.EmailAddress:
                    return identity?.UniqueName;
                case UserNameFormat.EmailAlias:
                    return identity?.UniqueName?.Split('@')[0];
                case UserNameFormat.Custom:
                    return _configuration.CustomUserNameFormat?.Invoke(identity);
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
        }

    }
}

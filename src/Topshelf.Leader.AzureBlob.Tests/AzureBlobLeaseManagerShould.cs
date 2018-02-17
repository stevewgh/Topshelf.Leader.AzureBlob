using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Xunit;

namespace Topshelf.Leader.AzureBlob.Tests
{
    [Trait("Category", "Integration")]
    public class AzureBlobLeaseManagerShould
    {
        [Fact]
        public void allow_only_one_lease_manager_to_own_the_mutex_at_any_given_time()
        {
            const int concurrentMutexes = 5;
            var leaseLength = TimeSpan.FromSeconds(15);

            using (var cts = new CancellationTokenSource())
            {
                var settings = new BlobSettings(CloudStorageAccount.DevelopmentStorageAccount, "integration", "onlyonegetsalease");
                var counter = 0;
                var managers = Enumerable.Range(0, concurrentMutexes)
                    .Select(completed => new AzureBlobLeaseManager(settings, leaseLength)).Select(manager =>
                        manager.AcquireLease(new LeaseOptions(counter++.ToString()), cts.Token)
                            .ConfigureAwait(false));

                var count = managers.Select(task => task.GetAwaiter().GetResult()).Count(b => b);

                Assert.Equal(1, count);
            }
        }

        [Fact]
        public async Task when_the_leader_renews_the_lease_another_node_cant_become_leader()
        {
            var settings = new BlobSettings(CloudStorageAccount.DevelopmentStorageAccount, "integration", "LeaderRenewing");

            var leaseLength = TimeSpan.FromSeconds(15);

            var firstLeader = new AzureBlobLeaseManager(settings, leaseLength);
            await firstLeader.AcquireLease(new LeaseOptions(nameof(firstLeader)), CancellationToken.None);
            await Task.Delay(leaseLength);

            await firstLeader.RenewLease(new LeaseOptions(nameof(firstLeader)), CancellationToken.None);

            var secondLeader = new AzureBlobLeaseManager(settings, leaseLength);
            Assert.False(await secondLeader.AcquireLease(new LeaseOptions(nameof(secondLeader)), CancellationToken.None));

            await firstLeader.ReleaseLease(new LeaseReleaseOptions(nameof(firstLeader)));
        }

        [Fact]
        public async Task when_the_leader_doesnt_renew_the_lease_another_node_can_become_leader()
        {
            var settings = new BlobSettings(CloudStorageAccount.DevelopmentStorageAccount, "integration", "LeaderRenewing");

            var leaseLength = TimeSpan.FromSeconds(15);
            var leaseExpiryWaitTime = TimeSpan.FromSeconds(5);

            var firstLeader = new AzureBlobLeaseManager(settings, leaseLength);
            await firstLeader.AcquireLease(new LeaseOptions(nameof(firstLeader)), CancellationToken.None);
            await Task.Delay(leaseLength + leaseExpiryWaitTime);

            var secondLeader = new AzureBlobLeaseManager(settings, leaseLength);
            Assert.True(await secondLeader.AcquireLease(new LeaseOptions(nameof(secondLeader)), CancellationToken.None));

            await secondLeader.ReleaseLease(new LeaseReleaseOptions(nameof(secondLeader)));
        }

        [Fact]
        public async Task when_the_leader_aborts_another_node_can_become_leader()
        {
            var settings = new BlobSettings(CloudStorageAccount.DevelopmentStorageAccount, "integration", "LeaderAbortingCreatesNewLeader");

            var tokenSource = new CancellationTokenSource();
            var firstLeader = new AzureBlobLeaseManager(settings, TimeSpan.FromSeconds(15));
            var secondLeader = new AzureBlobLeaseManager(settings, TimeSpan.FromSeconds(15));

            Assert.True(await firstLeader.AcquireLease(new LeaseOptions(nameof(firstLeader)), tokenSource.Token));
            Assert.False(await secondLeader.AcquireLease(new LeaseOptions(nameof(secondLeader)), tokenSource.Token));
            await firstLeader.ReleaseLease(new LeaseReleaseOptions(nameof(firstLeader)));

            Assert.True(await secondLeader.AcquireLease(new LeaseOptions(nameof(secondLeader)), tokenSource.Token));
        }
    }
}
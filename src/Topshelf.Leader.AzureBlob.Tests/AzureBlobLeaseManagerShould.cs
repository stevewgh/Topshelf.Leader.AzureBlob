using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Xunit;

namespace Topshelf.Leader.AzureBlob.Tests
{
    public class AzureBlobLeaseManagerShould
    {
        [Fact]
        [Trait("Category", "Integration")]
        public void allow_only_one_lease_manager_to_own_the_mutex_at_any_given_time()
        {
            const int concurrentMutexes = 5;
            var leaseLength = TimeSpan.FromSeconds(5);

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
        [Trait("Category", "Integration")]
        public void LeaderRenewsLease()
        {
            //const int ConcurrentMutexes = 5;
            //var settings = new BlobSettings(CloudStorageAccount.DevelopmentStorageAccount, "leases", "LeaderRenewsLease");

            //var mutexAcquired = Enumerable.Range(0, ConcurrentMutexes).Select(_ => new TaskCompletionSource<bool>()).ToArray();

            //var mutexes = mutexAcquired.Select(completed => new BlobDistributedMutex(settings, SignalAndWait(completed))).ToArray();

            //var cts = new CancellationTokenSource();

            //foreach (var mutex in mutexes)
            //{
            //    mutex.RunTaskWhenMutexAcquired(cts.Token);
            //}

            //bool allFinished = Task.WaitAll(mutexAcquired.Select(x => (Task)x.Task).ToArray(), TimeSpan.FromMinutes(3));

            //cts.Cancel();

            //Assert.IsFalse(allFinished);
            //Assert.AreEqual(1, mutexAcquired.Count(x => x.Task.IsCompleted));
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void LeaderAbortingCreatesNewLeader()
        {
            //const int ConcurrentMutexes = 5;
            //var settings = new BlobSettings(CloudStorageAccount.DevelopmentStorageAccount, "leases", "LeaderAbortingCreatesNewLeader");

            //var firstCts = new CancellationTokenSource();
            //var firstMutexAcquired = new TaskCompletionSource<bool>();
            //var firstLeader = new BlobDistributedMutex(settings, SignalAndWait(firstMutexAcquired));
            //firstLeader.RunTaskWhenMutexAcquired(firstCts.Token);

            //Assert.IsTrue(firstMutexAcquired.Task.Wait(TimeSpan.FromSeconds(5)));

            //var mutexAcquired = Enumerable.Range(0, ConcurrentMutexes).Select(_ => new TaskCompletionSource<bool>()).ToArray();

            //var mutexes = mutexAcquired.Select(completed => new BlobDistributedMutex(settings, SignalAndWait(completed))).ToArray();

            //var cts = new CancellationTokenSource();

            //foreach (var mutex in mutexes)
            //{
            //    mutex.RunTaskWhenMutexAcquired(cts.Token);
            //}

            //firstCts.Cancel();

            //Task.WaitAny(mutexAcquired.Select(x => (Task)x.Task).ToArray(), TimeSpan.FromSeconds(80));

            //cts.Cancel();

            //Assert.AreEqual(1, mutexAcquired.Count(x => x.Task.IsCompleted));
        }

        private static Func<CancellationToken, Task> SignalAndWait(TaskCompletionSource<bool> signal)
        {
            return async token => { signal.SetResult(true); await Task.Delay(1000000, token); };
        }
    }
}
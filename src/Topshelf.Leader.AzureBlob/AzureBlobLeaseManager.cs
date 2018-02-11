using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Topshelf.Logging;

namespace Topshelf.Leader.AzureBlob
{
    public struct BlobSettings
    {
        public const string DefaultContainer = "topshelf-leader";
        public const string DefaultBlob = "mutex";

        public readonly string Container;
        public readonly string BlobName;
        public CloudStorageAccount StorageAccount;

        public BlobSettings(CloudStorageAccount storageAccount) : this(storageAccount, DefaultContainer, DefaultBlob)
        {
        }

        public BlobSettings(CloudStorageAccount storageAccount, string container, string blobName)
        {
            NameValidator.ValidateContainerName(container);
            NameValidator.ValidateBlobName(blobName);

            StorageAccount = storageAccount;
            Container = container;
            BlobName = blobName;
        }
    }

    public class AzureBlobLeaseManager : ILeaseManager
    {
        private readonly CloudPageBlob leaseBlob;
        private readonly LogWriter logger = HostLogger.Get<ILeaseManager>();

        public AzureBlobLeaseManager(BlobSettings settings)
            : this(settings.StorageAccount.CreateCloudBlobClient(), settings.Container, settings.BlobName)
        {
        }

        public AzureBlobLeaseManager(CloudBlobClient blobClient, string leaseContainerName, string leaseBlobName)
        {
            blobClient.DefaultRequestOptions.RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(1), 3);
            var container = blobClient.GetContainerReference(leaseContainerName);
            leaseBlob = container.GetPageBlobReference(leaseBlobName);
        }

        public async Task<bool> AcquireLease(string nodeId, CancellationToken token)
        {
            var leaseIdentifier = NodeToLeaseIdentifier(nodeId);
            try
            {
                var lease = await leaseBlob.AcquireLeaseAsync(TimeSpan.FromSeconds(60), leaseIdentifier, token);
                return leaseIdentifier == lease;
            }
            catch (StorageException storageException)
            {
                if (storageException.InnerException is WebException webException && webException.Response is HttpWebResponse response)
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.NotFound:
                            await CreateBlobAsync(token);
                            return await AcquireLease(nodeId, token);
                        case HttpStatusCode.Conflict:
                            logger.Info("Could not Aquire the lease, another node owns the lease.");
                            return false;
                    }

                throw;
            }
        }

        public async Task<bool> RenewLease(string nodeId, CancellationToken token)
        {
            try
            {
                await leaseBlob.RenewLeaseAsync(new AccessCondition { LeaseId = NodeToLeaseIdentifier(nodeId) }, token);
                return true;
            }
            catch (StorageException storageException)
            {
                logger.WarnFormat("Could not renew the lease {0}", storageException.Message);
                return false;
            }
        }

        public async Task ReleaseLease(string nodeId)
        {
            try
            {
                await leaseBlob.ReleaseLeaseAsync(new AccessCondition { LeaseId = NodeToLeaseIdentifier(nodeId) });
            }
            catch (StorageException e)
            {
                // Lease will eventually be released.
                logger.ErrorFormat("Could not release the lease {0}", nameof(ReleaseLease), e.Message);
            }
        }

        private static string NodeToLeaseIdentifier(string nodeId)
        {
            return StringToGuidConverter.Convert(nodeId).ToString();
        }

        private async Task CreateBlobAsync(CancellationToken token)
        {
            logger.InfoFormat("Creating container {0} if it does not exist.", leaseBlob.Container.Name);
            await leaseBlob.Container.CreateIfNotExistsAsync(token);
            if (!await leaseBlob.ExistsAsync(token))
            {
                try
                {
                    logger.InfoFormat("Creating blob {0}.", leaseBlob.Name);
                    await leaseBlob.CreateAsync(0, token);
                }
                catch (StorageException e)
                {
                    if (e.InnerException is WebException webException)
                    {
                        if (!(webException.Response is HttpWebResponse response) || response.StatusCode != HttpStatusCode.PreconditionFailed)
                        {
                            throw;
                        }
                    }
                }
            }
        }
    }
}

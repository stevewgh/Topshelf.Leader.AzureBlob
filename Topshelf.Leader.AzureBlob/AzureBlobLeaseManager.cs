﻿using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

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
            var blobNotFound = false;
            try
            {
                var leaseIdentifier = NodeToLeaseIdentifier(nodeId);
                var lease = await leaseBlob.AcquireLeaseAsync(TimeSpan.FromSeconds(60), leaseIdentifier, token);
                return leaseIdentifier == lease;
            }
            catch (StorageException storageException)
            {
                if (storageException.InnerException is WebException webException)
                {
                    if (webException.Response is HttpWebResponse response)
                    {
                        switch (response.StatusCode)
                        {
                            case HttpStatusCode.BadRequest:
                            case HttpStatusCode.NotFound:
                                blobNotFound = true;
                                break;
                            case HttpStatusCode.Conflict:
                                return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            if (blobNotFound)
            {
                await CreateBlobAsync(token);
                return await AcquireLease(nodeId, token);
            }

            return false;
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
                // catch (WebException webException)
                Trace.TraceError(storageException.Message);

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
                Trace.TraceError(e.Message);
            }
        }

        private static string NodeToLeaseIdentifier(string nodeId)
        {
            return StringToGuidConverter.Convert(nodeId).ToString();
        }

        private async Task CreateBlobAsync(CancellationToken token)
        {
            await leaseBlob.Container.CreateIfNotExistsAsync(token);
            if (!await leaseBlob.ExistsAsync(token))
            {
                try
                {
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
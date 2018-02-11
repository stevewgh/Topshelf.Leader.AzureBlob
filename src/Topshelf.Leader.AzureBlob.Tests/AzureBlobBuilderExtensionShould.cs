using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Xunit;

namespace Topshelf.Leader.AzureBlob.Tests
{
    public class AzureBlobBuilderExtensionShould
    {
        [Fact]
        public void register_the_lease_manager_with_the_builder()
        {
            var builder = new LeaderConfigurationBuilder<object>();
            builder.WithAzureBlobStorageLeaseManager(new BlobSettings(CloudStorageAccount.DevelopmentStorageAccount));
            builder.WhenStarted((o, token) => Task.FromResult(true));

            var built = builder.Build();

            Assert.IsType<AzureBlobLeaseManager>(built.LeaseManager);
        }
    }
}
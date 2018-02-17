using Microsoft.WindowsAzure.Storage;
using Topshelf.Logging;
using Xunit;

namespace Topshelf.Leader.AzureBlob.Tests
{
    public class AzureBlobBuilderExtensionShould
    {
        public AzureBlobBuilderExtensionShould()
        {
            HostLogger.UseLogger(new TraceHostLoggerConfigurator());
        }

        [Fact]
        public void register_the_lease_manager_with_the_builder()
        {
            var builder = new LeaseConfigurationBuilder("Node1");
            builder.WithAzureBlobStorageLeaseManager(new BlobSettings(CloudStorageAccount.DevelopmentStorageAccount));
            var built = builder.Build();
            Assert.IsType<AzureBlobLeaseManager>(built.LeaseManager(built));
        }
    }
}
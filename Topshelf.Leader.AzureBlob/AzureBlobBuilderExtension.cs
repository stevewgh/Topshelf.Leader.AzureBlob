namespace Topshelf.Leader.AzureBlob
{
    public static class AzureBlobBuilderExtension
    {
        public static void WithAzureBlobStorageLeaseManager<T>(this LeaderConfigurationBuilder<T> builder, BlobSettings settings)
        {
            builder.WithLeaseManager(new AzureBlobLeaseManager(settings));
        }
    }
}
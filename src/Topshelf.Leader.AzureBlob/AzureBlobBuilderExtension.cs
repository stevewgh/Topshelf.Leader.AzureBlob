namespace Topshelf.Leader.AzureBlob
{
    public static class AzureBlobBuilderExtension
    {
        public static void WithAzureBlobStorageLeaseManager(this LeaseConfigurationBuilder builder, BlobSettings settings)
        {
            builder.WithLeaseManager(lc =>
            {
                var validator = new AzureBlobLeaseLengthValidator();
                var leaseLength = lc.LeaseLengthCalculator.Calculate();
                validator.Validate(leaseLength);
                return new AzureBlobLeaseManager(settings, leaseLength);
            });
        }
    }
}
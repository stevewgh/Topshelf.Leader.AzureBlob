# Topshelf.Leader.AzureBlob
Azure Blob storage based lease manager for use with Topshelf.Leader. Topshelf.Leader.AzureBlob allows multiple 
services to determine who is Active and who is Passive by utilising the lease properties of Azure Blob storage.

## Special Mention
Topshelf.Leader.AzureBlob has been based on the example given at https://docs.microsoft.com/en-us/azure/architecture/patterns/leader-election and has been adapted for use with [Topshelf.Leader](https://github.com/stevewgh/Topshelf.Leader).

## Getting started
```
Install-Package Topshelf.Leader.AzureBlob
```

The majority of options configured are the same as the example provided in the Topshelf.Leader project. The only difference is that the lease manager is now Azure Blob based and is configured during lease configuration.

### Example
```c#
using Topshelf.Leader;

public class Program
{
    static void Main(string[] args)
    {
      var rc = HostFactory.Run(x =>
      {
          x.Service<TheService>(s =>
          {
              s.WhenStartedAsLeader(b =>
              {
			b.WhenStarted(async (service, token) =>
			{
				await service.Start(token);
			});

			b.Lease(lcb =>
			{
				lcb.RenewLeaseEvery(TimeSpan.FromSeconds(30));
				lcb.AquireLeaseEvery(TimeSpan.FromMinutes(1));
				lcb.LeaseLength(TimeSpan.FromSeconds(15));

				var cloudStorageAccount = CloudStorageAccount.Parse(<ConnectionString>);
				var blobSettings = new BlobSettings(cloudStorageAccount);
				lcb.WithAzureBlobStorageLeaseManager(blobSettings);
			});
              });
              s.ConstructUsing(name => new TheService());
              s.WhenStopped(service => service.Stop());
          });
      });
  }
}
```

The Azure Blob lease system does have some constraints that you should be aware of. 

* Lease length can not be less than 15 seconds
* Lease length can not be greater than 60 seconds

If you try to specify a lease length outside of these parameters an exception will be thrown. 
If you don't specify a lease length then the Topshelf.Leader default of 5 seconds will be used and will cause an exception to be thrown. **Always specify a lease length.**

## Azure Blob Storage Container and File name
If you don't specify a container, the Lease Manager will create one called `topshelf-leader`. It will create a zero byte
file called `mutex` within the container. All lease operations are taken against the mutex file.

### Example of using a different Container and Mutex name
```c#
using Topshelf.Leader;

public class Program
{
    static void Main(string[] args)
    {
      var rc = HostFactory.Run(x =>
      {
          x.Service<TheService>(s =>
          {
              s.WhenStartedAsLeader(builder =>
              {
		b.WhenStarted(async (service, token) =>
		{
			await service.Start(token);
		});

		b.Lease(lcb =>
		{
			lcb.RenewLeaseEvery(TimeSpan.FromSeconds(30));
			lcb.AquireLeaseEvery(TimeSpan.FromMinutes(1));
			lcb.LeaseLength(TimeSpan.FromSeconds(15));

			var cloudStorageAccount = CloudStorageAccount.Parse(<ConnectionString>);
    			var blobSettings = new BlobSettings(cloudStorageAccount, "different-container", "different-mutex");
			lcb.WithAzureBlobStorageLeaseManager(blobSettings);
		});
              });
              s.ConstructUsing(name => new TheService());
              s.WhenStopped(service => service.Stop());
          });
      });
  }
}
```


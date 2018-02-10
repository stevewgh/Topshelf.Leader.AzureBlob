using System;

namespace Topshelf.Leader.AzureBlob.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var rc = HostFactory.Run(x =>
            {
                x.Service<Service>(sc =>
                {
                    sc.WhenStartedAsLeader(b =>
                    {
                        b.AttemptToBeTheLeaderEvery(TimeSpan.FromMinutes(1));
                        b.WhenLeaderIsElected(iamLeader =>
                        {
                            if (iamLeader)
                            {
                                System.Console.ForegroundColor = ConsoleColor.Black;
                                System.Console.ForegroundColor = ConsoleColor.Red;
                                System.Console.BackgroundColor = ConsoleColor.White;
                            }

                            System.Console.WriteLine($"Leader election took place: {iamLeader}");
                            System.Console.ForegroundColor = ConsoleColor.Gray;
                            System.Console.BackgroundColor = ConsoleColor.Black;
                        });

                        b.WithAzureBlobStorageLeaseManager(new BlobSettings());
                        b.WhenStarted(async (service, token) => await service.Start(token));
                    });
                    sc.WhenStopped(service => service.Stop());
                    sc.ConstructUsing(name => new Service());
                });
            });
        }
    }
}
﻿using System;
using System.Configuration;
using Microsoft.WindowsAzure.Storage;

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
                        b.Lease(lcb =>
                        {
                            lcb.RenewLeaseEvery(TimeSpan.FromSeconds(30));
                            lcb.AquireLeaseEvery(TimeSpan.FromMinutes(1));
                            lcb.LeaseLength(TimeSpan.FromSeconds(15));

                            var cloudStorageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["CloudStorageAccount"].ConnectionString);
                            var blobSettings = new BlobSettings(cloudStorageAccount);
                            lcb.WithAzureBlobStorageLeaseManager(blobSettings);
                        });

                        b.WhenStarted(async (service, token) => await service.Start(token));
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

                    });
                    sc.WhenStopped(service => service.Stop());
                    sc.ConstructUsing(name => new Service());
                });

                x.OnException(exception =>
                {
                    System.Console.WriteLine(exception);
                    System.Console.ReadLine();
                });
            });
        }
    }
}
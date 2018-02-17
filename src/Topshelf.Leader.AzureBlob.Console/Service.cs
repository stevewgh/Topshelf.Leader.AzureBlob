using System;
using System.Threading;
using System.Threading.Tasks;

namespace Topshelf.Leader.AzureBlob.Console
{
    public class Service
    {
        public void Stop()
        {
            System.Console.WriteLine("Stopping.");
        }

        public async Task Start(CancellationToken token)
        {
            System.Console.WriteLine("Starting and will now delay for 5 seconds.");
            await Task.Delay(TimeSpan.FromSeconds(5), token);
            System.Console.WriteLine("Delay complete.");
        }
    }
}

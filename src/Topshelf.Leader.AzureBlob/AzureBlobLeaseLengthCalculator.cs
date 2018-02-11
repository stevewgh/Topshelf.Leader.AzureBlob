using System;

namespace Topshelf.Leader.AzureBlob
{
    public class AzureBlobLeaseLengthCalculator : LeaseLengthCalculator
    {
        public static TimeSpan MinimumLeaseTime = TimeSpan.FromSeconds(5);
        public static TimeSpan MaximumLeaseTime = TimeSpan.FromSeconds(60);

        public override TimeSpan Calculate(LeaseCriteria leaseCriteria)
        {
            var leaseLength = base.Calculate(leaseCriteria);

            if (leaseLength.TotalSeconds < MinimumLeaseTime.TotalSeconds)
            {
                leaseLength = MinimumLeaseTime;
            } else if (leaseLength.TotalSeconds > MaximumLeaseTime.TotalSeconds)
            {
                leaseLength = MaximumLeaseTime;
            }

            return leaseLength;
        }
    }
}

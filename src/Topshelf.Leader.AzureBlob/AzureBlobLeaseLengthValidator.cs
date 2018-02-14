using System;
using FluentValidation;

namespace Topshelf.Leader.AzureBlob
{
    public class AzureBlobLeaseLengthValidator : AbstractValidator<TimeSpan>
    {
        public static TimeSpan MinimumLeaseTime = TimeSpan.FromSeconds(15);
        public static TimeSpan MaximumLeaseTime = TimeSpan.FromSeconds(60);

        public AzureBlobLeaseLengthValidator()
        {
            RuleFor(span => span).InclusiveBetween(MinimumLeaseTime, MaximumLeaseTime);
        }
    }
}
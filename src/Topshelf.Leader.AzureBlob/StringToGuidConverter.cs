using System;
using System.Security.Cryptography;
using System.Text;

namespace Topshelf.Leader.AzureBlob
{
    public class StringToGuidConverter
    {
        public static Guid Convert(string source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            try
            {
                return Guid.Parse(source);
            }
            catch (FormatException)
            {
                using (var md5 = new MD5Cng())
                {
                    var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(source));
                    return new Guid(hash);
                }
            }
        }
    }
}
using System;
using System.Security.Cryptography.X509Certificates;

namespace CheckAuthenticodeSigning
{
    internal sealed class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length != 1) {
                Console.WriteLine("USAGE: CheckAuthenticodeSigning [file]");
                return;
            }

            try {
                var cert = X509Certificate.CreateFromSignedFile(args[0]);
                Console.WriteLine($"Signed by {cert.Subject}");
            }
            catch (Exception ex) {
                Console.WriteLine($"Error in getting signing file {args[0]}: {ex}");
            }
        }
    }
}

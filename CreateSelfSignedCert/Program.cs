namespace Microsoft.Test
{
    using System;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;

    public sealed class Program
    {
        private const string CertSubjectName = "localhost";

        private static void Main()
        {
            try
            {
                // Create a self-signed certificate
                var cert = CreateSelfSignedCertificate();

                // Install the certificate in the local computer personal store
                InstallCertificate(cert);

                // Add the certificate to the trusted root certification authorities
                AddCertificateToTrustedRoot(cert);

                Console.WriteLine("Certificate successfully installed and added to trusted root authorities.");

                // Clean up: delete the certificate from both places
                DeleteCertificate(cert);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static X509Certificate2 CreateSelfSignedCertificate()
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest($"CN={CertSubjectName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            // available in .NET 8, not in net472.
            ////request.CertificateExtensions.Add(new X509SubjectAlternativeNameExtension(new[] { "localhost", "127.0.0.1" }));
            var cert = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
            return new X509Certificate2(cert.Export(X509ContentType.Pfx));
        }

        private static void InstallCertificate(X509Certificate2 cert)
        {
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(cert);
        }

        private static void AddCertificateToTrustedRoot(X509Certificate2 cert)
        {
            using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(cert);
        }

        private static void DeleteCertificate(X509Certificate2 cert)
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadWrite);
                var matchingCerts = store.Certificates.Find(X509FindType.FindByThumbprint, cert.Thumbprint, false);
                foreach (var matchingCert in matchingCerts)
                {
                    store.Remove(matchingCert);
                }
            }

            using (var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadWrite);
                var matchingCerts = store.Certificates.Find(X509FindType.FindByThumbprint, cert.Thumbprint, false);
                foreach (var matchingCert in matchingCerts)
                {
                    store.Remove(matchingCert);
                }
            }
        }
    }
}

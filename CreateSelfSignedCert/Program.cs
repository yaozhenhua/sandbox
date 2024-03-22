namespace Microsoft.Test
{
    using System;
    using System.Runtime.InteropServices;
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

                // Disposing the cert will corrupt it. This will prevent the Dispose() method from kicking in.
                GCHandle.Alloc(cert, GCHandleType.Normal);

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

            // Add Key Usages.
            request.CertificateExtensions.Add(new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                    false));

            // Add Enhanced Key Usages.
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
                new OidCollection
                {
                        new Oid("1.3.6.1.5.5.7.3.1"), // server authentication
                        new Oid("1.3.6.1.5.5.7.3.2"), // client authentication
                },
                false));

            // Add list of Subject Alternative Names.
            var san = new SubjectAlternativeNameBuilder();
            san.AddIpAddress(System.Net.IPAddress.Loopback);
            san.AddDnsName("localhost");
            request.CertificateExtensions.Add(san.Build());

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

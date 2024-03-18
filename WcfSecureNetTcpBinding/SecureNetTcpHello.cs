/*
 * To create a self-signed cert:
 * 
 *      New-SelfSignedCertificate -DnsName "localhost" -CertStoreLocation "cert:\LocalMachine\My"
 *      
 * To allow non-Administrator running the program, go to "Manage computer certificates", "Personal", "certificates"
 * folder, right click "localhost" and click "Manage private keys", add "Users" group so regular user can read.
 * 
 * Note that the certificate needs to be added in "Trusted Root Certification Authorities" in the local computer.
 * Otherwise the cert is not trusted and thus cannot be used.
 *
 * Update: the certificate can be changed from LocalMachine to CurrentUser then no administrator privilege is required.
 */

namespace Microsoft.Test
{
    using System;
    using System.IdentityModel.Selectors;
    using System.Security.Cryptography.X509Certificates;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;
    using System.ServiceModel.Security;

    /// <summary>
    /// Sample service contract.
    /// </summary>
    [ServiceContract]
    public interface ICalculatorService
    {
        [OperationContract]
        int Add(int num1, int num2);
    }

    /// <summary>
    /// Sample implementation of the service.
    /// </summary>
    public sealed class CalculatorService : ICalculatorService
    {
        public int Add(int num1, int num2) => num1 + num2;
    }

    /// <summary>
    /// Sample implementation of the client.
    /// </summary>
    public sealed class CalculatorClient(Binding binding, EndpointAddress address)
        : ClientBase<ICalculatorService>(binding, address)
    {
        public int Add(int num1, int num2) => this.Channel.Add(num1, num2);
    }

    /// <summary>
    /// Console program to start the WCF service and call it.
    /// </summary>
    public sealed class SecureNetTcpHello
    {
        private const string CertSubjectName = "localhost";
        private const string ServiceHostName = "localhost";
        private const int ServicePort = 8000;

        private static void Main()
        {
            //// ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13;

            // Create the service host
            using var host = new ServiceHost(typeof(CalculatorService));
            host.Credentials.ServiceCertificate.SetCertificate(
                StoreLocation.CurrentUser,
                StoreName.My,
                X509FindType.FindBySubjectName,
                CertSubjectName);

            // Optional: single instance concurrency mode.
            var behavior = host.Description.Behaviors.Find<ServiceBehaviorAttribute>();
            if (behavior == null)
            {
                behavior = new ServiceBehaviorAttribute();
                host.Description.Behaviors.Add(behavior);
            }

            behavior.InstanceContextMode = InstanceContextMode.Single;
            behavior.ConcurrencyMode = ConcurrencyMode.Multiple;

            var addr = new EndpointAddress($"net.tcp://{ServiceHostName}:{ServicePort}/CalculatorService");

            // Define the service endpoint
            ServiceEndpoint endpoint = host.AddServiceEndpoint(
                typeof(ICalculatorService),
                GetSecureNetTcpBinding(),
                addr.Uri);

            // This is required to accept the self-signed cert.
            host.Credentials.ClientCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.Custom;
            host.Credentials.ClientCertificate.Authentication.CustomCertificateValidator = new CustomX509CertificateValidator();

            // Open the service host to start listening for messages
            host.Open();

            Console.WriteLine("The service is ready at {0}", endpoint.Address);

            // Create a WCF client to confirm the service is working.
            using var client = new CalculatorClient(GetSecureNetTcpBinding(), addr);
            client.ClientCredentials.ClientCertificate.SetCertificate(
                StoreLocation.CurrentUser,
                StoreName.My,
                X509FindType.FindBySubjectName,
                CertSubjectName);

            // Call the service
            try
            {
                int result = client.Add(15, 23);
                Console.WriteLine($"The result of adding 15 and 23 is: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An exception occurred: {ex}");
            }
            finally
            {
                // Close the client
                client.Close();
            }

            // Optional: use ChannelFactory to create the client object and invoke the service.
            using var factory = new ChannelFactory<ICalculatorService>(GetSecureNetTcpBinding(), addr);
            factory.Credentials.ClientCertificate.SetCertificate(
                StoreLocation.CurrentUser,
                StoreName.My,
                X509FindType.FindBySubjectName,
                CertSubjectName);
            var channel = factory.CreateChannel();
            try
            {
                int result = channel.Add(15, 23);
                Console.WriteLine($"The result of adding 15 and 23 is: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An exception occurred: {ex}");
            }
            finally
            {
                ((IDisposable)channel).Dispose();
                factory.Close();
            }

            Console.WriteLine("Press <Enter> to stop the service.");
            Console.ReadLine();

            // Close the service host
            host.Close();
        }

        private static NetTcpBinding GetSecureNetTcpBinding()
        {
            // Create the binding from the provided configuration
            var binding = new NetTcpBinding(SecurityMode.TransportWithMessageCredential)
            {
                MaxConnections = 200,
                ReceiveTimeout = TimeSpan.FromMinutes(10),
                CloseTimeout = TimeSpan.FromMinutes(10),
                OpenTimeout = TimeSpan.FromMinutes(10),
                SendTimeout = TimeSpan.FromMinutes(10),
                MaxReceivedMessageSize = 268435456,
                ReaderQuotas = { MaxStringContentLength = 20971520, MaxArrayLength = 1048576 }
            };

            binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Certificate;
            binding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;
            binding.Security.Message.ClientCredentialType = MessageCredentialType.Certificate;

            return binding;
        }

        private sealed class CustomX509CertificateValidator : X509CertificateValidator
        {
            public override void Validate(X509Certificate2 certificate)
            {
                Console.WriteLine($"Validating cert: {certificate}");
            }
        }
    }
}

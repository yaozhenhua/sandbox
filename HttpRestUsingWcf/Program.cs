using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using System.Threading.Tasks;

[ServiceContract]
public interface IRestService
{
    // Simple GET with all parameters in the request URI.
    [OperationContract]
    [WebGet(UriTemplate = "/hello/{name}")]
    Dictionary<string,string> SayHello(string name);

    // POST with parameters in the body.
    [OperationContract]
    [WebInvoke(Method = "POST", UriTemplate = "/upload", BodyStyle = WebMessageBodyStyle.Wrapped)]
    void UploadMessage(string content, string name);
}

public class RestService : IRestService
{
    public Dictionary<string,string> SayHello(string name)
    {
        return new Dictionary<string, string>
        {
            { name, $"Hello, {name}!" },
        };
    }

    public void UploadMessage(string content, string name)
    {
        Console.WriteLine("Uploading: " + content + " with name: " + name);
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        // This requires administrator privilege to open the port.
        Uri baseAddress = new Uri("https://localhost:14080/WcfRest/");
        using var host = new ServiceHost(typeof(RestService), baseAddress);

        // The following will ignore the validation error caused by self-signed cert.
        ServicePointManager.ServerCertificateValidationCallback = (_1, _2, _3, _4) => true;

        var binding = new WebHttpBinding();
        binding.Security.Mode = WebHttpSecurityMode.Transport;
        binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;
        host.AddServiceEndpoint(typeof(IRestService), binding, "");

        var behavior = new WebHttpBehavior();
        behavior.DefaultOutgoingRequestFormat = WebMessageFormat.Json;
        behavior.DefaultOutgoingResponseFormat = WebMessageFormat.Json;
        host.Description.Endpoints[0].EndpointBehaviors.Add(behavior);

        host.Credentials.ServiceCertificate.SetCertificate(
            StoreLocation.LocalMachine,
            StoreName.My,
            X509FindType.FindBySubjectName,
            "localhost");

        host.Open();

        Console.WriteLine($"Service is running at {baseAddress}");

        // Test the service.
        var client = new HttpClient
        {
            BaseAddress = baseAddress,
        };

        // Send a GET request without body.
        var response = await client.GetAsync("hello/Yao").ConfigureAwait(false);
        Console.WriteLine($"Response code: {response.StatusCode} succeeded={response.IsSuccessStatusCode}");
        Console.WriteLine($"Content: {await response.Content.ReadAsStringAsync()}");

        // Send a POST request with body.
        var content = new StringContent(
            "{\"content\":\"haha\", \"name\":\"Yao\"}",
            System.Text.Encoding.UTF8,
            "application/json");
        response = await client.PostAsync("upload", content).ConfigureAwait(false);
        Console.WriteLine($"Response code: {response.StatusCode} succeeded={response.IsSuccessStatusCode}");
        Console.WriteLine($"Content: {await response.Content.ReadAsStringAsync()}");

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}

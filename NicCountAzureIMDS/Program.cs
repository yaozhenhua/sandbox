// This program retrieves the current number of Ethernet adapters in the VM, then retrieves the provisioned number of
// adapters by querying Azure instant metadata service.
//
// Initially written by ChatGPT 3.5.
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class Program
{
    static async Task Main()
    {
        // List the number of Ethernet NICs on the current computer
        Console.WriteLine("Number of Ethernet NICs on the current computer:");

        var localNICs = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic => nic.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Ethernet &&
                           !IsHyperVSwitch(nic.Name));

        int localNICCount = localNICs.Count();
        Console.WriteLine(localNICCount);

        // Send a request to Azure IMDS to get the number of NICs
        Console.WriteLine("\nSending request to Azure IMDS...");

        try
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Metadata", "true");
                var response = await client.GetAsync("http://169.254.169.254/metadata/instance/network/interface/?api-version=2020-09-01");
                if (response.IsSuccessStatusCode)
                {
                    string responseData = await response.Content.ReadAsStringAsync();
                    // Print responseData for debugging
                    Console.WriteLine($"Response Data: {responseData}");

                    // Deserialize the JSON array into objects
                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var networkInterfaces = JsonSerializer.Deserialize<NetworkInterface[]>(responseData, jsonOptions);

                    // Count the number of NICs
                    int azureNICCount = networkInterfaces.Length;
                    Console.WriteLine($"\nNumber of NICs on Azure VM according to IMDS: {azureNICCount}");
                }
                else
                {
                    Console.WriteLine($"Failed to get NIC information from Azure IMDS. Status code: {response.StatusCode}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    static bool IsHyperVSwitch(string name)
    {
        // Check if the name contains "Switch"
        return name.Contains("Switch", StringComparison.OrdinalIgnoreCase);
    }
}

public class NetworkInterface
{
    public IPv4 IPv4 { get; set; }
    public IPv6 IPv6 { get; set; }
    public string MacAddress { get; set; }
}

public class IPv4
{
    public IpAddress[] IpAddress { get; set; }
    public Subnet[] Subnet { get; set; }
}

public class IpAddress
{
    public string PrivateIpAddress { get; set; }
    public string PublicIpAddress { get; set; }
}

public class Subnet
{
    public string Address { get; set; }
    public string Prefix { get; set; }
}

public class IPv6
{
    public IpAddress[] IpAddress { get; set; }
}

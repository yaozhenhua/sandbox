// Build:
//      csc /platform:arm64 *.cs /main:Test.MeasureDnsQuery
//
namespace Test
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class MeasureDnsQuery
    {
        private static async Task Main(string[] argv)
        {
            var hostname = argv.Length > 0 ? argv[0] : "www.bing.com";
            var repeatedTimes = 500_000;

            var clock = Stopwatch.StartNew();

            for (int i = 0; i < repeatedTimes; i++)
            {
                var ret = await AsyncDnsQuery.QueryAsync(hostname, AsyncDnsQuery.DnsRecordType.DNS_TYPE_A);
            }

            clock.Stop();
            Console.WriteLine("async for loop duration: {0}", clock.Elapsed);

            clock.Restart();
            int finishedCount = 0;
            for (int i = 0; i < repeatedTimes; i++)
            {
                _ = Task.Run(
                    () =>
                    {
                        _ = System.Net.Dns.GetHostEntry(hostname);
                        Interlocked.Increment(ref finishedCount);
                    });
            }

            while (finishedCount < repeatedTimes)
            {
                await Task.Delay(125);
            }

            clock.Stop();
            Console.WriteLine("System.Net.Dns Task.Run duration: {0}", clock.Elapsed);

            clock.Restart();
            finishedCount = 0;

            for (int i = 0; i < repeatedTimes; i++)
            {
                _ = Task.Run(
                    async () =>
                    {
                        await AsyncDnsQuery.QueryAsync(hostname, AsyncDnsQuery.DnsRecordType.DNS_TYPE_A);
                        Interlocked.Increment(ref finishedCount);
                    });
            }

            while (finishedCount < repeatedTimes)
            {
                await Task.Delay(125);
            }

            clock.Stop();
            Console.WriteLine("async Task.Run duration: {0}", clock.Elapsed);
        }
    }
}

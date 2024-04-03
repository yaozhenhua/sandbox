// Compile this code
//      csc /platform:x64 /debug AsyncDnsQuery.cs
//
// On ARM64 Windows:
//      csc /platform:arm64 /debug AsyncDnsQuery.cs

namespace Test
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;

    /// <summary>
    /// Asynchronous DNS query result to dispose native resource.
    /// </summary>
    public sealed class DnsQueryResult : IDisposable
    {
        private readonly IntPtr contextPtr;

        public DnsQueryResult(int code, string message, IntPtr contextPtr)
        {
            this.Code = code;
            this.Message = message;
            this.contextPtr = contextPtr;
        }

        /// <summary>
        /// Gets the status code of the DNS query.
        /// </summary>
        public int Code { get; }

        /// <summary>
        /// Gets the return message, or the query result of the DNS query.
        /// </summary>
        public string Message { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            // Release the pinned resource.
            GCHandle.FromIntPtr(this.contextPtr).Free();
            GC.SuppressFinalize(this);

            // Console.WriteLine("GC handle freed");
        }
    }

    /// <summary>
    /// Queries the DNS using native Win32 API asynchronously.
    /// </summary>
    public static class AsyncDnsQuery
    {
        /// <summary>
        /// Types of DNS record type supported by the method.
        /// </summary>
        public enum DnsRecordType : ushort
        {
            DNS_TYPE_A = 0x1,
            DNS_TYPE_CNAME = 0x5,
            DNS_TYPE_AAAA = 0x1C,
            DNS_TYPE_SRV = 0x21,
        }

        /// <summary>
        /// Internal callback function passed to the native code.
        /// </summary>
        private delegate void OnQueryFinished(
            int returnCode,
            [MarshalAs(UnmanagedType.LPWStr)] string message,
            IntPtr context);

        /// <summary>
        /// Internal function defined in the native code to start the DNS query.
        /// </summary>
        [DllImport(
            "DnsQueryWin",
            CharSet = CharSet.Unicode,
            ExactSpelling = true,
            CallingConvention = CallingConvention.StdCall)]
        private static extern int QueryDns(
            string hostName,
            DnsRecordType queryType,
            [MarshalAs(UnmanagedType.FunctionPtr)] OnQueryFinished callback,
            IntPtr context);

        /// <summary>
        /// Queries DNS server using Win32 API asynchronously.
        /// </summary>
        /// <param name="hostName">Host name to be queried.</param>
        /// <param name="queryType">DNS query type.</param>
        /// <returns>Async task that can be resolved to the query result on completion.</returns>
        public static Task<DnsQueryResult> QueryAsync(string hostName, DnsRecordType queryType)
        {
            var tcs = new TaskCompletionSource<DnsQueryResult>();

            // Pin the object so it will not move during GC.
            var contextPtr = GCHandle.ToIntPtr(GCHandle.Alloc(tcs));

            AsyncDnsQuery.QueryDns(hostName, queryType, AsyncDnsQuery.MyOnQueryFinished, contextPtr);

            return tcs.Task;
        }

        /// <summary>
        /// Callback that passed to the native function. This will finish the async task.
        /// </summary>
        private static void MyOnQueryFinished(int returnCode, string message, IntPtr context)
        {
            var ret = (TaskCompletionSource<DnsQueryResult>)GCHandle.FromIntPtr(context).Target;

            ret.SetResult(new DnsQueryResult(returnCode, message, context));
        }
    }

    /// <summary>
    /// Sample test code.
    /// </summary>
    internal sealed class Test
    {
        private static async Task Main(string[] argv)
        {
            var hostname = argv.Length > 0 ? argv[0] : "www.bing.com";

            var ret = await AsyncDnsQuery.QueryAsync(hostname, AsyncDnsQuery.DnsRecordType.DNS_TYPE_A);
            Console.WriteLine($"type=A: status={ret.Code} result={ret.Message}\n");

            ret = await AsyncDnsQuery.QueryAsync(hostname, AsyncDnsQuery.DnsRecordType.DNS_TYPE_AAAA);
            Console.WriteLine($"type=AAAA: status={ret.Code} result={ret.Message}\n");

            ret = await AsyncDnsQuery.QueryAsync(hostname, AsyncDnsQuery.DnsRecordType.DNS_TYPE_CNAME);
            Console.WriteLine($"type=CNAME: status={ret.Code} result={ret.Message}\n");

            ret = await AsyncDnsQuery.QueryAsync(hostname, AsyncDnsQuery.DnsRecordType.DNS_TYPE_SRV);
            Console.WriteLine($"type=SRV: status={ret.Code} result={ret.Message}\n");

            ret.Dispose();
        }
    }
}

namespace Test
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;

    public sealed class DnsQueryResult : IDisposable
    {
        private readonly IntPtr contextPtr;

        public DnsQueryResult(int code, string message, IntPtr contextPtr)
        {
            this.Code = code;
            this.Message = message;
            this.contextPtr = contextPtr;
        }

        public int Code { get; }
        public string Message { get; }

        public void Dispose()
        {
            GCHandle.FromIntPtr(this.contextPtr).Free();
            GC.SuppressFinalize(this);

            Console.WriteLine("GC handle freed");
        }
    }

    public static class AsyncDnsQuery
    {
        private delegate void OnQueryFinished(
            int returnCode,
            [MarshalAs(UnmanagedType.LPWStr)] string message,
            IntPtr context);

        [DllImport(
            "DnsQueryWin",
            CharSet = CharSet.Unicode,
            ExactSpelling = true,
            CallingConvention = CallingConvention.StdCall)]
        private static extern int QueryDns(
            string hostName,
            [MarshalAs(UnmanagedType.FunctionPtr)] OnQueryFinished callback,
            IntPtr context);

        public static Task<DnsQueryResult> QueryAsync(string hostName)
        {
            var tcs = new TaskCompletionSource<DnsQueryResult>();
            var contextPtr = GCHandle.ToIntPtr(GCHandle.Alloc(tcs));

            AsyncDnsQuery.QueryDns(hostName, AsyncDnsQuery.MyOnQueryFinished, contextPtr);

            return tcs.Task;
        }

        private static void MyOnQueryFinished(int returnCode, string message, IntPtr context)
        {
            var ret = (TaskCompletionSource<DnsQueryResult>)GCHandle.FromIntPtr(context).Target;

            ret.SetResult(new DnsQueryResult(returnCode, message, context));
        }
    }

    internal sealed class Test
    {
        private static async Task Main()
        {
            using var ret = await AsyncDnsQuery.QueryAsync("www.google.com");
            Console.WriteLine($"Result: {ret.Code} {ret.Message}");
        }
    }
}

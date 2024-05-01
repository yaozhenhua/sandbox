// Run this:
//      dotnet run -c Release -f net48
//      dotnet run -c Release -f net8.0
using System;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Diagnosers;

[MemoryDiagnoser]
public class AsyncBenchmark
{
    private static Task<int> DummyAsync() => Task.FromResult(0);
    private static Task<int> DummyExceptionAsync() => throw new Exception();

    [Benchmark]
    public int MeasureResult()
    {
        return DummyAsync().Result;
    }

    [Benchmark]
    public int MeasureGetAwaiterGetResult()
    {
        return DummyAsync().GetAwaiter().GetResult();
    }

    [Benchmark]
    public int MeasureExceptionResult()
    {
        try
        {
            return DummyExceptionAsync().Result;
        }
        catch (Exception)
        {
            return -1; // Return a dummy value
        }
    }

    [Benchmark]
    public int MeasureExceptionGetAwaiterGetResult()
    {
        try
        {
            return DummyExceptionAsync().Result;
        }
        catch (Exception)
        {
            return -1; // Return a dummy value
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<AsyncBenchmark>();
    }
}


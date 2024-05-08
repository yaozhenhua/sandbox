namespace SplitStringBenchmark
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Running;

    public static class StringEx
    {
        public static IEnumerable<string> SplitSlim(
            this string data,
            char separator = ' ',
            int startIndex = 0,
            bool trimEntries = true,
            bool removeEmptyEntries = true)
        {
            if (!string.IsNullOrEmpty(data))
            {
                int dataLength = data.Length;

                while (startIndex < dataLength)
                {
                    if (trimEntries)
                    {
                        while (startIndex < dataLength && char.IsWhiteSpace(data[startIndex]))
                        {
                            startIndex++;
                        }

                        // Only white chars are remaining, nothing more to return.
                        if (startIndex >= dataLength)
                        {
                            if (!removeEmptyEntries)
                            {
                                yield return string.Empty;
                            }

                            break;
                        }
                    }

                    // This is either the separator or the last char in the string.
                    int endIndex = startIndex;
                    while (endIndex < dataLength && data[endIndex] != separator)
                    {
                        endIndex++;
                    }

                    int length = endIndex - startIndex;

                    if (trimEntries)
                    {
                        while (length > 0 && char.IsWhiteSpace(data[startIndex + length - 1]))
                        {
                            length--;
                        }
                    }

                    if (length > 0 || !removeEmptyEntries)
                    {
                        yield return length == dataLength
                            ? data
                            : data.Substring(startIndex, length);
                    }

                    // endIndex is the end of string or the separator char. So the next one starts with endIndex + 2.
                    startIndex = endIndex + 1;
                }
            }
        }
    }

    [GcServer(true)]
    [MemoryDiagnoser]
    public class SplitString
    {
        static readonly char[] SpaceDelimiter = new[] { ' ' };
        static readonly char[] ColonDelimiter = new[] { ':' };

        //private readonly string SampleString = " CNetSpace:10.0.0.0/8  VNetSpace:10.50.0.0/16  PNetSpace:10.0.0.0/8;100.64.0.0/10 Subnet:10.50.1.0/24;10.50.1.1;10.50.1.2 ";
        private readonly string SampleString = "CNetSpace:10.0.0.0/8;172.16.0.0/12;192.168.0.0/16;213.199.180.0/24 VNetSpace:10.50.0.0/16 PNetSpace:10.0.0.0/8;100.64.0.0/10 Subnet:10.50.1.0/24;10.50.1.1;10.50.1.2;10.50.1.3 Subnet:10.50.0.0/24;10.50.0.1;10.50.0.2;10.50.0.3 ConnectionType:DirectAttach";

        public SplitString()
        {
            //SampleString = string.Join(" ",
            //    Enumerable.Range(0, 1000).Select(x => string.Join(":", x.ToString(), (x + 1).ToString())));
        }

        [Benchmark]
        public void DefaultSplit()
        {
            int count = 0;
            foreach (var s in SampleString.Split(new[] { ' '}, StringSplitOptions.RemoveEmptyEntries))
            {
                foreach (var t in s.Trim().Split(new[] { ':'}, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (t.Trim().Length > 0)
                        count++;
                }
            }

            if (count == 0)
            {
                throw new Exception();
            }
        }

        [Benchmark]
        public void OptimizedSplit()
        {
            int count = 0;
            foreach (var s in SampleString.Split(SpaceDelimiter, StringSplitOptions.RemoveEmptyEntries))
            {
                foreach (var t in s.Trim().Split(ColonDelimiter, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (t.Trim().Length > 0)
                        count++;
                }
            }

            if (count == 0)
            {
                throw new Exception();
            }
        }

        //[Benchmark]
        //public void RegexSplit()
        //{
        //    int count = 0;

        //    var regexSpace = new Regex(@"\s+", RegexOptions.Compiled);
        //    var regexColon = new Regex(@":+", RegexOptions.Compiled);

        //    foreach (var s in regexSpace.Split(SampleString))
        //    {
        //        foreach (var t in regexColon.Split(SampleString))
        //        {
        //            count++;
        //        }
        //    }

        //    if (count == 0)
        //    {
        //        throw new Exception();
        //    }
        //}

        [Benchmark]
        public void SlimSplit()
        {
            int count = 0;
            foreach (var s in SampleString.SplitSlim(' '))
            {
                foreach (var t in s.SplitSlim(':'))
                {
                    count++;
                }
            }

            if (count == 0)
            {
                throw new Exception();
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<SplitString>();
        }
    }
}

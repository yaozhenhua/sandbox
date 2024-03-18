namespace MsBuildGraph
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Build.Experimental.Graph;

    public class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("USAGE: MsBuildGraph [dirs.proj]");
                return;
            }

            var graph = new ProjectGraph(args);
            /*
            var visited = new HashSet<ProjectGraphNode>();

            var checking = new Queue<ProjectGraphNode>();
            foreach (var n in graph.EntryPointNodes)
            {
                checking.Enqueue(n);
            }

            while (checking.Count > 0)
            {
                var node = checking.Dequeue();
                if (visited.Contains(node))
                {
                    continue;
                }
                else
                {
                    visited.Add(node);
                }

                Console.WriteLine(node.ProjectInstance.FullPath);
                foreach (var dep in node.ProjectReferences)
                {
                    checking.Enqueue(dep);
                    Console.WriteLine($"  --> {dep.ProjectInstance.FullPath}");
                }
            }

            Console.WriteLine("Sorted list");
            foreach (var n in graph.ProjectNodesTopologicallySorted)
            {
                Console.WriteLine($"  -> {n.ProjectInstance.FullPath}");
            }
            */

            Console.WriteLine("digraph {");
            Console.WriteLine("  node [shape=record fontname=Arial];");
            var name = 0;
            var nodeNames = new Dictionary<ProjectGraphNode, int>();

            foreach (var n in graph.ProjectNodesTopologicallySorted)
            {
                Console.WriteLine($"  N{name} [label=\"{Normalize(n.ProjectInstance.FullPath)}\"];");
                nodeNames.Add(n, name);

                name++;
            }

            Console.WriteLine();
            foreach (var n in graph.ProjectNodesTopologicallySorted)
            {
                var from = nodeNames[n];
                foreach (var m in n.ProjectReferences)
                {
                    var to = nodeNames[m];

                    Console.WriteLine($"  N{from} -> N{to};");
                }
            }

            Console.WriteLine("}");
        }

        private static string Normalize(string s)
        {
            return s.Replace("\\", "\\\\");
        }
    }
}

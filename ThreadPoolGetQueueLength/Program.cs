/*
 * Sample code to get the number of queued work items in the thread pool.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

for (int i = 0; i < 1000; i++)
{
    _ = Task.Run(() => Thread.Sleep(1000));
}

var asm = System.Reflection.Assembly.GetExecutingAssembly();
var type = typeof(System.Threading.ThreadPool);
var method = type.GetMethod(
    "GetQueuedWorkItems",
    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
var items = method.Invoke(null, new object[0]) as IEnumerable<object>;

Console.WriteLine($"item count = {items.Count()}");

﻿namespace Fixie.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using Fixie.Internal;

    public static class Utility
    {
        public static string FullName<T>()
        {
            return typeof(T).FullName ??
                   throw new Exception($"Expected type {typeof(T).Name} to have a non-null FullName.");
        }

        public static string At<T>(string method, [CallerFilePath] string path = default!)
            => $"   at {FullName<T>().Replace("+", ".")}.{method} in {path}:line #";

        public static string[] For<TSampleTestClass>(params string[] entries)
            => entries.Select(x => FullName<TSampleTestClass>() + x).ToArray();

        public static string PathToThisFile([CallerFilePath] string path = default!)
            => path;

        public static IEnumerable<string> Run<TSampleTestClass, TExecution>() where TExecution : Execution, new()
            => Run<TSampleTestClass>(new SelfTestDiscovery(), new TExecution());

        public static IEnumerable<string> Run<TSampleTestClass>(Execution execution)
            => Run<TSampleTestClass>(new SelfTestDiscovery(), execution);

        public static IEnumerable<string> Run<TSampleTestClass>(Discovery discovery, Execution execution)
        {
            var listener = new StubListener();
            Run(listener, discovery, execution, typeof(TSampleTestClass));
            return listener.Entries;
        }

        public static IEnumerable<string> Run<TExecution>(Type testClass, TExecution execution) where TExecution : Execution
        {
            var listener = new StubListener();
            Run(listener, new SelfTestDiscovery(), execution, testClass);
            return listener.Entries;
        }

        public static IEnumerable<string> Run<TSampleTestClass>()
            => Run<TSampleTestClass>(new SelfTestDiscovery(), new DefaultExecution());

        public static IEnumerable<string> Run<TExecution>(Type testClass) where TExecution : Execution, new()
        {
            var listener = new StubListener();
            Run(listener, new SelfTestDiscovery(), new TExecution(), testClass);
            return listener.Entries;
        }

        public static void Discover(Listener listener, Discovery discovery, params Type[] candidateTypes)
        {
            if (candidateTypes.Length == 0)
                throw new InvalidOperationException("At least one type must be specified.");

            new TestAssembly(candidateTypes[0].Assembly, listener).Discover(candidateTypes, discovery);
        }

        public static void Run(Listener listener, Discovery discovery, Execution execution, params Type[] candidateTypes)
        {
            if (candidateTypes.Length == 0)
                throw new InvalidOperationException("At least one type must be specified.");

            new TestAssembly(candidateTypes[0].Assembly, listener).Run(candidateTypes, discovery, execution);
        }
    }
}

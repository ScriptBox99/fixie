namespace Fixie.Tests
{
    using System;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using Assertions;
    using Fixie.Internal;

    public class TestClassConstructionTests
    {
        static string[] Run<TSampleTestClass, TExecution>() where TExecution : Execution, new()
            => Run<TExecution>(typeof(TSampleTestClass));

        static string[] Run<TExecution>(Type testClass) where TExecution : Execution, new()
        {
            var listener = new StubListener();
            var discovery = new SelfTestDiscovery();
            var execution = new TExecution();
            using var console = new RedirectedConsole();

            Utility.Run(listener, discovery, execution, testClass);

            return console.Lines().ToArray();
        }

        class SampleTestClass : IDisposable
        {
            bool disposed;

            public SampleTestClass()
            {
                WhereAmI();
            }

            public void Pass()
            {
                WhereAmI();
            }

            public void Fail()
            {
                WhereAmI();
                throw new FailureException();
            }

            public void Skip()
            {
                WhereAmI();
                throw new ShouldBeUnreachableException();
            }

            public void Dispose()
            {
                if (disposed)
                    throw new ShouldBeUnreachableException();
                disposed = true;

                WhereAmI();
            }
        }

        class AllSkippedTestClass : IDisposable
        {
            bool disposed;

            public AllSkippedTestClass()
            {
                WhereAmI();
            }

            public void SkipA()
            {
                WhereAmI();
                throw new ShouldBeUnreachableException();
            }

            public void SkipB()
            {
                WhereAmI();
                throw new ShouldBeUnreachableException();
            }

            public void SkipC()
            {
                WhereAmI();
                throw new ShouldBeUnreachableException();
            }

            public void Dispose()
            {
                if (disposed)
                    throw new ShouldBeUnreachableException();
                disposed = true;

                WhereAmI();
            }
        }

        static class StaticTestClass
        {
            public static void Pass()
            {
                WhereAmI();
            }

            public static void Fail()
            {
                WhereAmI();
                throw new FailureException();
            }

            public static void Skip()
            {
                WhereAmI();
                throw new ShouldBeUnreachableException();
            }
        }

        static void WhereAmI([CallerMemberName] string member = default!)
            => System.Console.WriteLine(member);

        static bool ShouldSkip(TestMethod test)
            => test.Method.Name.Contains("Skip");

        class CreateInstancePerCase : Execution
        {
            public void Execute(TestClass testClass)
            {
                testClass.RunTests(test =>
                {
                    if (!ShouldSkip(test))
                        test.RunCases(@case => @case.Execute());
                });
            }
        }

        class CreateInstancePerClass : Execution
        {
            public void Execute(TestClass testClass)
            {
                var type = testClass.Type;
                var instance = type.IsStatic() ? null : Activator.CreateInstance(type);

                testClass.RunTests(test =>
                {
                    if (!ShouldSkip(test))
                        test.RunCases(@case => @case.Execute(instance));
                });

                instance.Dispose();
            }
        }

        public void ShouldConstructPerCaseByDefault()
        {
            Run<SampleTestClass, DefaultExecution>()
                .ShouldBe(
                    ".ctor", "Fail", "Dispose",
                    ".ctor", "Pass", "Dispose",
                    ".ctor", "Skip", "Dispose");
        }

        public void ShouldAllowConstructingPerCase()
        {
            Run<SampleTestClass, CreateInstancePerCase>()
                .ShouldBe(
                    ".ctor", "Fail", "Dispose",
                    ".ctor", "Pass", "Dispose");
        }

        public void ShouldAllowConstructingPerClass()
        {
            Run<SampleTestClass, CreateInstancePerClass>()
                .ShouldBe(".ctor", "Fail", "Pass", "Dispose");
        }

        public void ShouldBypassConstructionWhenConstructingPerCaseAndAllCasesAreSkipped()
        {
            Run<AllSkippedTestClass, CreateInstancePerCase>()
                .ShouldBeEmpty();
        }

        public void ShouldNotBypassConstructionWhenConstructingPerClassAndAllCasesAreSkipped()
        {
            Run<AllSkippedTestClass, CreateInstancePerClass>()
                .ShouldBe(".ctor", "Dispose");
        }

        public void ShouldBypassConstructionAttemptsWhenTestMethodsAreStatic()
        {
            Run<DefaultExecution>(typeof(StaticTestClass))
                .ShouldBe("Fail", "Pass", "Skip");

            Run<CreateInstancePerCase>(typeof(StaticTestClass))
                .ShouldBe("Fail", "Pass");

            Run<CreateInstancePerClass>(typeof(StaticTestClass))
                .ShouldBe("Fail", "Pass");
        }
    }
}
﻿namespace Fixie.Tests.Execution.Listeners
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;
    using Fixie.Execution.Listeners;
    using Fixie.Internal;
    using Should;
    using static Utility;

    public class TeamCityListenerTests
    {
        public void ShouldReportResultsToTheConsoleInTeamCityFormat()
        {
            using (var console = new RedirectedConsole())
            {
                var listener = new TeamCityListener();
                var convention = SelfTestConvention.Build();
                convention.CaseExecution.Skip(x => x.Method.Has<SkipAttribute>(), x => x.Method.GetCustomAttribute<SkipAttribute>().Reason);

                typeof(PassFailTestClass).Run(listener, convention);

                var testClass = FullName<PassFailTestClass>();

                console.Lines()
                       .Select(x => Regex.Replace(x, @":line \d+", ":line #")) //Avoid brittle assertion introduced by stack trace line numbers.
                       .Select(x => Regex.Replace(x, @"duration='\d+'", "duration='#'")) //Avoid brittle assertion introduced by durations.
                       .ShouldEqual(
                           "##teamcity[testSuiteStarted name='Fixie.Tests.dll']",
                           "##teamcity[testIgnored name='" + testClass + ".SkipWithReason' message='Skipped with reason.']",
                           "##teamcity[testIgnored name='" + testClass + ".SkipWithoutReason' message='']",

                           "Console.Out: Fail",
                           "Console.Error: Fail",
                           "Console.Out: FailByAssertion",
                           "Console.Error: FailByAssertion",
                           "Console.Out: Pass",
                           "Console.Error: Pass",

                           "##teamcity[testStarted name='" + testClass + ".Fail']",
                           "##teamcity[testStdOut name='" + testClass + ".Fail' out='Console.Out: Fail|r|nConsole.Error: Fail|r|n']",
                           "##teamcity[testFailed name='" + testClass + ".Fail' message='|'Fail|' failed!' details='Fixie.Tests.FailureException|r|n|'Fail|' failed!|r|n" + At<PassFailTestClass>("Fail()") + "']",
                           "##teamcity[testFinished name='" + testClass + ".Fail' duration='#']",
                           "##teamcity[testStarted name='" + testClass + ".FailByAssertion']",
                           "##teamcity[testStdOut name='" + testClass + ".FailByAssertion' out='Console.Out: FailByAssertion|r|nConsole.Error: FailByAssertion|r|n']",
                           "##teamcity[testFailed name='" + testClass + ".FailByAssertion' message='Assert.Equal() Failure|r|nExpected: 2|r|nActual:   1' details='Should.Core.Exceptions.EqualException|r|nAssert.Equal() Failure|r|nExpected: 2|r|nActual:   1|r|n" + At<PassFailTestClass>("FailByAssertion()") + "']",
                           "##teamcity[testFinished name='" + testClass + ".FailByAssertion' duration='#']",
                           "##teamcity[testStarted name='" + testClass + ".Pass']",
                           "##teamcity[testStdOut name='" + testClass + ".Pass' out='Console.Out: Pass|r|nConsole.Error: Pass|r|n']",
                           "##teamcity[testFinished name='" + testClass + ".Pass' duration='#']",
                           "##teamcity[testSuiteFinished name='Fixie.Tests.dll']");
            }
        }

        class PassFailTestClass
        {
            public void Fail()
            {
                WhereAmI();
                throw new FailureException();
            }

            public void FailByAssertion()
            {
                WhereAmI();
                1.ShouldEqual(2);
            }

            public void Pass()
            {
                WhereAmI();
            }

            [Skip]
            public void SkipWithoutReason() { throw new ShouldBeUnreachableException(); }

            [Skip("Skipped with reason.")]
            public void SkipWithReason() { throw new ShouldBeUnreachableException(); }

            static void WhereAmI([CallerMemberName] string member = null)
            {
                Console.Out.WriteLine("Console.Out: " + member);
                Console.Error.WriteLine("Console.Error: " + member);
            }
        }
    }
}
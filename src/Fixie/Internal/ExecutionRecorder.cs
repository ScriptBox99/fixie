﻿using System.Diagnostics;
using System.Threading.Channels;
using Fixie.Reports;

namespace Fixie.Internal;

class ExecutionRecorder
{
    readonly Channel<IMessage> channel;

    readonly ExecutionSummary assemblySummary;
    readonly Stopwatch assemblyStopwatch;
    readonly Stopwatch caseStopwatch;

    public ExecutionRecorder(Channel<IMessage> channel)
    {
        this.channel = channel;

        assemblySummary = new ExecutionSummary();

        assemblyStopwatch = new Stopwatch();
        caseStopwatch = new Stopwatch();
    }

    public async Task StartExecution()
    {
        var message = new ExecutionStarted();
        await channel.Writer.WriteAsync(message);
        assemblyStopwatch.Restart();
        caseStopwatch.Restart();
    }

    public async Task Start(Test test)
    {
        var message = new TestStarted(test);
        await channel.Writer.WriteAsync(message);
    }

    public async Task Skip(Test test, string name, string reason)
    {
        var duration = caseStopwatch.Elapsed;

        var message = new TestSkipped(test.Name, name, duration, reason);
        assemblySummary.Add(message);
        await channel.Writer.WriteAsync(message);

        caseStopwatch.Restart();
    }

    public async Task Pass(Test test, string name)
    {
        var duration = caseStopwatch.Elapsed;

        var message = new TestPassed(test.Name, name, duration);
        assemblySummary.Add(message);
        await channel.Writer.WriteAsync(message);

        caseStopwatch.Restart();
    }

    public async Task Fail(Test test, string name, Exception reason)
    {
        var duration = caseStopwatch.Elapsed;

        var message = new TestFailed(test.Name, name, duration, reason);
        assemblySummary.Add(message);
        await channel.Writer.WriteAsync(message);

        caseStopwatch.Restart();
    }

    public async Task<ExecutionSummary> CompleteExecution()
    {
        var duration = assemblyStopwatch.Elapsed;
        caseStopwatch.Stop();
        assemblyStopwatch.Stop();

        var message = new ExecutionCompleted(assemblySummary, duration);
        await channel.Writer.WriteAsync(message);

        return assemblySummary;
    }
}
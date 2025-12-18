using Temporalio.Client;
using Temporalio.Testing;
using Temporalio.Worker;
using TemporalioSamples.NexusSimple;
using TemporalioSamples.NexusSimple.Caller;
using TemporalioSamples.NexusSimple.Handler;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable SA1028
namespace TemporalioSamples.Tests.NexusSimple;

public class CallerWorkflowTests : WorkflowEnvironmentTestBase
{
    // ReSharper disable once InconsistentNaming
#pragma warning disable SA1311
#pragma warning disable SA1311
    private static readonly WorkflowEnvironmentStartTimeSkippingOptions opts = new()
#pragma warning restore SA1311
#pragma warning restore SA1311
    {
        TestServer = new TestServerOptions
        {
            DownloadVersion = "latest",
            ExtraArgs =
            [
                "--tls=false",
                "--log-level debug",
                // "--tls-disable-host-verification=true",
                // "--dynamic-config-value",
                // "frontend.enableUpdateWorkflowExecution=true",
                // Enable multi-op
                // "--dynamic-config-value",
                // "frontend.enableExecuteMultiOperation=true",
                // "--dynamic-config-value",
                // "frontend.disableHostVerification=true",
                // "--dynamic-config-value",
                // "internode.disableHostVerification=true",
                // "--dynamic-config-value",
                // "insecure-skip-verify=true"
            ],
        },
    };

    private static Task<string>? lazyHandlerTaskQueue;
    
    private ITemporalClient client;

    private Temporalio.Testing.WorkflowEnvironment env;

    public CallerWorkflowTests(ITestOutputHelper output, WorkflowEnvironment env)
        : base(output, env)
    {
    }

    public Task<string> EnsureHandlerTaskQueueAsync()
    {
        return LazyInitializer.EnsureInitialized(ref lazyHandlerTaskQueue, async () =>
        {
            var handlerTaskQueue = $"tq-{Guid.NewGuid()}";
            env = await Temporalio.Testing.WorkflowEnvironment.StartLocalAsync(new()
            {
                DevServerOptions = new()
                {
                    DownloadVersion = "latest",
                    ExtraArgs =
                    [
                        "--dynamic-config-value",
                        "frontend.enableUpdateWorkflowExecution=true",
                        // Enable multi-op
                        "--dynamic-config-value",
                        "frontend.enableExecuteMultiOperation=true"
                    ],
                },
            }).ConfigureAwait(false);
            client = env.Client;
            await env.CreateNexusEndpointAsync(IHelloService.EndpointName, handlerTaskQueue).ConfigureAwait(false);
            return handlerTaskQueue;
        });
    }

    [TimeSkippingServerFact]
    public async Task RunAsync_EchoCallerWorkflow_Succeeds()
    {
        // Run handler worker
        var handlerTaskQueue = await EnsureHandlerTaskQueueAsync();
        using var handlerWorker = new TemporalWorker(
            client,
            new TemporalWorkerOptions(handlerTaskQueue).AddNexusService(new HelloService())
                .AddWorkflow<HelloHandlerWorkflow>());
        await handlerWorker.ExecuteAsync(async () =>
        {
            // Run caller worker
            await using var env2 = await Temporalio.Testing.WorkflowEnvironment.StartTimeSkippingAsync(opts).ConfigureAwait(false);

            using var callerWorker = new TemporalWorker(
                env2.Client,
                new TemporalWorkerOptions($"tq-{Guid.NewGuid()}").AddWorkflow<EchoCallerWorkflow>());
            await callerWorker.ExecuteAsync(async () =>
            {
                // Run workflow, confirm result
                var result = await env2.Client.ExecuteWorkflowAsync(
                    (EchoCallerWorkflow wf) => wf.RunAsync("some-message"),
                    new WorkflowOptions($"wf-{Guid.NewGuid()}", callerWorker.Options.TaskQueue!));
                Assert.Equal("some-message", result);
            });
        }).ConfigureAwait(false);
    }

    // [TimeSkippingServerFact]
    // public async Task RunAsync_HelloCallerWorkflow_Succeeds()
    // {
    //     // Run handler worker
    //     var handlerTaskQueue = await EnsureHandlerTaskQueueAsync();
    //     using var handlerWorker = new TemporalWorker(
    //         client,
    //         new TemporalWorkerOptions(handlerTaskQueue).AddNexusService(new HelloService())
    //             .AddWorkflow<HelloHandlerWorkflow>());
    //     await handlerWorker.ExecuteAsync(async () =>
    //     {
    //         // Run caller worker
    //         using var callerWorker = new TemporalWorker(
    //             client,
    //             new TemporalWorkerOptions($"tq-{Guid.NewGuid()}").AddWorkflow<HelloCallerWorkflow>());
    //         await callerWorker.ExecuteAsync(async () =>
    //         {
    //             // Run workflow, confirm result
    //             var result = await Client.ExecuteWorkflowAsync(
    //                 (HelloCallerWorkflow wf) => wf.RunAsync("some-name", IHelloService.HelloLanguage.Fr),
    //                 new WorkflowOptions($"wf-{Guid.NewGuid()}", callerWorker.Options.TaskQueue!));
    //             Assert.Equal("Bonjour some-name 👋", result);
    //         });
    //     });
    // }
}
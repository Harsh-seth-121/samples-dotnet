using Temporalio.Testing;

namespace TemporalioSamples.Tests;

using System;
using Temporalio.Client;
using Xunit;

public class WorkflowEnvironment : IAsyncLifetime
{
    private Temporalio.Testing.WorkflowEnvironment? env;

    public Temporalio.Testing.WorkflowEnvironment TestEnv =>
        env ?? throw new InvalidOperationException("Environment not created");

    public ITemporalClient Client =>
        env?.Client ?? throw new InvalidOperationException("Environment not created");

    public async Task InitializeAsync()
    {
        /*
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
        });
        */

        var opts = new WorkflowEnvironmentStartTimeSkippingOptions()
        {
            TestServer = new TestServerOptions()
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
        };
        env = await Temporalio.Testing.WorkflowEnvironment.StartTimeSkippingAsync(opts);
    }

    public async Task DisposeAsync()
    {
        if (env != null)
        {
            await env.ShutdownAsync();
        }
    }
}

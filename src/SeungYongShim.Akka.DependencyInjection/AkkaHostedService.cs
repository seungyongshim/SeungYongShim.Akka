using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection
{
    internal delegate void AkkaHostedServiceStart(ActorSystem actorSystem);

    internal class AkkaHostedService : IHostedService
    {
        public AkkaHostedService(IServiceProvider serviceProvider, ActorSystem actorSystem, AkkaHostedServiceStart akkaHostedServiceStart)
        {
            ServiceProvider = serviceProvider;
            ActorSystem = actorSystem;
            AkkaHostedServiceStart = akkaHostedServiceStart;
        }

        public IServiceProvider ServiceProvider { get; }
        public ActorSystem ActorSystem { get; }
        public AkkaHostedServiceStart AkkaHostedServiceStart { get; }

        public async Task StartAsync(CancellationToken cancellationToken) 
        {
            AkkaHostedServiceStart(ActorSystem);

            await Task.Delay(300);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
            {
                ActorSystem.Terminate().Wait(cts.Token);
            }

            return Task.CompletedTask;
        }
    }
}

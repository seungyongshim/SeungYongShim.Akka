using System;
using Akka.Actor;
using Akka.Actor.Setup;
using Akka.Configuration;
using Akka.DependencyInjection;
using SeungYongShim.Akka.DependencyInjection.Abstractions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AddAkkaExtensions
    {

        internal static IServiceCollection AddAkka(this IServiceCollection services,
                                                   string actorSystemName,
                                                   string hocon,
                                                   Action<IServiceProvider, ActorSystem> startAction = null) =>
            services.AddSingleton(sp =>
                BootstrapSetup.Create()
                              .WithConfig(ConfigurationFactory.ParseString(hocon))
                              .And(ServiceProviderSetup.Create(sp)))
                    .AddSingleton<AkkaHostedServiceStart>(sp => sys => startAction?.Invoke(sp, sys))
                    .AddSingleton(typeof(IPropsFactory<>), typeof(PropsFactory<>))
                    .AddHostedService<AkkaHostedService>()
                    .AddSingleton(sp => ActorSystem.Create(actorSystemName, sp.GetService<ActorSystemSetup>()));
    }
}

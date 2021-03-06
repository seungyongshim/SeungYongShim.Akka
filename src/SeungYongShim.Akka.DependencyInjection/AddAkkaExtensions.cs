using System;
using Akka.Actor;
using Akka.Actor.Setup;
using Akka.Configuration;
using Akka.DependencyInjection;
using SeungYongShim.Akka;
using SeungYongShim.Akka.DependencyInjection.Abstractions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AddAkkaExtensions
    {
        internal static IServiceCollection AddAkka(this IServiceCollection services,
                                                   string actorSystemName,
                                                   string hocon,
                                                   Func<Config, Config> hoconFunc,
                                                   Action<IServiceProvider, ActorSystem> startAction = null) =>
            services.AddSingleton(sp =>
            {
                return BootstrapSetup.Create()
                                     .WithConfig(hoconFunc(ConfigurationFactory.ParseString(hocon)))
                                     .And(DependencyResolverSetup.Create(sp));
            })
            .AddSingleton<AkkaHostedServiceStart>(sp => sys =>
            {
                startAction?.Invoke(sp, sys);
            })
            .AddSingleton(typeof(IPropsFac<>), typeof(PropsFac<>))
            .AddHostedService<AkkaHostedService>()
            .AddSingleton(sp => ActorSystem.Create(actorSystemName, sp.GetService<ActorSystemSetup>()));
    }
}

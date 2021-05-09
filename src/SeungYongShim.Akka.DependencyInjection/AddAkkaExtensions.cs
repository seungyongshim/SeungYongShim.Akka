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
                                     .And(ServiceProviderSetup.Create(sp));
            })
            .AddSingleton<AkkaHostedServiceStart>(sp => sys =>
            {
                startAction?.Invoke(sp, sys);
            })
            .AddSingleton(typeof(IPropsFactory<>), typeof(PropsFactory<>))
            .AddHostedService<AkkaHostedService>()
            .AddSingleton(sp => ActorSystem.Create(actorSystemName, sp.GetService<ActorSystemSetup>()));
    }
}

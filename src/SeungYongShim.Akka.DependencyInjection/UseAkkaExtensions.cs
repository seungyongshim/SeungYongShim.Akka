using System;
using System.Runtime.CompilerServices;
using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("Mirero.Akka.Extensions.DependencyInjection.XUnit2")]

namespace Microsoft.Extensions.Hosting
{
    public static class UseAkkaExtensions
    {
        public static IHostBuilder UseAkka(this IHostBuilder host,
                                           string actorSystemName,
                                           string hocon,
                                           Action<IServiceProvider, ActorSystem> startAction = null) =>
            host.ConfigureServices((context, services) =>
                services.AddAkka(actorSystemName, hocon, startAction));
    }
}

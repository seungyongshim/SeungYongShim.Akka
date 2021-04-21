using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Actor.Setup;
using Akka.TestKit.Xunit2;
using Akka.Util.Internal;
using Microsoft.Extensions.Hosting;
using SeungYongShim.Akka.DependencyInjection;
using SeungYongShim.Akka.DependencyInjection.Abstractions;

namespace Microsoft.Extensions.DependencyInjection
{
    public delegate IActorRef GetTestActor();

    public static class UseAkkaTestKitExtensions
    {
        public static IHostBuilder UseAkkaWithXUnit2(this IHostBuilder host) =>
            host.UseAkkaWithXUnit2(default);

        public static IHostBuilder UseAkkaWithXUnit2(this IHostBuilder host,
                                                     params Type [] mockType) =>
            host.ConfigureServices((context, services) =>
                services.AddAkkaTestKit(mockType ?? Enumerable.Empty<Type>()));

        private static IServiceCollection AddAkkaTestKit(this IServiceCollection services, IEnumerable<Type> mocks)
        {
            services.AddSingleton(sp => new TestKit(sp.GetService<ActorSystemSetup>()))
                    .AddSingleton<GetTestActor>(sp => () => sp.GetService<TestKit>().TestActor)
                    .AddSingleton(sp => sp.GetService<TestKit>().Sys);

            mocks.ForEach(x => services.AddSingleton(typeof(IPropsFactory<>).MakeGenericType(x),
                                                     typeof(FakePropsFactory<>).MakeGenericType(x)));

            return services;
        }
    }
}

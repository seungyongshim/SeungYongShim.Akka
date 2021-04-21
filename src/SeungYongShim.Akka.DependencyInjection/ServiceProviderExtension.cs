using Akka.DependencyInjection;
using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;
using SeungYongShim.Akka.DependencyInjection.Abstractions;


namespace SeungYongShim.Akka.DependencyInjection
{
    public static class ServiceProviderExtension
    {
        public static IPropsFactory<T> PropsFactory<T>(this ActorSystem actorSystem) where T : ActorBase =>
            ServiceProvider.For(actorSystem).Provider.GetRequiredService<IPropsFactory<T>>();

        public static IPropsFactory<T> PropsFactory<T>(this IUntypedActorContext context) where T : ActorBase =>
            ServiceProvider.For(context.System).Provider.GetRequiredService<IPropsFactory<T>>();
    }
}

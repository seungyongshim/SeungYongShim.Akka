using Akka.DependencyInjection;
using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;
using SeungYongShim.Akka.DependencyInjection.Abstractions;


namespace SeungYongShim.Akka.DependencyInjection
{
    public static class ServiceProviderExtension
    {
        public static IPropsFactory<T> PropsFactory<T>(this ActorSystem actorSystem) where T : ActorBase =>
            DependencyResolver.For(actorSystem).Resolver.GetService<IPropsFactory<T>>();

        public static IPropsFactory<T> PropsFactory<T>(this IUntypedActorContext context) where T : ActorBase =>
            DependencyResolver.For(context.System).Resolver.GetService<IPropsFactory<T>>();
    }
}

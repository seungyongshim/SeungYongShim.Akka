using Akka.Actor;
using SeungYongShim.Akka.DependencyInjection.Abstractions;

namespace Akka.DependencyInjection
{
    public class PropsFac<T> : IPropsFac<T> where T : ActorBase
    {
        public PropsFac(ActorSystem actorSystem) =>
            Resolver = DependencyResolver.For(actorSystem).Resolver;

        public IDependencyResolver Resolver { get;}

        public Props Create(params object [] args) =>
            Resolver.Props<T>(args);
    }
}

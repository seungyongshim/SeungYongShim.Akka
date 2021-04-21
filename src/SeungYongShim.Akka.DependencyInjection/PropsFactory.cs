using Akka.Actor;
using SeungYongShim.Akka.DependencyInjection.Abstractions;

namespace Akka.DependencyInjection
{
    public class PropsFactory<T> : IPropsFactory<T> where T : ActorBase
    {
        public PropsFactory(ActorSystem actorSystem) => ActorSystem = actorSystem;

        public ActorSystem ActorSystem { get; }

        public Props Create(params object [] args) => ServiceProvider.For(ActorSystem).Props<T>(args);
    }
}

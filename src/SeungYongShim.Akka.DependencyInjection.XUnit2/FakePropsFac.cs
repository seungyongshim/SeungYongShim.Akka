using Akka.Actor;
using Akka.DependencyInjection;
using SeungYongShim.Akka.DependencyInjection.Abstractions;

namespace SeungYongShim.Akka.DependencyInjection
{
    internal class FakePropsFac<T> : IPropsFac<T> where T : ActorBase
    {
        public FakePropsFac(ActorSystem actorSystem) => ActorSystem = actorSystem;

        public ActorSystem ActorSystem { get; }

        public Props Create(params object[] args) =>
            DependencyResolver.For(ActorSystem).Props<FakeActor>(args);
    }
}

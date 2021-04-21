using System;
using Akka.Actor;
using Akka.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using SeungYongShim.Akka.DependencyInjection.Abstractions;

namespace SeungYongShim.Akka.DependencyInjection
{
    
    internal class FakePropsFactory<T> : IPropsFactory<T> where T : ActorBase
    {
        public FakePropsFactory(ActorSystem actorSystem) => ActorSystem = actorSystem;

        public ActorSystem ActorSystem { get; }

        public Props Create(params object[] args) => ServiceProvider.For(ActorSystem).Props<FakeActor>(args);
    }

    internal class FakeActor : ReceiveActor
    {
        public FakeActor(IServiceProvider sp) : this(sp, default) { }
        public FakeActor(IServiceProvider sp, object o1) : this(sp, o1, default) { }
        public FakeActor(IServiceProvider sp, object o1, object o2) : this(sp, o1, o2, default) { }
        public FakeActor(IServiceProvider sp, object o1, object o2, object o3) : this(sp, o1, o2, o3, default) { }
        public FakeActor(IServiceProvider sp, object o1, object o2, object o3, object o4) : this(sp, o1, o2, o3, o4, default) { }
        private FakeActor(IServiceProvider sp, params object[] args)
        {
            var testActor = sp.GetService<GetTestActor>()();
            ReceiveAny(testActor.Forward);
        }
    }
}

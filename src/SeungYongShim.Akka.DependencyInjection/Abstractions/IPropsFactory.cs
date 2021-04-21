using Akka.Actor;

namespace SeungYongShim.Akka.DependencyInjection.Abstractions
{
    public interface IPropsFactory<out T> where T : ActorBase
    {
        Props Create(params object[] args);
    }
}





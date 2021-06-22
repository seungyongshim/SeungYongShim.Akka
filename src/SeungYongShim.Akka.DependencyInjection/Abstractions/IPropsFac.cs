using Akka.Actor;

namespace SeungYongShim.Akka.DependencyInjection.Abstractions
{
    public interface IPropsFac<out T> where T : ActorBase
    {
        Props Create(params object[] args);
    }
}





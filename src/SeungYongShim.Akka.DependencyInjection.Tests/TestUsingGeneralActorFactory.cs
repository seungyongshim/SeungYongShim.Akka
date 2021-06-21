using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SeungYongShim.Akka.DependencyInjection;
using Xunit;

namespace Tests
{
    public class ParentActor : ReceiveActor
    {
        public ParentActor()
        {
            var childActor = Context.ActorOf(Context.PropsFactory<ChildActor>().Create(Self), "Child1");

            Receive<string>(msg =>
            {
                childActor.Forward($"{msg}, Kid");
            });
        }
    }

    public class ChildActor : ReceiveActor
    {
        public ChildActor(IServiceProvider sp, IActorRef actorRef) => Receive<string>(m => actorRef.Tell(m));
    }

    public class TestUsingGeneralActorFactory
    {
        [Fact]
        public async Task Check_Child_Actor_Recieved_Messages()
        {
            // Arrange
            var host = Host.CreateDefaultBuilder()
                           .UseAkka("ProductionSystem", string.Empty, (sp, sys) =>
                           {
                               sys.ActorOf(sys.PropsFactory<ParentActor>().Create(), "Parent");
                           })
                           .UseAkkaWithXUnit2(typeof(ChildActor))
                           .Build();

            await host.StartAsync();

            var testKit = host.Services.GetService<TestKit>();

            // Act
            testKit.ActorSelection("/user/Parent").Tell("Hello");

            // Assert
            testKit.ExpectMsg<string>().Should().Be("Hello, Kid");

            var child1 = await testKit.Sys.ActorSelection("/user/Parent/Child1")
                                          .ResolveOne(5.Seconds());
            child1.Path.Name.Should().Be("Child1");

            await host.StopAsync();
        }

        [Fact]
        public async Task Production()
        {
            // Arrange
            var host = Host.CreateDefaultBuilder()
                           .UseAkka("ProductionSystem", string.Empty, (sp, sys) =>
                           {
                               sys.ActorOf(sys.PropsFactory<ParentActor>().Create(), "Parent");
                           })
                           .Build();

            await host.StartAsync();
            var actorSystem = host.Services.GetService<ActorSystem>();

            // Act
            var ret = await actorSystem.ActorSelection("/user/Parent")
                                       .Ask<string>("Hello");

            ret.Should().Be("Hello, Kid");

            await host.StopAsync();
        }
    }
}

using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SeungYongShim.Akka.DependencyInjection;
using Xunit;

namespace SeungYongShim.Akka.OpenTelemetry.Tests
{
    public class ActivityOn : IClassFixture<ActivityCollectionFixture>
    {
        public ActivityCollectionFixture ActivityCollection { get; }

        public ActivityOn(ActivityCollectionFixture activityCollection)
        {
            ActivityCollection = activityCollection;
        }

        public class PingActor : ReceiveActor
        {
            public PingActor(IActorRef pong) => Receive<Sample>(m => pong.Tell(m));
        }

        public class PongActor : ReceiveActor
        {
            public PongActor(IActorRef test) => Receive<Sample>(m => test.Forward(m));
        }

        [Fact]
        public async Task Test1()
        {
            using var host = Host.CreateDefaultBuilder()
                                 .UseAkka("test", string.Empty, conf => conf.WithOpenTelemetry(), (sp, sys) =>
                                 {
                                     var test = sp.GetRequiredService<GetTestActor>()();

                                     var pong = sys.ActorOf(sys.PropsFactory<PongActor>()
                                                               .Create(test), "PongActor");

                                     var ping = sys.ActorOf(sys.PropsFactory<PingActor>()
                                                               .Create(pong), "PingActor");
                                 })
                                 .ConfigureServices(services =>
                                 {
                                     services.AddSingleton(ActivitySourceStatic.Instance);
                                 })
                                 .UseAkkaWithXUnit2()
                                 .Build();

            await host.StartAsync();

            var sys = host.Services.GetRequiredService<ActorSystem>();
            var test = host.Services.GetRequiredService<TestKit>();
            using (var activity = host.Services.GetRequiredService<ActivitySource>().StartActivity("start"))
            {
                var pingActor = await sys.ActorSelection("/user/PingActor")
                                         .ResolveOne(3.Seconds());

                pingActor.Tell(new Sample { ID = "1" });
                test.ExpectMsg<Sample>().Should().Be(new Sample { ID = "1" });

                await Task.Delay(300);
                ActivityCollection.Activities.Where(x => x.RootId == activity.RootId).Count().Should().Be(3);
            }

            await host.StopAsync();
        }
    }
}

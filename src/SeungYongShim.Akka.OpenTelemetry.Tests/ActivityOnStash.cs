using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SeungYongShim.Akka.DependencyInjection;
using Xunit;

namespace SeungYongShim.Akka.OpenTelemetry.Tests
{
    public class ActivityOnStash : IClassFixture<ActivityCollectionFixture>
    {
        public ActivityCollectionFixture ActivityCollection { get; }

        public ActivityOnStash(ActivityCollectionFixture activityCollection)
        {
            ActivityCollection = activityCollection;
        }
        public class StartSend { }

        public class PingActor : ReceiveActor, IWithUnboundedStash
        {
            public PingActor(IActorRef pong)
            {
                Pong = pong;
                Receive<Sample>(m =>
                {
                    Stash.Stash();
                });

                Receive<StartSend>(m =>
                {
                    Become(Start);
                    Stash.UnstashAll();

                    //throw new Exception();
                });
            }

            private void Start()
            {
                Receive<Sample>(m =>
                {
                    Pong.Tell(m);
                });
            }

            public IStash Stash { get; set; }
            public IActorRef Pong { get; }
        }

        public class PongActor : ReceiveActor, IWithUnboundedStash
        {
            public PongActor(IActorRef test) => Receive<Sample>(m => test.Forward(m));

            public IStash Stash { get; set; }
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
                pingActor.Tell(new StartSend());

                test.ExpectMsg<Sample>(3.Seconds()).Should().Be(new Sample { ID = "1" });

                await Task.Delay(300);
                ActivityCollection.Activities.Where(x => x.RootId == activity.RootId).Count().Should().Be(5);
            }

            await host.StopAsync();
        }
    }
}

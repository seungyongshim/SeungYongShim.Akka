using System;
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
using OpenTelemetry.Trace;
using SeungYongShim.Akka.DependencyInjection;
using Xunit;

namespace SeungYongShim.Akka.OpenTelemetry.Tests
{
    public class ActivityOnStash
    {
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
                });

            }

            void Start()
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
            var memoryExport = new List<Activity>();

            using var host = Host.CreateDefaultBuilder()
                                 .UseAkka("test", @"
                                 akka {
                                   actor {
                                     provider = ""SeungYongShim.Akka.OpenTelemetry.TraceLocalActorRefProvider, SeungYongShim.Akka.OpenTelemetry""
                                   }
                                 }
                                 ", (sp, sys) =>
                                 {
                                     var test = sp.GetRequiredService<GetTestActor>()();

                                     var pong = sys.ActorOf(sys.PropsFactory<PongActor>()
                                                               .Create(test), "PongActor");

                                     var ping = sys.ActorOf(sys.PropsFactory<PingActor>()
                                                               .Create(pong), "PingActor");
                                 })
                                 .ConfigureServices(services =>
                                 {
                                     services.AddSingleton(new ActivitySource("ActivityOnStash"));
                                     services.AddOpenTelemetryTracing(builder => builder
                                                .AddSource("ActivityOnStash")
                                                .AddSource("SeungYongShim.Akka.OpenTelemetry")
                                                .SetSampler(new AlwaysOnSampler())
                                                .AddZipkinExporter()
                                                .AddInMemoryExporter(memoryExport));
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
                memoryExport.Where(x => x.RootId == activity.RootId).Count().Should().Be(5);
            }

            await host.StopAsync();
        }
    }
}

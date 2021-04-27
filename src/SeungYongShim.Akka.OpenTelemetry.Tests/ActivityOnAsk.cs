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
    public class ActivityOnAsk
    {
        public class PingActor : ReceiveActor
        {
            public PingActor() => Receive<Sample>(m => Sender.Tell(m));
        }


        // xunit is not support isolation process test.
        // https://github.com/xunit/xunit/issues/1044
        [Fact]
        public async Task Test1()
        {
            var memoryExport = new List<Activity>();

            using var host = Host.CreateDefaultBuilder()
                                 .UseAkka("test", string.Empty, conf => conf.WithOpenTelemetry(), (sp, sys) =>
                                 {
                                     var ping = sys.ActorOf(sys.PropsFactory<PingActor>()
                                                               .Create(), "PingActor");
                                 })
                                 .ConfigureServices(services =>
                                     services.AddSingleton(new ActivitySource("ActivityOnAsk"))
                                             .AddOpenTelemetryTracing((builder) => builder
                                             .AddSource("ActivityOnAsk")
                                             .AddSource("SeungYongShim.Akka.OpenTelemetry")
                                             .SetSampler(new AlwaysOnSampler())
                                             .AddZipkinExporter()
                                             .AddInMemoryExporter(memoryExport)))
                                 .UseAkkaWithXUnit2()
                                 .Build();

            await host.StartAsync();

            var sys = host.Services.GetRequiredService<ActorSystem>();
            var test = host.Services.GetRequiredService<TestKit>();
            using (var activity = host.Services.GetRequiredService<ActivitySource>().StartActivity("start"))
            {
                var pingActor = await sys.ActorSelection("/user/PingActor")
                                         .ResolveOne(3.Seconds());

                var ret = await pingActor.Ask<Sample>(new Sample { ID = "1" });

                ret.Should().Be(new Sample { ID = "1" });

                await Task.Delay(300);
                memoryExport.Where(x => x.RootId == activity.RootId).Count().Should().Be(1);
            }

            await host.StopAsync();
        }
    }
}

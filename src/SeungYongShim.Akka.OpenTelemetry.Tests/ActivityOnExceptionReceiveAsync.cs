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
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.ActivityTags;
using Serilog.Enrichers.Span;
using Serilog.Sinks.Kafka;
using SeungYongShim.Akka.DependencyInjection;
using Xunit;

namespace SeungYongShim.Akka.OpenTelemetry.Tests
{
    public class ActivityOnExceptionReceiveAsync : IClassFixture<ActivityCollectionFixture>
    {
        public ActivityOnExceptionReceiveAsync(ActivityCollectionFixture activityCollection)
        {
            ActivityCollection = activityCollection;
        }

        public List<Activity> MemoryExport { get; private set; }
        public ActivityCollectionFixture ActivityCollection { get; }

        public class PingActor : ReceiveActor
        {
            private async Task Crash()
            {
                await Task.CompletedTask;
                throw new Exception();
            }

            public PingActor() => ReceiveAsync<Sample>(async m => await Crash());
        }

        [Fact]
        public async Task ReceiveAsync()
        {
            using var host = Host.CreateDefaultBuilder()
                                 .UseAkka("test", @"
                                 akka {
                                    loggers=[""Akka.Logger.Serilog.SerilogLogger, Akka.Logger.Serilog""]
                                    loglevel=DEBUG
                                 }", conf => conf.WithOpenTelemetry(), (sp, sys) =>
                                 {
                                     var test = sp.GetRequiredService<GetTestActor>()();

                                     var ping = sys.ActorOf(sys.PropsFactory<PingActor>()
                                                               .Create(), "PingActor");
                                 })
                                 .ConfigureServices(services =>
                                 {
                                     services.AddSingleton(ActivitySourceStatic.Instance);
                                     services.AddOpenTelemetryTracing(builder => builder
                                                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("ExceptionReceiveAsync"))
                                                .AddSource("SeungYongShim.Akka.OpenTelemetry")
                                                .SetSampler(new AlwaysOnSampler())
                                                .AddOtlpExporter()
                                                .AddZipkinExporter());
                                 })
                                 .UseAkkaWithXUnit2()
                                 .UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration
                                        .MinimumLevel.Debug()
                                        .Enrich.With<ActivityEnricher>()
                                        .Enrich.With<ActivityTagsEnricher>()
                                        .WriteTo.Kafka())
                                 .Build();

            await host.StartAsync();

            var sys = host.Services.GetRequiredService<ActorSystem>();
            var test = host.Services.GetRequiredService<TestKit>();
            using (var activity = host.Services.GetRequiredService<ActivitySource>().StartActivity("start"))
            {
                var pingActor = await sys.ActorSelection("/user/PingActor")
                                         .ResolveOne(3.Seconds());

                pingActor.Tell(new Sample { ID = "1" });

                await Task.Delay(1000);
                ActivityCollection.Activities
                                  .Where(x => x.RootId == activity.RootId)
                                  .First()
                                  .Tags
                                  .ToDictionary(x => x.Key, x => x.Value)
                                  ["otel.status_code"]
                                  .Should().Be("ERROR");
            }

            await host.StopAsync();
        }
    }
}

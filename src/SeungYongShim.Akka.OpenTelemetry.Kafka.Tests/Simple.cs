using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using FluentAssertions;
using FluentAssertions.Extensions;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SeungYongShim.Akka.DependencyInjection;
using SeungYongShim.Kafka;
using Xunit;
using static SeungYongShim.Akka.OpenTelemetry.Kafka.KafkaProducerActor;

namespace SeungYongShim.Akka.OpenTelemetry.Kafka.Tests
{
    public class KafkaSpec
    {
        public class AggregateActor : ReceiveActor
        {
            private readonly string _groupId = "akkaunittest";
            public IList<string> Topics { get; }
            public IActorRef KafkaConsumerActor { get; }
            public IActorRef KafkaProducerActor { get; }

            public AggregateActor(IActorRef testActor, string topic)
            {
                Topics = new[] { topic };

                KafkaConsumerActor = Context.ActorOf(Context.PropsFactory<KafkaConsumerActor>()
                                                            .Create(Topics, _groupId, testActor),
                                                     "KafkaConsumerActor");
                KafkaProducerActor = Context.ActorOf(Context.PropsFactory<KafkaProducerActor>()
                                                            .Create(),
                                                     "KafkaProducerActor");

                ReceiveAsync<Sample>(async msg =>
                {
                    var ret = await KafkaProducerActor.Ask<Result>(new KafkaMessage(msg, topic));

                    switch (ret)
                    {
                        case ResultException m:
                            throw m.Exception;
                    }
                });
            }
        }

        [Fact]
        public async void Simple()
        {
            var timeout = 100000.Seconds();
            var memoryExport = new List<Activity>();
            var bootstrapServers = "localhost:9092";
            var topicName = "kafka.spec.simple.akka.test";

            using (var adminClient = new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = bootstrapServers
            }).Build())
            {
                try
                {
                    await adminClient.DeleteTopicsAsync(new[]
                    {
                        topicName
                    });
                    await Task.Delay(1000);
                }
                catch (DeleteTopicsException)
                {
                }

                await adminClient.CreateTopicsAsync(new TopicSpecification[]
                {
                    new TopicSpecification
                    {
                        Name = topicName,
                        ReplicationFactor = 1,
                        NumPartitions = 1
                    }
                });
            }

            // arrange
            using var host =
                Host.CreateDefaultBuilder()
                    .UseKafka(new KafkaConfig(bootstrapServers, TimeSpan.FromSeconds(10)))
                    .UseAkka("test", string.Empty, c => c.WithOpenTelemetry(), (sp, sys) =>
                    {
                        var test = sp.GetRequiredService<GetTestActor>()();
                        sys.ActorOf(sys.PropsFactory<AggregateActor>().Create(test, topicName), "AggregateActor");
                    })
                    .UseAkkaWithXUnit2()
                    .ConfigureServices(services =>
                    {
                        services.AddOpenTelemetryTracing(builder =>
                            builder.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("SeungYongShim.Akka.OpenTelemetry.Kafka.Tests.Simple"))
                                   .AddSource("SeungYongShim.OpenTelemetry")
                                   .SetSampler(new AlwaysOnSampler())
                                   .AddZipkinExporter()
                                   .AddInMemoryExporter(memoryExport));
                    })
                    .Build();

            await host.StartAsync();

            var sys = host.Services.GetRequiredService<ActorSystem>();
            var test = host.Services.GetRequiredService<TestKit>();

            using (var activity = new ActivitySource("SeungYongShim.OpenTelemetry").StartActivity("start"))
            {
                var aggregateActor = await sys.ActorSelection("/user/AggregateActor")
                                              .ResolveOne(timeout);

                aggregateActor.Tell(new Sample { ID = "1" });
                test.ExpectMsg<IMessage>(timeout)
                    .Should()
                    .Be(new Sample { ID = "1" });

                await Task.Delay(300);
                memoryExport.Where(x => x.RootId == activity.RootId)
                            .Count()
                            .Should()
                            .Be(7);
            }

            await host.StopAsync();
        }
    }
}

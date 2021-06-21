using System;
using System.Collections.Generic;
using System.Diagnostics;
using Akka.Actor;
using SeungYongShim.Kafka;

namespace SeungYongShim.Akka.OpenTelemetry.Kafka
{
    public class KafkaConsumerActor : ReceiveActor
    {
        public KafkaConsumerActor(KafkaConsumer kafkaConsumer,
                                  IList<string> topics,
                                  string groupId,
                                  IActorRef parserActor)
        {
            var self = Context.Self;
            KafkaConsumer = kafkaConsumer;
            KafkaConsumer.Start(groupId, topics);

            ReceiveAsync<KafkaRequest>(async msg =>
            {
                self.Tell(KafkaRequest.Instance);
                var receive = await KafkaConsumer.ConsumeAsync(TimeSpan.FromSeconds(1));
                using var activity = ActivitySourceStatic.Instance.StartActivity("KafkaConsumerActor", ActivityKind.Internal, receive.ActivityId);
                await parserActor.Ask<KafkaCommit>(receive.Message);
                receive.Commit();
            });

            self.Tell(KafkaRequest.Instance);
        }

        private KafkaConsumer KafkaConsumer { get; }

        protected override void PostStop()
        {
            KafkaConsumer.Stop();
            KafkaConsumer.Dispose();
            base.PostStop();
        }

        protected override void PreRestart(Exception reason, object message)
        {
            KafkaConsumer.Stop();
            KafkaConsumer.Dispose();
            base.PreRestart(reason, message);
        }

        public record KafkaCommit();

        internal class KafkaRequest
        {
            public static readonly KafkaRequest Instance = new();
        }
    }
}

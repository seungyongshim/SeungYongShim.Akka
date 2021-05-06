using System;
using System.Collections.Generic;
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

            KafkaConsumer.Run(groupId, topics, m => self.Tell(m));

            ReceiveAsync<Commitable>(async msg =>
            {
                await parserActor.Ask<Commit>(msg.Body);
                msg.Commit();
            });
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

        public record Commit();
    }
}

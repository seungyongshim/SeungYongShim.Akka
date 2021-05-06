using System.Diagnostics;
using Google.Protobuf;
using SeungYongShim.Kafka;
using Akka.Actor;
using System;

namespace SeungYongShim.Akka.OpenTelemetry.Kafka
{
    public class KafkaProducerActor : ReceiveActor
    {
        public record Message(IMessage Body, string Topic, string Key = "1");

        public record Result(Exception Exception);

        public KafkaProducerActor(KafkaProducer kafkaProducer)
        {
            KafkaProducer = kafkaProducer;

            ReceiveAsync<Message>(async msg =>
            {
                var sender = Sender;

                try
                {
                    await kafkaProducer.SendAsync(msg.Body, msg.Topic, msg.Key);
                    sender.Tell(new Result(null));
                }
                catch (Exception ex)
                {
                    sender.Tell(new Result(ex));
                }
            });
        }

        protected override void PostStop()
        {
            KafkaProducer.Dispose();
            base.PostStop();
        }

        protected override void PreRestart(Exception reason, object message)
        {
            KafkaProducer.Dispose();
            base.PreRestart(reason, message);
        }

        public KafkaProducer KafkaProducer { get; }
    }
}

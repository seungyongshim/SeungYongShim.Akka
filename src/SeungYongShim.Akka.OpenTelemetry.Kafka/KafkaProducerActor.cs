using System;
using Akka.Actor;
using Google.Protobuf;
using SeungYongShim.Kafka;

namespace SeungYongShim.Akka.OpenTelemetry.Kafka
{
    public class KafkaProducerActor : ReceiveActor
    {
        public record KafkaMessage(IMessage Body, string Topic, string Key = null);
        public record Result();
        public record ResultException(Exception Exception) : Result;

        public KafkaProducerActor(KafkaProducer kafkaProducer)
        {
            KafkaProducer = kafkaProducer;

            ReceiveAsync<KafkaMessage>(async msg =>
            {
                var sender = Sender;

                try
                {
                    await kafkaProducer.SendAsync(msg.Body, msg.Topic, msg.Key);
                    sender.Tell(new Result());
                }
                catch (Exception ex)
                {
                    sender.Tell(new ResultException(ex));
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

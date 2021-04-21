using System;
using System.Diagnostics;
using Akka.Actor;
using Akka.Actor.Internal;
using Akka.Dispatch;

namespace SeungYongShim.Akka.OpenTelemetry
{
    public class TraceActorCell : ActorCell
    {
        public TraceActorCell(ActorSystemImpl system, IInternalActorRef self, Props props, MessageDispatcher dispatcher, IInternalActorRef parent) : base(system, self, props, dispatcher, parent)
        {
        }

        public override void SendMessage(IActorRef sender, object message)
        {
            var ret = message switch
            {
                IAutoReceivedMessage m => m,
                var m when Activity.Current is not null => new TraceMessage(Activity.Current.Id, m),
                _ => message
            };

            base.SendMessage(sender, ret);
        }

        protected override void ReceiveMessage(object message)
        {
            if (message is TraceMessage m)
            {
                var parentId = m.ActivityId;
                message = m.Body;

                using var activity = ActivitySourceStatic.Instance.StartActivity($"{Self.Path.ToString()}@{message.GetType().Name}", ActivityKind.Internal, parentId);
                activity?.AddTag("ActorPath", Self.Path.ToStringWithUid());

                try
                {
                    activity?.AddTag("message", $"{message}");

                    base.ReceiveMessage(message);
                }
                catch (Exception ex)
                {
                    activity?.AddTag("Exception", ex)
                             .AddTag("otel.status_code", "ERROR");
                    throw;
                }
            }
            else
            {
                base.ReceiveMessage(message);
            }
        }
    }
}

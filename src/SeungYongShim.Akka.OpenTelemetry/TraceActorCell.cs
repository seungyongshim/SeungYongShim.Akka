using System;
using System.Diagnostics;
using System.Reflection;
using Akka.Actor;
using Akka.Actor.Internal;
using Akka.Dispatch;
using Akka.Dispatch.SysMsg;
using Akka.Event;

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
                //IAutoReceivedMessage m => m,
                LogEvent m => m,
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
                var activityId = activity?.Id;
                activity?.AddTag("actor.path", Self.Path.ToStringWithUid());
                activity?.AddTag("actor.message", $"{message}");

                try
                {
                    if (message is IAutoReceivedMessage)
                        base.AutoReceiveMessage(new Envelope(message, Sender));
                    else
                        base.ReceiveMessage(message);
                }
                catch (Exception ex)
                {
                    throw new TraceException(ex, activityId);
                }
            }
            else
            {
                base.ReceiveMessage(message);
            }
        }
    }
}

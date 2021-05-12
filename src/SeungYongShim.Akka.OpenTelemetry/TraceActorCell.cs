using System;
using System.Diagnostics;
using Akka.Actor;
using Akka.Actor.Internal;
using Akka.Dispatch;
using Akka.Event;

namespace SeungYongShim.Akka.OpenTelemetry
{
    public class TraceActorCell : ActorCell
    {
        internal string ActivityNew { get; set; }

        public TraceActorCell(ActorSystemImpl system, IInternalActorRef self, Props props, MessageDispatcher dispatcher, IInternalActorRef parent) : base(system, self, props, dispatcher, parent) => ActivityNew = Activity.Current?.Id;

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

        protected override ActorBase CreateNewActorInstance()
        {
            if (ActivityNew is not null)
            {
                using (var activity = ActivitySourceStatic.Instance.StartActivity($"{Self.Path}@Create", ActivityKind.Internal, ActivityNew))
                {
                    return base.CreateNewActorInstance();
                }
            }
            else
            {
                return base.CreateNewActorInstance();
            }
        }

        protected override void ReceiveMessage(object message)
        {
            switch (message)
            {
                case Error m when m.Cause is TraceException x:
                    using (var activity = ActivitySourceStatic.Instance.StartActivity("Exception", ActivityKind.Internal, x.ActivityId))
                    {
                        activity?.AddTag("actor.path", Self.Path);
                        activity?.AddTagException(x.InnerException?.Demystify());

                        base.ReceiveMessage(message);
                    }
                    return;

                case TraceMessage m:
                    message = m.Body;
                    using (var activity = ActivitySourceStatic.Instance.StartActivity($"{Self.Path}@{message.GetType().FullName}", ActivityKind.Internal, m.ActivityId))
                    {
                        var activityId = activity?.Id;
                        activity?.AddTag("actor.path", Self.Path);
                        activity?.AddTag("actor.library", Props.Type.Assembly.GetName().Name);
                        activity?.AddTag("actor.type", Props.Type.Name);
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
                            ActivityNew = activityId;
                            throw new TraceException(ex, activityId);
                        }
                    }
                    return;

                default:
                    base.ReceiveMessage(message);
                    return;
            }
        }
    }
}

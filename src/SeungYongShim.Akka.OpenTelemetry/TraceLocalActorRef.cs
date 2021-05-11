using System;
using System.Diagnostics;
using Akka.Actor;
using Akka.Actor.Internal;
using Akka.Dispatch;
using Akka.Dispatch.SysMsg;

namespace SeungYongShim.Akka.OpenTelemetry
{

    public class TraceLocalActorRef : LocalActorRef
    {
        public TraceLocalActorRef(ActorSystemImpl system,
                                  Props props,
                                  MessageDispatcher dispatcher,
                                  MailboxType mailboxType,
                                  IInternalActorRef supervisor,
                                  ActorPath path,
                                  Type actorTaskSchedulerMessageType) : base(system, props, dispatcher, mailboxType, supervisor, path)
        {
            ActorTaskSchedulerMessageType = actorTaskSchedulerMessageType;
        }

        public Type ActorTaskSchedulerMessageType { get; }
        public TraceActorCell TraceActorCell { get; private set; }

        public override void SendSystemMessage(ISystemMessage message)
        {
            if (ActorTaskSchedulerMessageType.IsInstanceOfType(message))
            {
                if (ActorTaskSchedulerMessageType.GetProperty("Exception")?
                                                 .GetValue(message) is Exception ex)
                {
                    var m = ActorTaskSchedulerMessageType.GetProperty("Message")?
                                                         .GetValue(message);

                    message = (ISystemMessage)Activator.CreateInstance(ActorTaskSchedulerMessageType,
                                                                       new TraceException(ex), m);

                    TraceActorCell.ActivityNew = Activity.Current?.Id;
                }
            }

            base.SendSystemMessage(message);
        }

        protected override ActorCell NewActorCell(ActorSystemImpl system, IInternalActorRef self, Props props,
            MessageDispatcher dispatcher, IInternalActorRef supervisor)
        {
            TraceActorCell = new TraceActorCell(system, self, props, dispatcher, supervisor);
            return TraceActorCell;
        }
    }
}

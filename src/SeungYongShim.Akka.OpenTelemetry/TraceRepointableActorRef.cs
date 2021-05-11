using System;
using System.Diagnostics;
using Akka.Actor;
using Akka.Actor.Internal;
using Akka.Dispatch;
using Akka.Dispatch.SysMsg;
using System.Reflection;

namespace SeungYongShim.Akka.OpenTelemetry
{
    public class TraceRepointableActorRef : RepointableActorRef
    {
        private MailboxType _mailboxType;
        private Type ActorTaskSchedulerMessageType;

        public TraceRepointableActorRef(ActorSystemImpl system,
                                        Props props,
                                        MessageDispatcher dispatcher,
                                        MailboxType mailboxType,
                                        IInternalActorRef supervisor,
                                        ActorPath path,
                                        Type actorTaskSchedulerMessageType)
            : base(system, props, dispatcher, mailboxType, supervisor, path)
        {
            _mailboxType = mailboxType;
            ActorTaskSchedulerMessageType = actorTaskSchedulerMessageType;
        }

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

        protected override ActorCell NewCell()
        {
            TraceActorCell = new TraceActorCell(System, this, Props, Dispatcher, Supervisor);
            TraceActorCell.Init(false, _mailboxType);
            return TraceActorCell;
        }
    }
}

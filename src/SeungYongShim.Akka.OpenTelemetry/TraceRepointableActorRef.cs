using System;
using System.Diagnostics;
using Akka.Actor;
using Akka.Actor.Internal;
using Akka.Dispatch;
using Akka.Dispatch.SysMsg;

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

        public override void SendSystemMessage(ISystemMessage message)
        {
            if (ActorTaskSchedulerMessageType.IsInstanceOfType(message))
            {
                if (ActorTaskSchedulerMessageType.GetProperty("Exception")?
                                                 .GetValue(message) is Exception ex)
                {
                    ActorTaskSchedulerMessageType.GetProperty("Exception")?.SetValue(message, new TraceException(ex));
                }
            }
            
            base.SendSystemMessage(message);
        }

        protected override ActorCell NewCell()
        {
            var actorCell = new TraceActorCell(System, this, Props, Dispatcher, Supervisor);
            actorCell.Init(false, _mailboxType);
            return actorCell;
        }
    }
}

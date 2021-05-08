using System;
using System.Diagnostics;
using System.Reflection;
using Akka.Actor;
using Akka.Actor.Internal;
using Akka.Dispatch;
using Akka.Dispatch.SysMsg;

namespace SeungYongShim.Akka.OpenTelemetry
{
    public class TraceRepointableActorRef : RepointableActorRef
    {
        private MailboxType _mailboxType;
        Type ActorTaskSchedulerMessageType;

        public TraceRepointableActorRef(ActorSystemImpl system, Props props, MessageDispatcher dispatcher, MailboxType mailboxType, IInternalActorRef supervisor, ActorPath path) : base(system, props, dispatcher, mailboxType, supervisor, path)
        {
            _mailboxType = mailboxType;
            Assembly design = Assembly.GetAssembly(typeof(RepointableActorRef));
            ActorTaskSchedulerMessageType = design.GetType("Akka.Dispatch.SysMsg.ActorTaskSchedulerMessage");
        }

        public override void SendSystemMessage(ISystemMessage message)
        {
            if (ActorTaskSchedulerMessageType.IsInstanceOfType(message))
            {
                if (ActorTaskSchedulerMessageType.GetProperty("Exception")?
                                                 .GetValue(message) is Exception ex)
                {
                    var activity = Activity.Current;
                    activity?.AddTagException(ex);
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

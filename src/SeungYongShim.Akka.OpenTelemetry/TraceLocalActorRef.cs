using System.Diagnostics;
using Akka.Actor;
using Akka.Actor.Internal;
using Akka.Dispatch;
using Akka.Dispatch.SysMsg;

namespace SeungYongShim.Akka.OpenTelemetry
{

    public class TraceLocalActorRef : LocalActorRef
    {
        public TraceLocalActorRef(ActorSystemImpl system, Props props, MessageDispatcher dispatcher, MailboxType mailboxType, IInternalActorRef supervisor, ActorPath path) : base(system, props, dispatcher, mailboxType, supervisor, path)
        {
        }

        public override void SendSystemMessage(ISystemMessage message) => base.SendSystemMessage(message);

        protected override ActorCell NewActorCell(ActorSystemImpl system, IInternalActorRef self, Props props,
            MessageDispatcher dispatcher, IInternalActorRef supervisor)
        {
            if (Activity.Current is not null)
            {

            }
            return new TraceActorCell(system, self, props, dispatcher, supervisor);
        }
    }
}

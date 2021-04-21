using Akka.Actor;
using Akka.Actor.Internal;
using Akka.Dispatch;

namespace SeungYongShim.Akka.OpenTelemetry
{

    public class TraceLocalActorRef : LocalActorRef
    {
        public TraceLocalActorRef(ActorSystemImpl system, Props props, MessageDispatcher dispatcher, MailboxType mailboxType, IInternalActorRef supervisor, ActorPath path) : base(system, props, dispatcher, mailboxType, supervisor, path)
        {
        }

        protected override ActorCell NewActorCell(ActorSystemImpl system, IInternalActorRef self, Props props,
            MessageDispatcher dispatcher, IInternalActorRef supervisor)
        {
            return new TraceActorCell(system, self, props, dispatcher, supervisor);
        }
    }
}

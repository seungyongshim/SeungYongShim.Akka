using Akka.Actor;
using Akka.Actor.Internal;
using Akka.Dispatch;

namespace SeungYongShim.Akka.OpenTelemetry
{
    public class TraceRepointableActorRef : RepointableActorRef
    {
        private MailboxType _mailboxType;

        public TraceRepointableActorRef(ActorSystemImpl system, Props props, MessageDispatcher dispatcher, MailboxType mailboxType, IInternalActorRef supervisor, ActorPath path) : base(system, props, dispatcher, mailboxType, supervisor, path)
        {
            _mailboxType = mailboxType;
        }

        protected override ActorCell NewCell()
        {
            var actorCell = new TraceActorCell(System, this, Props, Dispatcher, Supervisor);
            actorCell.Init(false, _mailboxType);
            return actorCell;
        }
    }
}

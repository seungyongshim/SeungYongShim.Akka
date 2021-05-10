using System;
using System.Diagnostics;
using Akka.Actor;

namespace SeungYongShim.Akka.OpenTelemetry
{
    public class TraceOneForOneStrategy : OneForOneStrategy
    {
        public TraceOneForOneStrategy(IDecider decider) : base(decider)
        {
        }

        protected override void LogFailure(IActorContext context, IActorRef child, Exception cause, Directive directive)
        {
            if (cause is TraceException ex)
            {
                var parentId = ex.ActivityId;
                using var activity = ActivitySourceStatic.Instance.StartActivity($"Exception", ActivityKind.Internal, parentId);

                activity?.AddTagException(ex.Demystify());

                base.LogFailure(context, child, ex.InnerException, directive);
            }
            else
                base.LogFailure(context, child, cause, directive);
        }
    }
}

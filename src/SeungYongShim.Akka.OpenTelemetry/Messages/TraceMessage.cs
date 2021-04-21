using System.Diagnostics;

namespace SeungYongShim.Akka.OpenTelemetry
{
    public record TraceMessage(string ActivityId, object Body)
    {
        public TraceMessage(Activity activity, object message) :
            this(activity?.Id, message)
        { }
    }
}

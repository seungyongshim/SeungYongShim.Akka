using System.Diagnostics;
using System.Runtime.CompilerServices;
using Google.Protobuf;

namespace SeungYongShim.Akka.OpenTelemetry
{
    public record TraceMessage(string ActivityId, object Body)
    {
        public TraceMessage(Activity activity, object message) :
            this(activity?.Id, message) { }
    }
}

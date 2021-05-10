using System;
using System.Diagnostics;

namespace SeungYongShim.Akka.OpenTelemetry
{
    public class TraceException : Exception
    {
        public TraceException(Exception exception) : this(exception, Activity.Current?.Id)
        {
            
        }

        public TraceException(Exception exception, string activityId) : base("TraceException", exception)
        {
            ActivityId = activityId;
        }

        public string ActivityId { get; }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeungYongShim.Akka.OpenTelemetry
{
    public class TraceException : Exception
    {
        public TraceException(Exception exception) : base("TraceException", exception)
        {
            TraceId = Activity.Current?.TraceId;
        }

        public ActivityTraceId? TraceId { get; }
    }
}

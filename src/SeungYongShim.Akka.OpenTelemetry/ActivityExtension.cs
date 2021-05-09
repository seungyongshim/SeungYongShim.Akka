using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

namespace SeungYongShim.Akka.OpenTelemetry
{
    public static class ActivityExtension
    {
        // https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/exceptions.md
        public static Activity AddTagException(this Activity activity, Exception ex)
        {
            activity.RecordException(ex);
            activity.SetStatus(Status.Error);
            return activity;
        }
            
    }
}

using System;
using System.Diagnostics;

namespace SeungYongShim.Akka.OpenTelemetry
{
    public static class ActivityExtension
    {
        // https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/exceptions.md
        public static Activity AddTagException(this Activity activity, Exception ex) =>
            activity.AddTag("exception.type", ex.GetType().Name)
                    .AddTag("exception.stacktrace", ex.Demystify().StackTrace)
                    .AddTag("exception.message", ex.Message)
                    .AddTag("otel.status_code", "ERROR");
    }
}

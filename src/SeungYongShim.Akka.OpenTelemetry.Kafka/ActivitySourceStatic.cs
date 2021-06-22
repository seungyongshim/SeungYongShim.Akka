using System.Diagnostics;

namespace SeungYongShim.Akka.OpenTelemetry.Kafka
{
    internal static class ActivitySourceStatic
    {
        public static ActivitySource Instance { get; } = new ActivitySource("SeungYongShim.OpenTelemetry");
    }
}

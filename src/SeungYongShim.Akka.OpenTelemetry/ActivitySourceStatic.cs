using System.Diagnostics;

namespace SeungYongShim.Akka.OpenTelemetry
{
    internal static class ActivitySourceStatic
    {
        public static ActivitySource Instance { get; } = new ActivitySource("SeungYongShim.OpenTelemetry");
    }
}

using System.Diagnostics;

namespace SeungYongShim.Akka.OpenTelemetry
{
    public static class ActivitySourceStatic
    {
        public static ActivitySource Instance { get; } = new ActivitySource("SeungYongShim.Akka.OpenTelemetry");
    }
}

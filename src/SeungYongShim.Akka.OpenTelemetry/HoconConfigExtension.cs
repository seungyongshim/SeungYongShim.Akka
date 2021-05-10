using Akka.Configuration;

namespace Microsoft.Extensions.Hosting
{

    


    public static class HoconConfigExtensions
    {
        public static Config WithOpenTelemetry(this Config config) =>
            config.WithFallback(@"
                                 akka {
                                   actor {
                                     provider = ""SeungYongShim.Akka.OpenTelemetry.TraceLocalActorRefProvider, SeungYongShim.Akka.OpenTelemetry""
                                   }
                                 }");
    }
}

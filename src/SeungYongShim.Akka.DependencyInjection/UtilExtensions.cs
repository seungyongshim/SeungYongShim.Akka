namespace Akka.Configuration
{
    public static class UtilExtensions
    {
        public static Config WithFallback(this Config config, string hocon) =>
            config.WithFallback(ConfigurationFactory.ParseString(hocon));
    }
}

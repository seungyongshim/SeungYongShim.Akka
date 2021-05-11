using System;
using System.Configuration;
using System.Reflection;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Actor.Internal;
using Akka.Event;
using Akka.Routing;
using Akka.Serialization;

namespace SeungYongShim.Akka.OpenTelemetry
{
    public sealed class TraceLocalActorRefProvider : IActorRefProvider
    {
        private readonly LocalActorRefProvider _localActorRefProvider;
        private ActorSystemImpl _system;

        public TraceLocalActorRefProvider(string systemName, Settings settings, EventStream eventStream)
            : this(systemName, settings, eventStream, null, null)
        {
        }

        public TraceLocalActorRefProvider(string systemName, Settings settings, EventStream eventStream, Deployer deployer, Func<ActorPath, IInternalActorRef> deadLettersFactory)
        {
            var rootPath = new RootActorPath(new Address("akka", systemName));
            Log = Logging.GetLogger(eventStream, "TraceLocalActorRefProvider(" + rootPath.Address + ")");
            _localActorRefProvider = new LocalActorRefProvider(systemName, settings, eventStream, deployer, deadLettersFactory);

            var design = Assembly.GetAssembly(typeof(RepointableActorRef));
            ActorTaskSchedulerMessageType = design?.GetType("Akka.Dispatch.SysMsg.ActorTaskSchedulerMessage");
        }

        public IInternalActorRef RootGuardian => _localActorRefProvider.RootGuardian;

        public LocalActorRef Guardian => _localActorRefProvider.Guardian;

        public LocalActorRef SystemGuardian => _localActorRefProvider.SystemGuardian;

        public IActorRef DeadLetters => _localActorRefProvider.DeadLetters;

        public IActorRef IgnoreRef => _localActorRefProvider.IgnoreRef;

        public ActorPath RootPath => _localActorRefProvider.RootPath;

        public Settings Settings => _localActorRefProvider.Settings;

        public Deployer Deployer => _localActorRefProvider.Deployer;

        public IInternalActorRef TempContainer => _localActorRefProvider.TempContainer;

        public Task TerminationTask => _localActorRefProvider.TerminationTask;

        public Address DefaultAddress => _localActorRefProvider.DefaultAddress;

        public Information SerializationInformation => _localActorRefProvider.SerializationInformation;

        public Address GetExternalAddressFor(Address address) => _localActorRefProvider.GetExternalAddressFor(address);

        public void Init(ActorSystemImpl system)
        {
            _system = system;
            _localActorRefProvider.Init(system);
        }

        public void RegisterTempActor(IInternalActorRef actorRef, ActorPath path) => _localActorRefProvider.RegisterTempActor(actorRef, path);

        public IActorRef ResolveActorRef(string path) => _localActorRefProvider.ResolveActorRef(path);

        public IActorRef ResolveActorRef(ActorPath actorPath) => _localActorRefProvider.ResolveActorRef(actorPath);

        public IActorRef RootGuardianAt(Address address) => _localActorRefProvider.RootGuardianAt(address);

        public ActorPath TempPath() => _localActorRefProvider.TempPath();

        public void UnregisterTempActor(ActorPath path) => _localActorRefProvider.UnregisterTempActor(path);

        public ILoggingAdapter Log { get; }
        public Type ActorTaskSchedulerMessageType { get; }

        public IInternalActorRef ActorOf(ActorSystemImpl system, Props props, IInternalActorRef supervisor, ActorPath path, bool systemService, Deploy deploy, bool lookupDeploy, bool async)
        {
            if (props.Deploy.RouterConfig is NoRouter)
            {
                if (Settings.DebugRouterMisconfiguration)
                {
                    var d = Deployer.Lookup(path);
                    if (d != null && !(d.RouterConfig is NoRouter))
                        Log.Warning("Configuration says that [{0}] should be a router, but code disagrees. Remove the config or add a RouterConfig to its Props.",
                                    path);
                }

                var props2 = props;

                // mailbox and dispatcher defined in deploy should override props
                var propsDeploy = lookupDeploy ? Deployer.Lookup(path) : deploy;
                if (propsDeploy != null)
                {
                    if (propsDeploy.Mailbox != Deploy.NoMailboxGiven)
                        props2 = props2.WithMailbox(propsDeploy.Mailbox);
                    if (propsDeploy.Dispatcher != Deploy.NoDispatcherGiven)
                        props2 = props2.WithDispatcher(propsDeploy.Dispatcher);
                }

                if (!system.Dispatchers.HasDispatcher(props2.Dispatcher))
                {
                    throw new ConfigurationException($"Dispatcher [{props2.Dispatcher}] not configured for path {path}");
                }

                try
                {
                    // for consistency we check configuration of dispatcher and mailbox locally
                    var dispatcher = _system.Dispatchers.Lookup(props2.Dispatcher);
                    var mailboxType = _system.Mailboxes.GetMailboxType(props2, dispatcher.Configurator.Config);

                    if (async)
                        return
                            new TraceRepointableActorRef(system, props2, dispatcher,
                                mailboxType, supervisor, path, ActorTaskSchedulerMessageType).Initialize(async);
                    return new TraceLocalActorRef(system, props2, dispatcher,
                        mailboxType, supervisor, path, ActorTaskSchedulerMessageType);
                }
                catch (Exception ex)
                {
                    throw new ConfigurationException(
                        $"Configuration problem while creating [{path}] with dispatcher [{props.Dispatcher}] and mailbox [{props.Mailbox}]", ex);
                }
            }
            else //routers!!!
            {
                throw new NotImplementedException("Router not support");
            }
        }
    }
}

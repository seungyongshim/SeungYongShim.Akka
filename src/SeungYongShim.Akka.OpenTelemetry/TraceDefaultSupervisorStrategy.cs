using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;

namespace SeungYongShim.Akka.OpenTelemetry
{

    public class TraceDefaultSupervisorStrategy : DefaultSupervisorStrategy
    {
        public override SupervisorStrategy Create()
        {
            return new TraceOneForOneStrategy(SupervisorStrategy.DefaultDecider);
        }
    }
}

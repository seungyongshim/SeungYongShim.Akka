using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;

namespace SeungYongShim.Akka.OpenTelemetry.Kafka.Tests
{
    public static class SampleExt
    {
        public static Sample AddBody(this Sample sample, IEnumerable<string> items)
        {
            foreach(var x in items)
            {
                sample.Body.Add(x);
            }
            
            return sample;
        }
    }
}

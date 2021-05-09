using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeungYongShim.Akka.OpenTelemetry.Tests
{
    public class ActivityCollectionFixture
    {
        public ConcurrentBag<Activity> Activities { get; }
        public ActivityCollectionFixture()
        {
            Activities = new ConcurrentBag<Activity>();

            ActivitySource.AddActivityListener(new ActivityListener
            {
                ActivityStarted = activity => { },
                ActivityStopped = activity => Activities.Add(activity),
                ShouldListenTo = activitySource => true,
                SampleUsingParentId = (ref ActivityCreationOptions<string> activityOptions) => ActivitySamplingResult.AllDataAndRecorded,
                Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivitySamplingResult.AllDataAndRecorded,
            });
        }
    }
}

using System;

namespace MetricGenerator
{
    /***** Platform types defined in R9 *****/

    // indicates a field is declaring a label name which triggers code gen
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class LabelNamesAttribute : Attribute
    {
    }

    // indicates a method is a metric reporter and triggers code gen
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class MetricAttribute : Attribute
    {
    }

    // a place where to send metrics.
    public interface IMetricRecorder
    {
        void Record(string name, int value, Span<string> labelNames, Span<string> labelValues);
    }

    // a type which holds a bunch of ordered label names
    public class LabelNames
    {
        public LabelNames(params string[] names)
        {
            // not implemented
        }

        public LabelNames(LabelNames ancestors, params string[] names)
        {
            // not implemented
        }

        // return the set of label names, all ancestors are flattened into a single list
        public Span<string> AllNames => new string[0];
    }

    /***** Example of stuff the service developer writes */

    public static partial class Metrics
    {
        // Defines a base set of ambient labels
        [LabelNames]
        private static readonly LabelNames _ambientLabels = new LabelNames("ClusterId", "PodId");

        // Defines a set of ops labels, available in a nested context
        [LabelNames]
        private static readonly LabelNames _opLabels = new LabelNames(_ambientLabels, "Operation");

        // Defines the OperationCount metric and the labels expected with it
        [Metric]
        public static partial void OperationCount(IMetricRecorder recorder, int value, OpLabels labels);
    }

    /***** Stuff we auto-generate based on what the developer wrote above *****/

    public static partial class Metrics
    {
        public static partial void OperationCount(IMetricRecorder recorder, int value, OpLabels labels)
        {
            // ideally this would be a span allocated on the stack, but this is not allowed for strings. So we'll eventually get this array from a pool
            var values = new string[3];
            values[0] = labels.AmbientLabels.ClusterId;
            values[1] = labels.AmbientLabels.PodId;
            values[2] = labels.Operation;
            recorder.Record("OperationCount", value, _opLabels.AllNames, values);
        }

        public readonly struct AmbientLabels
        {
            public readonly string ClusterId;
            public readonly string PodId;

            public AmbientLabels(string clusterId, string podId)
            {
                ClusterId = clusterId;
                PodId = podId;
            }
        }

        public readonly struct OpLabels
        {
            public readonly AmbientLabels AmbientLabels;
            public readonly string Operation;

            public OpLabels(AmbientLabels ambientLabels, string operation)
            {
                AmbientLabels = ambientLabels;
                Operation = operation;
            }
        }
    }

    /***** Example usage of the metric stuff in a service *****/

    public class MyService
    {
        private readonly IMetricRecorder _recorder; // get this from DI

        public void APIDispatch()
        {
            // create the ambient labels
            var ambientLabels = new Metrics.AmbientLabels("123", "456");

            DeleteObject(ambientLabels);
        }

        public void DeleteObject(Metrics.AmbientLabels ambientLabels)
        {
            // create the specialized labels
            var opLabels = new Metrics.OpLabels(ambientLabels, "DeleteObject");

            // increment the metric, supplying all the requisite labels
            Metrics.OperationCount(_recorder, 1, opLabels);

            // The above will add 1 to the metric "OperationCount" to the cell identified by the labels
            //   ClusterId = "123"
            //   PodId = "456"
            //   Operation = "DeleteObject"
        }
    }
}

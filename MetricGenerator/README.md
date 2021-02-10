# Metrics

Metrics are recorded at runtime and have 3 core components:

    * Name
    * Value
    * Labels

where labels are a set of name/value pairs where both name and value are strings.
When you want to record a metric, you need to supply all three value. The labels serve
to define a unique counter associated with the given metric. Different labels means 
a different counter.

The set of labels associated with a metric can come from different places in an executing
program:

    * Ambient. This are immutable values for the life of the process. Perhaps this includes the 
      pod id, the cluster id, the region id, etc.

    * Contextual. This depends on the operation being performed. For example, maybe some extra labels
      are created to represent the identity of a particular caller to the service.

    * Scoped. This depends on the specific operation being performed. If you're reading a resource on behalf of a user, the labels might include the resource id.

So as execution progresses, more label values become available. At some point, all the labels have been accumulated and its time to actually produce a metric
using all those labels to identity the particular counter to manipulate.

# The Model

What we are defining here is a model to make it easy to declare the known label names used in a program, along with the set of known metrics it
produces:

* You define label names as either absolute, or as extensions over an existing set of label names. This captures the nesting model where some label values are
known at certain places in the code, and more values become available as execution progresses.

* You define the metrics you program reports along with the set of labels that metric uses to identify its counters. In order to emit that metric, code will
be required to acquire all the stipulated metric values.

Once you've defined these few pieces of state, code generation kicks in and produces several artifacts. The primary artifact of concern to the 
developer is the metric recording function which takes a metric recorder (where metrics are pushed), along with a set of label names and label values.

The motivation for this approach is that:

* It provides a declarative model to define metrics used in a program.

* It provides strongly-typed methods for recording metrics, which deliver IntelliSense and prevent recording metrics 
which accidentally skip important labels.

* It is efficient, avoid work that may otherwise be needed and minimizing allocations. For example, it is necessary to
ensure label names are unique within a metric, which would normally requiring explicit sorting and dedupping of names
every time a metric is reported. In this model however, this can be handled at initialization time instead at runtime.
 
using Xunit;

// AwsListenerTests installs a capturing provider into the process-global Configuration.Default,
// because the listener under test builds its EventContext from the ambient Configuration.  Any
// test that disposes an EventContext constructed without an explicit Configuration would publish
// into whichever list is installed at that moment, so no other test may run alongside those.
// Scoping this to a collection wouldn't be enough: xunit parallelizes across collections.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

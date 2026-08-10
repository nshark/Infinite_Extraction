using Xunit;

// The logging system is process-global static state, so tests that configure it
// would race each other. Serialising the suite is cheaper than threading a logger
// context through every call site.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

// Integration tests share a process with WebApplicationFactory and Testcontainers.
// Running two WebApplicationFactory<Program> instances in parallel causes host-build
// interference (both call Program.cs simultaneously and can corrupt each other's config).
// Serializing execution removes the race without slowing real-world CI since Testcontainers
// still starts containers concurrently within each fixture.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

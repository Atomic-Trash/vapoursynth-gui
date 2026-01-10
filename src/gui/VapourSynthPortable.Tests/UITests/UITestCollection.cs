namespace VapourSynthPortable.Tests.UITests;

/// <summary>
/// Collection definition to ensure UI tests run sequentially (not in parallel).
/// UI tests launch the actual application and can't run concurrently.
/// </summary>
[CollectionDefinition("UI Tests", DisableParallelization = true)]
public class UITestCollection
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

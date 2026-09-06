namespace Library.Api;

/// <summary>
/// Marker type for the shared resource files (Resources/SharedResources.resx,
/// SharedResources.bn.resx, ...). Inject <c>IStringLocalizer&lt;SharedResources&gt;</c>
/// and look messages up by key. Add a new language by dropping in a
/// <c>SharedResources.&lt;culture&gt;.resx</c> - no code change.
/// </summary>
public sealed class SharedResources
{
}

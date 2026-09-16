namespace BulkUpload.Resolvers;

/// <summary>
/// Converts one CSV value into the value Umbraco stores for a property, selected by the
/// <c>propertyAlias|resolverAlias</c> column syntax.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Resolvers must be singleton-safe.</strong> <see cref="ResolverFactory"/> is a singleton that
/// resolves every <see cref="IResolver"/> once, from the root service provider, and caches the instances
/// for the lifetime of the application — so an instance registered as transient or scoped is still only
/// created once, and whatever it captures in its constructor lives forever.
/// </para>
/// <para>
/// In practice that means: do not hold a scoped service (an <c>IPublishedContentQuery</c>, a scoped
/// repository, anything tied to a request) in a field. Take the singleton-safe services instead —
/// <c>IUmbracoContextFactory</c>, <c>IPublishedContentCache</c>, <c>IContentService</c>,
/// <c>IMediaService</c> — and open what you need inside <see cref="Resolve"/>, for example with
/// <c>using var _ = contextFactory.EnsureUmbracoContext();</c>. <see cref="Resolve"/> must also be safe to
/// call from any thread.
/// </para>
/// </remarks>
public interface IResolver
{
    /// <summary>The alias used after the pipe in a CSV column header, e.g. <c>heroImage|urlToMedia</c>.</summary>
    string Alias();

    /// <summary>Converts the raw CSV value into the value to store on the property.</summary>
    object Resolve(object value);
}

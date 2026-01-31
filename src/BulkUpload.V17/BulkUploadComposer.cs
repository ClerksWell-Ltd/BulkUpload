using BulkUpload.Core.Resolvers;
using BulkUpload.Core.Services;

using Microsoft.Extensions.DependencyInjection;

using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace BulkUpload.V17;

internal class BulkUploadComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // NOTE: No ManifestFilter or Section registration needed for v17
        // Sections and dashboards are auto-discovered from umbraco-package.json

        // Register all resolvers (same as v13)
        builder.Services.AddSingleton<IResolver, TextResolver>();
        builder.Services.AddSingleton<IResolver, BooleanResolver>();
        builder.Services.AddSingleton<IResolver, ObjectToJsonResolver>();
        builder.Services.AddSingleton<IResolver, TextToLinkResolver>();
        builder.Services.AddTransient<IResolver, GuidToContentUdiResolver>();
        builder.Services.AddTransient<IResolver, GuidsToContentUdisResolver>();
        builder.Services.AddTransient<IResolver, ContentIdToContentUdiResolver>();
        builder.Services.AddTransient<IResolver, ContentIdsToContentUdisResolver>();
        builder.Services.AddTransient<IResolver, GuidToMediaUdiResolver>();
        builder.Services.AddTransient<IResolver, GuidsToMediaUdisResolver>();
        builder.Services.AddTransient<IResolver, MediaIdToMediaUdiResolver>();
        builder.Services.AddTransient<IResolver, MediaIdsToMediaUdisResolver>();
        builder.Services.AddTransient<IResolver, UrlToMediaResolver>();
        builder.Services.AddTransient<IResolver, PathToMediaResolver>();
        builder.Services.AddTransient<IResolver, UrlToStreamResolver>();
        builder.Services.AddTransient<IResolver, PathToStreamResolver>();
        builder.Services.AddSingleton<IResolver, DateTimeResolver>();
        builder.Services.AddSingleton<IResolver, SampleBlockListContentResolver>();
        builder.Services.AddSingleton<IResolver, MultiBlockListResolver>();
        builder.Services.AddSingleton<IResolver, StringArrayResolver>();
        builder.Services.AddTransient<IResolver, SampleAuthorNameResolver>();
        builder.Services.AddTransient<IResolver, SampleCategoryNamesResolver>();
        builder.Services.AddSingleton<IResolver, LegacyContentPickerResolver>();
        builder.Services.AddSingleton<IResolver, LegacyContentPickersResolver>();

        // Register core services (same as v13)
        builder.Services.AddSingleton<IResolverFactory, ResolverFactory>();
        builder.Services.AddSingleton<IParentLookupCache, ParentLookupCache>();
        builder.Services.AddSingleton<ILegacyIdCache, LegacyIdCache>();
        builder.Services.AddSingleton<IMediaItemCache, MediaItemCache>();
        builder.Services.AddSingleton<IHierarchyResolver, HierarchyResolver>();
        builder.Services.AddSingleton<IImportUtilityService, ImportUtilityService>();
        builder.Services.AddSingleton<IMediaImportService, MediaImportService>();
        builder.Services.AddSingleton<IMediaPreprocessorService, MediaPreprocessorService>();
    }
}

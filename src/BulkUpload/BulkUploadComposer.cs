using Microsoft.Extensions.DependencyInjection;

using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.BulkUpload.Resolvers;
using Umbraco.Community.BulkUpload.Services;

namespace Umbraco.Community.BulkUpload;

internal class BulkUploadComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.ManifestFilters().Append<BulkUploadManifestFilter>();

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
        builder.Services.AddSingleton<IResolver, DateTimeResolver>();
        builder.Services.AddSingleton<IResolver, SampleBlockListContentResolver>();
        builder.Services.AddSingleton<IResolver, StringArrayResolver>();

        builder.Services.AddSingleton<IResolverFactory, ResolverFactory>();
        builder.Services.AddSingleton<IImportUtilityService, ImportUtilityService>();
    }
}
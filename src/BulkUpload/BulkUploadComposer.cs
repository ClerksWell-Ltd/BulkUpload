using BulkUpload.Resolvers;
using BulkUpload.Services;

using Microsoft.Extensions.DependencyInjection;

using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

#if NET8_0
using BulkUpload.Sections;

using Umbraco.Cms.Core.Sections;
#else
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
#endif

namespace BulkUpload;

internal class BulkUploadComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
#if NET8_0
        // Umbraco 13: Register sections and dashboards via C# API
        builder.ManifestFilters().Append<BulkUploadManifestFilter>();
        builder.Sections().InsertAfter<TranslationSection, BulkUploadSection>();
#else
        // Umbraco 17: Register Swagger/OpenAPI documentation
        builder.Services.ConfigureOptions<ConfigureBulkUploadSwaggerGenOptions>();
#endif
        // Note: Umbraco 17 uses umbraco-package.json for section/dashboard registration

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
        builder.Services.AddTransient<IResolver, ZipFileToMediaResolver>();
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

#if NET10_0
/// <summary>
/// Configures Swagger/OpenAPI generation options for BulkUpload API
/// </summary>
internal class ConfigureBulkUploadSwaggerGenOptions : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions swaggerGenOptions)
    {
        swaggerGenOptions.SwaggerDoc(
            "bulk-upload",
            new OpenApiInfo
            {
                Title = "BulkUpload API",
                Version = "v1.0",
                Description = "API for bulk importing content and media from CSV/ZIP files into Umbraco CMS. " +
                              "Supports multi-CSV imports, media deduplication, legacy content migration, and update mode.",
                Contact = new OpenApiContact
                {
                    Name = "BulkUpload Package",
                    Url = new Uri("https://github.com/ClerksWell-Ltd/BulkUpload")
                },
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://github.com/ClerksWell-Ltd/BulkUpload/blob/main/LICENSE")
                }
            });

        // Include XML comments from the assembly
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            swaggerGenOptions.IncludeXmlComments(xmlPath);
        }

        // Optionally include XML comments from BulkUpload.Core if it generates them
        var coreXmlFile = "BulkUpload.Core.xml";
        var coreXmlPath = Path.Combine(AppContext.BaseDirectory, coreXmlFile);
        if (File.Exists(coreXmlPath))
        {
            swaggerGenOptions.IncludeXmlComments(coreXmlPath);
        }
    }
}
#endif
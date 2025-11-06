using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Services;
using Umbraco.Community.BulkUpload.Models;
using Umbraco.Community.BulkUpload.Resolvers;

namespace Umbraco.Community.BulkUpload.Services;

public class ImportUtilityService : IImportUtilityService
{
#pragma warning disable IDE1006 // Naming Styles
    protected readonly IContentService _contentService;
#pragma warning restore IDE1006 // Naming Styles
    private readonly IResolverFactory _resolverFactory;
    private readonly ILogger<ImportUtilityService> _logger;

    public ImportUtilityService(IContentService contentService, IResolverFactory resolverFactory, ILogger<ImportUtilityService> logger)
    {
        _contentService = contentService;
        _resolverFactory = resolverFactory;
        _logger = logger;
    }

    public virtual ImportObject CreateImportObject(dynamic? record)
    {
        if (record == null)
        {
            _logger.LogError("Bulk Upload: Record is null");
            throw new ArgumentNullException(nameof(record));
        }

        var dynamicProperties = (IDictionary<string, object>)record;

        var propertiesToCreate = new Dictionary<string, object>();

        var name = "";
        if (dynamicProperties.TryGetValue("name", out object nameValue))
        {
            name = nameValue.ToString();
        }

        int parentId = 0;
        if (dynamicProperties.TryGetValue("parentId", out object parentIdValue))
        {
            parentId = int.Parse(parentIdValue.ToString());
        }

        var docTypeAlias = "";
        if (dynamicProperties.TryGetValue("docTypeAlias", out object docTypeAliasValue))
        {
            docTypeAlias = docTypeAliasValue.ToString();
        }

        ImportObject importObject = new ImportObject() { ContentTypeAlais = docTypeAlias, Name = name, ParentId = parentId };

        foreach (var property in dynamicProperties)
        {
            var columnDetails = property.Key.Split('|');
            var columnName = columnDetails.First();
            string? aliasValue = null;
            if (columnDetails.Length > 1)
            {
                aliasValue = columnDetails.Last();
            }

            if (new string[] { "name", "parentId", "docTypeAlias" }.Contains(property.Key.Split('|')[0]))
                continue;
            var resolverAlias = aliasValue ?? "text";

            var resolver = _resolverFactory.GetByAlias(resolverAlias);
            object? propertyValue = null;
            propertyValue = resolver.Resolve(property.Value);

            propertiesToCreate.Add(columnName, propertyValue);
        }


        importObject.Properties = propertiesToCreate;
        return importObject;
    }

    public virtual void ImportSingleItem(ImportObject importObject, bool publish = false)
    {
        // Try to find an existing item under the same parent with the same name
        var existingItem = _contentService
            .GetPagedChildren(importObject.ParentId, 0, int.MaxValue, out _)
            .FirstOrDefault(x => string.Equals(x.Name, importObject.Name, StringComparison.InvariantCultureIgnoreCase));

        // Create or reuse existing item
        var contentItem = existingItem
            ?? _contentService.Create(importObject!.Name, importObject.ParentId, importObject.ContentTypeAlais);


        // Update properties (same for both new and existing)
        if (importObject.Properties != null && importObject.Properties.Any())
        {
            foreach (var property in importObject.Properties)
            {
                contentItem.SetValue(property.Key, property.Value);
            }
        }

        // Save or publish
        if (publish)
        {
            _contentService.SaveAndPublish(contentItem);
        }
        else
        {
            _contentService.Save(contentItem);
        }
    }
}
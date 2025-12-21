using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
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
    private readonly IParentLookupCache _parentLookupCache;
    private readonly ILogger<ImportUtilityService> _logger;

    public ImportUtilityService(
        IContentService contentService,
        IResolverFactory resolverFactory,
        IParentLookupCache parentLookupCache,
        ILogger<ImportUtilityService> logger)
    {
        _contentService = contentService;
        _resolverFactory = resolverFactory;
        _parentLookupCache = parentLookupCache;
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
        if (dynamicProperties.TryGetValue("name", out object? nameValue))
        {
            name = nameValue?.ToString() ?? "";
        }

        // Support both "parent" (new) and "parentId" (legacy) columns
        string? parent = null;
        if (dynamicProperties.TryGetValue("parent", out object? parentValue))
        {
            var parentStr = parentValue?.ToString();
            if (!string.IsNullOrWhiteSpace(parentStr))
            {
                parent = ValidateParentValue(parentStr);
            }
        }
        // Fallback to legacy "parentId" column for backward compatibility
        else if (dynamicProperties.TryGetValue("parentId", out object? parentIdValue))
        {
            var parentIdStr = parentIdValue?.ToString();
            if (!string.IsNullOrWhiteSpace(parentIdStr))
            {
                parent = ValidateParentValue(parentIdStr);
            }
        }

        var docTypeAlias = "";
        if (dynamicProperties.TryGetValue("docTypeAlias", out object? docTypeAliasValue))
        {
            docTypeAlias = docTypeAliasValue?.ToString() ?? "";
        }

        ImportObject importObject = new ImportObject() { ContentTypeAlais = docTypeAlias, Name = name, Parent = parent };

        foreach (var property in dynamicProperties)
        {
            var columnDetails = property.Key.Split('|');
            var columnName = columnDetails.First();
            string? aliasValue = null;
            if (columnDetails.Length > 1)
            {
                aliasValue = columnDetails.Last();
            }

            if (new string[] { "name", "parent", "parentId", "docTypeAlias" }.Contains(property.Key.Split('|')[0]))
                continue;
            var resolverAlias = aliasValue ?? "text";

            var resolver = _resolverFactory.GetByAlias(resolverAlias);
            object? propertyValue = null;
            if (resolver != null)
            {
                propertyValue = resolver.Resolve(property.Value);
            }

            propertiesToCreate.Add(columnName, propertyValue);
        }


        importObject.Properties = propertiesToCreate;
        return importObject;
    }

    public virtual void ImportSingleItem(ImportObject importObject, bool publish = false)
    {
        // Resolve parent specification to content ID
        var parentId = ResolveParentId(importObject.Parent);
        _logger.LogDebug("Resolved parent '{Parent}' to ID {ParentId}", importObject.Parent, parentId);

        // Try to find an existing item under the same parent with the same name
        var existingItem = _contentService
            .GetPagedChildren(parentId, 0, int.MaxValue, out _)
            .FirstOrDefault(x => string.Equals(x.Name, importObject.Name, StringComparison.InvariantCultureIgnoreCase));

        // Create or reuse existing item
        var contentItem = existingItem
            ?? _contentService.Create(importObject!.Name, parentId, importObject.ContentTypeAlais);


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

    /// <summary>
    /// Validates that the parent value is in a supported format.
    /// Returns the value if it's a valid integer, GUID, or path; otherwise returns null.
    /// </summary>
    private string? ValidateParentValue(string parent)
    {
        if (string.IsNullOrWhiteSpace(parent))
        {
            return null;
        }

        // Check if it's a valid integer
        if (int.TryParse(parent, out _))
        {
            return parent;
        }

        // Check if it's a valid GUID
        if (Guid.TryParse(parent, out _))
        {
            return parent;
        }

        // Check if it's a path (starts with /)
        if (parent.TrimStart().StartsWith("/"))
        {
            return parent;
        }

        // Invalid format
        return null;
    }

    /// <summary>
    /// Resolves the parent folder specification to a content ID.
    /// Supports integer ID, GUID, or path (resolves to existing folders).
    /// </summary>
    public int ResolveParentId(string? parent)
    {
        if (string.IsNullOrWhiteSpace(parent))
        {
            return Constants.System.Root;
        }

        // Try to parse as integer ID
        if (int.TryParse(parent, out var parentId))
        {
            return parentId;
        }

        // Try to parse as GUID and resolve to content ID using cache
        if (Guid.TryParse(parent, out var guid))
        {
            var contentId = _parentLookupCache.GetContentIdByGuid(guid);
            if (contentId.HasValue)
            {
                _logger.LogDebug("Resolved parent GUID {Guid} to content ID {Id}", guid, contentId.Value);
                return contentId.Value;
            }
            _logger.LogWarning("Parent GUID {Guid} not found, using root folder", guid);
            return Constants.System.Root;
        }

        // Treat as path - resolve to existing folder structure using cache
        var folderId = _parentLookupCache.GetOrCreateContentFolderByPath(parent);
        return folderId ?? Constants.System.Root;
    }
}
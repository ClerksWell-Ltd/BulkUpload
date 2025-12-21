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
        // Resolve parent specification to content parent (GUID or int)
        var parent = ResolveParent(importObject.Parent);
        _logger.LogDebug("Resolved parent '{Parent}' to {ParentType} {ParentValue}",
            importObject.Parent, parent.GetType().Name, parent);

        // Try to find an existing item under the same parent with the same name
        IEnumerable<IContent> children;
        if (parent is Guid parentGuid)
        {
            children = parentGuid == Guid.Empty
                ? _contentService.GetPagedChildren(Constants.System.Root, 0, int.MaxValue, out _)
                : _contentService.GetPagedChildren(parentGuid, 0, int.MaxValue, out _);
        }
        else if (parent is int parentId)
        {
            children = _contentService.GetPagedChildren(parentId, 0, int.MaxValue, out _);
        }
        else
        {
            throw new InvalidOperationException("Invalid parent type resolved");
        }

        var existingItem = children.FirstOrDefault(x =>
            string.Equals(x.Name, importObject.Name, StringComparison.InvariantCultureIgnoreCase));

        // Create or reuse existing item
        IContent contentItem;
        if (existingItem != null)
        {
            contentItem = existingItem;
        }
        else
        {
            // Use GUID-based or int-based Create depending on parent type
            if (parent is Guid guid)
            {
                contentItem = guid == Guid.Empty
                    ? _contentService.Create(importObject!.Name, Constants.System.Root, importObject.ContentTypeAlais)
                    : _contentService.Create(importObject!.Name, guid, importObject.ContentTypeAlais);
            }
            else if (parent is int id)
            {
                contentItem = _contentService.Create(importObject!.Name, id, importObject.ContentTypeAlais);
            }
            else
            {
                throw new InvalidOperationException("Invalid parent type for content creation");
            }
        }

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
    /// Resolves the parent folder specification to a content parent (GUID or integer ID for .NET 8).
    /// Supports integer ID (.NET 8 only), GUID, or path (resolves to existing folders).
    /// Uses caching for improved performance.
    /// Returns either Guid or int depending on input and framework version.
    /// </summary>
    public object ResolveParent(string? parent)
    {
        if (string.IsNullOrWhiteSpace(parent))
        {
            return Constants.System.Root;
        }

        // Try to parse as GUID - use directly without lookup for modern Umbraco compatibility
        if (Guid.TryParse(parent, out var guid))
        {
            _logger.LogDebug("Using parent GUID {Guid} directly", guid);
            return guid;
        }

        // Try to parse as integer ID - only use for .NET 8 compatibility
        if (int.TryParse(parent, out var parentId))
        {
#if NET8_0
            _logger.LogDebug("Using parent integer ID {Id} (.NET 8)", parentId);
            return parentId;
#else
            // For non-.NET 8, look up the GUID from the integer ID
            var content = _contentService.GetById(parentId);
            if (content != null)
            {
                _logger.LogDebug("Resolved parent integer ID {Id} to GUID {Guid}", parentId, content.Key);
                return content.Key;
            }
            _logger.LogWarning("Parent ID {Id} not found, using root folder", parentId);
            return Constants.System.Root;
#endif
        }

        // Treat as path - resolve to existing folder structure using cache (returns GUID)
        var folderGuid = _parentLookupCache.GetOrCreateContentFolderByPath(parent);
        if (folderGuid.HasValue)
        {
            _logger.LogDebug("Resolved parent path '{Path}' to GUID {Guid}", parent, folderGuid.Value);
            return folderGuid.Value;
        }

        _logger.LogWarning("Could not resolve parent path '{Path}', using root folder", parent);
        return Constants.System.Root;
    }
}
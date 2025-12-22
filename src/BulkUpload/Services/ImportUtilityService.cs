using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.BulkUpload.Models;
using Umbraco.Community.BulkUpload.Resolvers;
using BulkUpload.Constants;

namespace Umbraco.Community.BulkUpload.Services;

public class ImportUtilityService : IImportUtilityService
{
#pragma warning disable IDE1006 // Naming Styles
    protected readonly IContentService _contentService;
#pragma warning restore IDE1006 // Naming Styles
    private readonly IResolverFactory _resolverFactory;
    private readonly IParentLookupCache _parentLookupCache;
    private readonly ILegacyIdCache _legacyIdCache;
    private readonly ILogger<ImportUtilityService> _logger;

    public ImportUtilityService(
        IContentService contentService,
        IResolverFactory resolverFactory,
        IParentLookupCache parentLookupCache,
        ILegacyIdCache legacyIdCache,
        ILogger<ImportUtilityService> logger)
    {
        _contentService = contentService;
        _resolverFactory = resolverFactory;
        _parentLookupCache = parentLookupCache;
        _legacyIdCache = legacyIdCache;
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

        // Extract reserved columns for legacy hierarchy mapping
        string? legacyId = null;
        if (dynamicProperties.TryGetValue(ReservedColumns.BulkUploadLegacyId, out object? legacyIdValue))
        {
            legacyId = legacyIdValue?.ToString();
        }

        string? legacyParentId = null;
        if (dynamicProperties.TryGetValue(ReservedColumns.BulkUploadLegacyParentId, out object? legacyParentIdValue))
        {
            legacyParentId = legacyParentIdValue?.ToString();
        }

        ImportObject importObject = new ImportObject()
        {
            ContentTypeAlais = docTypeAlias,
            Name = name,
            Parent = parent,
            LegacyId = legacyId,
            LegacyParentId = legacyParentId
        };

        foreach (var property in dynamicProperties)
        {
            var columnDetails = property.Key.Split('|');
            var columnName = columnDetails.First();
            string? aliasValue = null;
            if (columnDetails.Length > 1)
            {
                aliasValue = columnDetails.Last();
            }

            // Skip standard columns and reserved columns
            var standardColumns = new string[] { "name", "parent", "parentId", "docTypeAlias" };
            if (standardColumns.Contains(property.Key.Split('|')[0]) || ReservedColumns.IsReserved(property.Key.Split('|')[0]))
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

    public virtual ContentImportResult ImportSingleItem(ImportObject importObject, bool publish = false)
    {
        try
        {
            // Resolve parent specification to content parent (GUID or int)
            object parent;

            // Check if using legacy hierarchy mapping
            if (!string.IsNullOrWhiteSpace(importObject.LegacyParentId))
            {
                // Resolve parent from legacy ID cache
                if (_legacyIdCache.TryGetGuid(importObject.LegacyParentId, out var legacyParentGuid))
                {
                    parent = legacyParentGuid;
                    _logger.LogDebug("Resolved legacy parent ID '{LegacyParentId}' to GUID {Guid}",
                        importObject.LegacyParentId, legacyParentGuid);
                }
                else
                {
                    return new ContentImportResult
                    {
                        ContentName = importObject.Name,
                        Success = false,
                        ErrorMessage = $"Legacy parent ID '{importObject.LegacyParentId}' not found in cache. The parent must be created before this item.",
                        BulkUploadLegacyId = importObject.LegacyId
                    };
                }
            }
            else
            {
                // Use standard parent resolution
                parent = ResolveParent(importObject.Parent);
                _logger.LogDebug("Resolved parent '{Parent}' to {ParentType} {ParentValue}",
                    importObject.Parent, parent.GetType().Name, parent);
            }

            // Try to find an existing item under the same parent with the same name
            // For querying, we need to use integer ID (GetPagedChildren doesn't support GUID in all versions)
            int queryParentId;
            if (parent is Guid parentGuid)
            {
                if (parentGuid == Guid.Empty)
                {
                    queryParentId = Constants.System.Root;
                }
                else
                {
                    var parentContent = _contentService.GetById(parentGuid);
                    if (parentContent == null)
                    {
                        return new ContentImportResult
                        {
                            ContentName = importObject.Name,
                            Success = false,
                            ErrorMessage = $"Parent with GUID {parentGuid} not found",
                            BulkUploadLegacyId = importObject.LegacyId
                        };
                    }
                    queryParentId = parentContent.Id;
                }
            }
            else if (parent is int parentId)
            {
                queryParentId = parentId;
            }
            else
            {
                return new ContentImportResult
                {
                    ContentName = importObject.Name,
                    Success = false,
                    ErrorMessage = "Invalid parent type resolved",
                    BulkUploadLegacyId = importObject.LegacyId
                };
            }

            var existingItem = _contentService
                .GetPagedChildren(queryParentId, 0, int.MaxValue, out _)
                .FirstOrDefault(x => string.Equals(x.Name, importObject.Name, StringComparison.InvariantCultureIgnoreCase));

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
                    return new ContentImportResult
                    {
                        ContentName = importObject.Name,
                        Success = false,
                        ErrorMessage = "Invalid parent type for content creation",
                        BulkUploadLegacyId = importObject.LegacyId
                    };
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

            // Cache the created content GUID for legacy hierarchy resolution
            if (!string.IsNullOrWhiteSpace(importObject.LegacyId))
            {
                var contentGuid = contentItem.Key;
                if (_legacyIdCache.TryAdd(importObject.LegacyId, contentGuid))
                {
                    _logger.LogDebug("Cached legacy ID '{LegacyId}' → Umbraco GUID {Guid}",
                        importObject.LegacyId, contentGuid);
                }
                else
                {
                    _logger.LogWarning("Failed to cache legacy ID '{LegacyId}' - may already exist in cache",
                        importObject.LegacyId);
                }
            }

            // Return success result
            return new ContentImportResult
            {
                ContentName = importObject.Name,
                Success = true,
                ContentId = contentItem.Id,
                ContentGuid = contentItem.Key,
                ContentUdi = Udi.Create(Constants.UdiEntityType.Document, contentItem.Key).ToString(),
                BulkUploadLegacyId = importObject.LegacyId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing content item '{Name}'", importObject.Name);
            return new ContentImportResult
            {
                ContentName = importObject.Name,
                Success = false,
                ErrorMessage = ex.Message,
                BulkUploadLegacyId = importObject.LegacyId
            };
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
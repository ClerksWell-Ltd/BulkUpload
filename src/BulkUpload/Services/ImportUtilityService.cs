using BulkUpload.Models;
using BulkUpload.Resolvers;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

using ReservedColumns = BulkUpload.Constants.ReservedColumns;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace BulkUpload.Services;

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

        // Extract bulkUploadShouldPublish flag
        // Column presence indicates UPDATE MODE (file-level)
        // Row value determines whether to update this specific row (row-level)
        bool bulkUploadShouldPublish = false;
        bool bulkUploadShouldPublishColumnExisted = false;
        var shouldPublishKey = dynamicProperties.Keys.FirstOrDefault(k =>
            k.Split('|')[0].Equals(ReservedColumns.BulkUploadShouldPublish, StringComparison.OrdinalIgnoreCase));
        if (shouldPublishKey != null)
        {
            bulkUploadShouldPublishColumnExisted = true;
            if (dynamicProperties.TryGetValue(shouldPublishKey, out object? shouldPublishValue))
            {
                var shouldPublishStr = shouldPublishValue?.ToString()?.Trim().ToLowerInvariant();
                bulkUploadShouldPublish = shouldPublishStr == "true" || shouldPublishStr == "yes" || shouldPublishStr == "1";
            }
        }

        // Extract bulkUploadContentGuid for updating existing content
        Guid? bulkUploadContentGuid = null;
        if (dynamicProperties.TryGetValue(ReservedColumns.BulkUploadContentGuid, out object? contentGuidValue))
        {
            var contentGuidStr = contentGuidValue?.ToString();
            if (!string.IsNullOrWhiteSpace(contentGuidStr) && Guid.TryParse(contentGuidStr, out var parsedContentGuid))
            {
                bulkUploadContentGuid = parsedContentGuid;
            }
        }

        // Extract bulkUploadParentGuid for moving content
        Guid? bulkUploadParentGuid = null;
        if (dynamicProperties.TryGetValue(ReservedColumns.BulkUploadParentGuid, out object? parentGuidValue))
        {
            var parentGuidStr = parentGuidValue?.ToString();
            if (!string.IsNullOrWhiteSpace(parentGuidStr) && Guid.TryParse(parentGuidStr, out var parsedParentGuid))
            {
                bulkUploadParentGuid = parsedParentGuid;
            }
        }

        // Extract bulkUploadShouldUpdate flag
        // Column presence indicates UPDATE MODE (file-level)
        // Row value determines whether to update this specific row (row-level)
        bool bulkUploadShouldUpdate = false;
        bool bulkUploadShouldUpdateColumnExisted = false;
        var shouldUpdateKey = dynamicProperties.Keys.FirstOrDefault(k =>
            k.Split('|')[0].Equals(ReservedColumns.BulkUploadShouldUpdate, StringComparison.OrdinalIgnoreCase));
        if (shouldUpdateKey != null)
        {
            bulkUploadShouldUpdateColumnExisted = true; // Column exists = UPDATE MODE
            if (dynamicProperties.TryGetValue(shouldUpdateKey, out object? shouldUpdateValue))
            {
                var shouldUpdateStr = shouldUpdateValue?.ToString()?.Trim().ToLowerInvariant();
                bulkUploadShouldUpdate = shouldUpdateStr == "true" || shouldUpdateStr == "yes" || shouldUpdateStr == "1";
            }
        }

        ImportObject importObject = new ImportObject()
        {
            ContentTypeAlais = docTypeAlias,
            Name = name,
            Parent = parent,
            LegacyId = legacyId,
            LegacyParentId = legacyParentId,
            BulkUploadContentGuid = bulkUploadContentGuid,
            BulkUploadParentGuid = bulkUploadParentGuid,
            BulkUploadShouldUpdate = bulkUploadShouldUpdate,
            BulkUploadShouldUpdateColumnExisted = bulkUploadShouldUpdateColumnExisted,
            BulkUploadShouldPublish = bulkUploadShouldPublish,
            BulkUploadShouldPublishColumnExisted = bulkUploadShouldPublishColumnExisted
        };

        var contentPickerDependencies = new List<string>();
        var deferredProperties = new Dictionary<string, (object value, string resolverAlias)>();

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

            if (resolver != null)
            {
                // Check if this is a deferred resolver (e.g., content picker by legacy ID)
                if (resolver is IDeferredResolver deferredResolver)
                {
                    // Extract dependencies for topological sorting
                    var dependencies = deferredResolver.ExtractDependencies(property.Value);
                    contentPickerDependencies.AddRange(dependencies);

                    // Store the raw value and resolver alias for later resolution
                    deferredProperties[columnName] = (property.Value, resolverAlias);
                }
                else
                {
                    // Standard resolver - resolve immediately
                    object? propertyValue = resolver.Resolve(property.Value);

                    if (propertyValue != null)
                    {
                        propertiesToCreate.Add(columnName, propertyValue);
                    }
                }
            }
        }

        importObject.Properties = propertiesToCreate;

        // Store content picker dependencies for topological sorting
        if (contentPickerDependencies.Count > 0)
        {
            importObject.ContentPickerDependencies = contentPickerDependencies;
        }

        // Store deferred properties for resolution after dependencies are created
        if (deferredProperties.Count > 0)
        {
            importObject.DeferredProperties = deferredProperties;
        }

        return importObject;
    }

    public virtual ContentImportResult ImportSingleItem(ImportObject importObject, bool publish = false)
    {
        try
        {
            IContent? contentItem;
            var parentContentGuid = importObject.BulkUploadParentGuid;

            // Check if this is an update operation (bulkUploadContentGuid is present)
            if (importObject.BulkUploadContentGuid.HasValue)
            {
                // Update mode: Get existing content by GUID
                contentItem = _contentService.GetById(importObject.BulkUploadContentGuid.Value);
                if (contentItem == null)
                {
                    return new ContentImportResult
                    {
                        BulkUploadSuccess = false,
                        BulkUploadContentGuid = importObject.BulkUploadContentGuid,
                        BulkUploadParentGuid = importObject.BulkUploadParentGuid,
                        BulkUploadErrorMessage = $"Content with GUID {importObject.BulkUploadContentGuid.Value} not found",
                        BulkUploadLegacyId = importObject.LegacyId,
                        BulkUploadShouldUpdate = importObject.BulkUploadShouldUpdate,
                        BulkUploadShouldUpdateColumnExisted = importObject.BulkUploadShouldUpdateColumnExisted,
                        BulkUploadShouldPublish = importObject.BulkUploadShouldPublish,
                        BulkUploadShouldPublishColumnExisted = importObject.BulkUploadShouldPublishColumnExisted,
                        OriginalCsvData = importObject.OriginalCsvData,
                        SourceCsvFileName = importObject.SourceCsvFileName
                    };
                }

                _logger.LogDebug("Updating existing content with GUID {Guid}", importObject.BulkUploadContentGuid.Value);

                // Update name if different
                if (!string.IsNullOrWhiteSpace(importObject.Name) && contentItem.Name != importObject.Name)
                {
                    contentItem.Name = importObject.Name;
                }

                // Move to new parent if bulkUploadParentGuid is specified
                if (importObject.BulkUploadParentGuid.HasValue)
                {
                    var newParentGuid = importObject.BulkUploadParentGuid.Value;
                    int newParentId;

                    if (newParentGuid == Guid.Empty)
                    {
                        newParentId = UmbracoConstants.System.Root;
                    }
                    else
                    {
                        var newParentContent = _contentService.GetById(newParentGuid);
                        if (newParentContent == null)
                        {
                            return new ContentImportResult
                            {
                                BulkUploadSuccess = false,
                                BulkUploadErrorMessage = $"Parent with GUID {newParentGuid} not found",
                                BulkUploadParentGuid = importObject.BulkUploadParentGuid,
                                BulkUploadLegacyId = importObject.LegacyId,
                                BulkUploadShouldUpdate = importObject.BulkUploadShouldUpdate,
                                BulkUploadShouldUpdateColumnExisted = importObject.BulkUploadShouldUpdateColumnExisted,
                                BulkUploadShouldPublish = importObject.BulkUploadShouldPublish,
                                BulkUploadShouldPublishColumnExisted = importObject.BulkUploadShouldPublishColumnExisted,
                                OriginalCsvData = importObject.OriginalCsvData,
                                SourceCsvFileName = importObject.SourceCsvFileName
                            };
                        }
                        newParentId = newParentContent.Id;
                        parentContentGuid = newParentContent.Key;
                    }

                    // Move content to new parent if different
                    if (contentItem.ParentId != newParentId)
                    {
                        _contentService.Move(contentItem, newParentId);
                        _logger.LogDebug("Moved content '{Name}' to new parent {ParentId}", importObject.Name, newParentId);
                    }
                }
            }
            else
            {
                // Create mode: Use existing logic
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
                        parentContentGuid = legacyParentGuid;
                    }
                    else
                    {
                        return new ContentImportResult
                        {
                            BulkUploadSuccess = false,
                            BulkUploadParentGuid = importObject.BulkUploadParentGuid,
                            BulkUploadErrorMessage = $"Legacy parent ID '{importObject.LegacyParentId}' not found in cache. The parent must be created before this item.",
                            BulkUploadLegacyId = importObject.LegacyId,
                            BulkUploadShouldUpdate = importObject.BulkUploadShouldUpdate,
                            BulkUploadShouldUpdateColumnExisted = importObject.BulkUploadShouldUpdateColumnExisted,
                            BulkUploadShouldPublish = importObject.BulkUploadShouldPublish,
                            BulkUploadShouldPublishColumnExisted = importObject.BulkUploadShouldPublishColumnExisted,
                            OriginalCsvData = importObject.OriginalCsvData,
                            SourceCsvFileName = importObject.SourceCsvFileName
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


                // Use GUID-based or int-based Create depending on parent type
                if (parent is Guid guid)
                {
                    contentItem = guid == Guid.Empty
                        ? _contentService.Create(importObject!.Name, UmbracoConstants.System.Root, importObject.ContentTypeAlais)
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
                        BulkUploadSuccess = false,
                        BulkUploadParentGuid = importObject.BulkUploadParentGuid,
                        BulkUploadErrorMessage = "Invalid parent type for content creation",
                        BulkUploadLegacyId = importObject.LegacyId,
                        BulkUploadShouldUpdate = importObject.BulkUploadShouldUpdate,
                        BulkUploadShouldUpdateColumnExisted = importObject.BulkUploadShouldUpdateColumnExisted,
                        BulkUploadShouldPublish = importObject.BulkUploadShouldPublish,
                        BulkUploadShouldPublishColumnExisted = importObject.BulkUploadShouldPublishColumnExisted,
                        OriginalCsvData = importObject.OriginalCsvData,
                        SourceCsvFileName = importObject.SourceCsvFileName
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

            // Resolve and set deferred properties (e.g., content pickers by legacy ID)
            if (importObject.DeferredProperties != null && importObject.DeferredProperties.Any())
            {
                foreach (var deferredProperty in importObject.DeferredProperties)
                {
                    var propertyAlias = deferredProperty.Key;
                    var (rawValue, resolverAlias) = deferredProperty.Value;

                    // Get the deferred resolver
                    var resolver = _resolverFactory.GetByAlias(resolverAlias);

                    if (resolver is IDeferredResolver deferredResolver)
                    {
                        // Resolve using the LegacyIdCache
                        var resolvedValue = deferredResolver.ResolveDeferred(rawValue, _legacyIdCache);

                        if (resolvedValue != null)
                        {
                            contentItem.SetValue(propertyAlias, resolvedValue);
                        }
                    }
                }
            }

            // Save or publish
            if (publish)
            {
#if NET8_0
                _contentService.SaveAndPublish(contentItem);
#else
                    // Umbraco 17: Save and Publish are separate operations
                    _contentService.Save(contentItem);
                    _contentService.Publish(contentItem, Array.Empty<string>());
#endif
            }
            else
            {
                var published = contentItem.Published;
                _contentService.Save(contentItem);
                if (published)
                {
                    _contentService.Unpublish(contentItem);
                }
            }

            // Cache the created content GUID for legacy hierarchy resolution
            if (!string.IsNullOrWhiteSpace(importObject.LegacyId))
            {
                var bulkUploadContentGuid = contentItem.Key;
                if (_legacyIdCache.TryAdd(importObject.LegacyId, bulkUploadContentGuid))
                {
                    _logger.LogDebug("Cached legacy ID '{LegacyId}' → Umbraco GUID {Guid}",
                        importObject.LegacyId, bulkUploadContentGuid);
                }
                else
                {
                    _logger.LogWarning("Failed to cache legacy ID '{LegacyId}' - may already exist in cache",
                        importObject.LegacyId);
                }
            }

            // Get parent GUID if content has a parent
            Guid? bulkUploadParentGuid = null;
            if (contentItem.ParentId != UmbracoConstants.System.Root)
            {
                var parentContent = _contentService.GetById(contentItem.ParentId);
                bulkUploadParentGuid = parentContent?.Key;
            }

            // Return success result
            return new ContentImportResult
            {
                BulkUploadSuccess = true,
                BulkUploadContentGuid = contentItem.Key,
                BulkUploadParentGuid = bulkUploadParentGuid,
                BulkUploadLegacyId = importObject.LegacyId,
                BulkUploadShouldUpdate = importObject.BulkUploadShouldUpdate,
                BulkUploadShouldUpdateColumnExisted = importObject.BulkUploadShouldUpdateColumnExisted,
                BulkUploadShouldPublish = importObject.BulkUploadShouldPublish,
                BulkUploadShouldPublishColumnExisted = importObject.BulkUploadShouldPublishColumnExisted,
                OriginalCsvData = importObject.OriginalCsvData,
                SourceCsvFileName = importObject.SourceCsvFileName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing content item '{Name}'", importObject.Name);
            return new ContentImportResult
            {
                BulkUploadSuccess = false,
                BulkUploadErrorMessage = ex.Message,
                BulkUploadLegacyId = importObject.LegacyId,
                BulkUploadShouldUpdate = importObject.BulkUploadShouldUpdate,
                BulkUploadShouldUpdateColumnExisted = importObject.BulkUploadShouldUpdateColumnExisted,
                BulkUploadShouldPublish = importObject.BulkUploadShouldPublish,
                BulkUploadShouldPublishColumnExisted = importObject.BulkUploadShouldPublishColumnExisted,
                OriginalCsvData = importObject.OriginalCsvData,
                SourceCsvFileName = importObject.SourceCsvFileName
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
            return UmbracoConstants.System.Root;
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
            return UmbracoConstants.System.Root;
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
        return UmbracoConstants.System.Root;
    }
}
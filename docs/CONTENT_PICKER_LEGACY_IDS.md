# Content Picker with Legacy IDs

## Overview

The BulkUpload package now supports content picker and multi-node tree picker properties that reference other content items by their legacy IDs. This feature automatically handles dependency tracking and ensures that referenced content is created before the content that references it.

## Features

- **Automatic Dependency Resolution**: Content items are automatically sorted to ensure referenced items are created first
- **Two Resolver Types**:
  - `legacyContentPicker` - For single content picker properties
  - `legacyContentPickers` - For multi-node tree picker properties (comma-separated legacy IDs)
- **Cross-File Support**: Dependencies can span multiple CSV files in the same import batch
- **Flexible References**: Can reference content that already exists in Umbraco or content being created in the same import

## How It Works

### 1. Legacy ID Tracking

When you import content with the `bulkUploadLegacyId` column, each created content item's GUID is cached and mapped to its legacy ID. This cache is then used to resolve content picker references.

### 2. Dependency Detection

When a property uses the `legacyContentPicker` or `legacyContentPickers` resolver, the system:
1. Extracts the legacy ID(s) from the CSV value
2. Adds them to the item's dependency list
3. Ensures those items are created before this item during topological sorting

### 3. Deferred Resolution

Unlike standard resolvers that execute immediately, content picker resolvers are "deferred":
1. During CSV parsing, only dependencies are extracted (resolution is skipped)
2. During import, after the content item is created, the resolver looks up the legacy ID in the cache
3. The GUID is converted to a content UDI and set on the property

## Usage

### Basic Example: Single Content Picker

```csv
name,docTypeAlias,bulkUploadLegacyId,author|legacyContentPicker
John Doe,author,author-john,
Article 1,article,article-1,author-john
```

In this example:
- The `author` property on Article 1 will be resolved to reference the "John Doe" content item
- The system ensures "John Doe" is created before "Article 1"

### Advanced Example: Multi-Node Tree Picker

```csv
name,docTypeAlias,bulkUploadLegacyId,categories|legacyContentPickers
Technology,category,cat-tech,
Business,category,cat-business,
Article 1,article,article-1,"cat-tech,cat-business"
```

In this example:
- The `categories` property on Article 1 will reference both categories
- Comma-separated legacy IDs are supported for multi-selection pickers
- Both categories will be created before Article 1

### Complete Example

See `samples/content-upload-with-content-pickers.csv` for a comprehensive example that demonstrates:
- Parent-child relationships using `bulkUploadLegacyParentId`
- Single content pickers for author references
- Multi-node tree pickers for category references
- Combined dependency tracking

## Column Format

Use the pipe syntax to specify the resolver:

```
propertyAlias|resolverAlias
```

Examples:
- `author|legacyContentPicker` - Single content picker
- `categories|legacyContentPickers` - Multi-node tree picker

## Dependency Rules

### Within Same Import Batch

If the referenced content has a legacy ID and is in the same import batch:
- The dependency is tracked
- Content is sorted to ensure correct creation order
- Legacy ID is resolved to the newly created content GUID

### Already Exists in Umbraco

If the referenced content is not in the import batch:
- The dependency is ignored (not an error)
- Resolution is attempted during import
- If the legacy ID is not found in the cache, the property is set to empty

### Circular Dependencies

Circular dependencies are detected and reported with a helpful error message:
```
Circular reference detected in legacy hierarchy: item-a → item-b → item-c → item-a
```

## Advanced Scenarios

### Combined Parent and Content Picker Dependencies

You can have both parent-child relationships AND content picker dependencies:

```csv
name,docTypeAlias,bulkUploadLegacyId,bulkUploadLegacyParentId,author|legacyContentPicker
Authors,authorsContainer,authors,,
Blog,blogContainer,blog,,
John Doe,author,author-john,authors,
Article 1,article,article-1,blog,author-john
```

In this case:
1. "Authors" container is created first (no dependencies)
2. "Blog" container is created second (no dependencies)
3. "John Doe" is created third (depends on "Authors" parent)
4. "Article 1" is created last (depends on both "Blog" parent AND "John Doe" author)

### Multiple CSV Files

Dependencies work across multiple CSV files in the same import:

**authors.csv**:
```csv
name,docTypeAlias,bulkUploadLegacyId
John Doe,author,author-john
```

**articles.csv**:
```csv
name,docTypeAlias,bulkUploadLegacyId,author|legacyContentPicker
Article 1,article,article-1,author-john
```

When both CSVs are uploaded together (in a ZIP file), the dependency is correctly tracked across files.

## Resolver Reference

### legacyContentPicker

**Alias**: `legacyContentPicker`

**Purpose**: Resolves a single legacy ID to a content UDI for content picker properties

**Input**: String containing a legacy ID

**Output**: Content UDI string (e.g., `umb://document/a1b2c3d4e5f6...`)

**Example**:
```csv
author|legacyContentPicker
author-john
```

### legacyContentPickers

**Alias**: `legacyContentPickers`

**Purpose**: Resolves comma-separated legacy IDs to comma-separated content UDIs for multi-node tree picker properties

**Input**: Comma-separated string of legacy IDs

**Output**: Comma-separated content UDI strings

**Example**:
```csv
categories|legacyContentPickers
"cat-tech,cat-business,cat-finance"
```

**Note**: Use quotes around comma-separated values to ensure proper CSV parsing.

## Error Handling

### Invalid Legacy ID Reference

If a legacy ID referenced in a content picker is not found:
- The property is set to an empty string
- No error is thrown (graceful degradation)
- This allows for references to content that may already exist in Umbraco

### Missing Required Dependencies

If a parent legacy ID is missing (via `bulkUploadLegacyParentId`):
- An error is returned for that specific item
- The import continues for other items
- Error message clearly states which legacy ID is missing

### Duplicate Legacy IDs

If the same legacy ID appears multiple times:
- An error is thrown before import begins
- The error message lists all items with the duplicate ID
- No content is created until duplicates are resolved

## Best Practices

1. **Unique Legacy IDs**: Ensure each legacy ID is unique across your entire import
2. **Consistent Naming**: Use a consistent naming scheme for legacy IDs (e.g., `author-{id}`, `category-{id}`)
3. **Test Small Batches**: Test with a small subset of data first to verify dependencies are correct
4. **Use Meaningful IDs**: Make legacy IDs human-readable for easier debugging
5. **Document Dependencies**: Keep track of which content types reference each other

## Troubleshooting

### Content Picker Shows Empty

**Problem**: A content picker property is empty after import

**Possible Causes**:
1. Referenced legacy ID doesn't exist in the import batch
2. Referenced content failed to import
3. Referenced content was created after the referencing content (dependency tracking failed)

**Solution**:
- Verify the legacy ID exists in your CSV
- Check the import results for errors on the referenced content
- Ensure both items have `bulkUploadLegacyId` values

### Topological Sort Error

**Problem**: "Circular reference detected in legacy hierarchy"

**Cause**: Two or more items reference each other in a loop

**Solution**:
- Review your dependencies to find the circular reference
- Restructure your content to break the cycle
- The error message shows the cycle path to help identify the problem

## Technical Details

### Implementation Architecture

The content picker feature uses a "deferred resolver" pattern:

1. **IDeferredResolver Interface**: Special resolver interface with two phases:
   - `ExtractDependencies()`: Called during CSV parsing to build dependency graph
   - `ResolveDeferred()`: Called during import with access to the LegacyIdCache

2. **ImportObject Extensions**: Two new properties track deferred data:
   - `ContentPickerDependencies`: List of legacy IDs this item depends on
   - `DeferredProperties`: Raw CSV values and resolver aliases for later resolution

3. **Enhanced Topological Sort**: The HierarchyResolver now considers both:
   - Parent-child dependencies (via `bulkUploadLegacyParentId`)
   - Content picker dependencies (via `ContentPickerDependencies`)

4. **Two-Phase Resolution**:
   - Phase 1: Parse CSV, extract dependencies, build graph, topologically sort
   - Phase 2: Import in sorted order, resolve deferred properties using cache

### Performance Considerations

- **Cache Efficiency**: The LegacyIdCache uses `ConcurrentDictionary` for thread-safe, O(1) lookups
- **Minimal Overhead**: Deferred resolution only occurs for properties using legacy content pickers
- **Batch Processing**: All dependency analysis happens once before any content is created

## Related Features

- [Legacy Hierarchy Import](./LEGACY_HIERARCHY_IMPORT.md) - Parent-child relationships using legacy IDs
- [Resolver System](./RESOLVERS.md) - Complete guide to all property resolvers
- [Multi-File Import](./MULTI_FILE_IMPORT.md) - Importing from multiple CSV files and ZIP archives

## Examples

For working examples, see:
- `samples/content-upload-with-content-pickers.csv` - Complete example with authors and categories
- `samples/legacy-hierarchy-import-sample.csv` - Parent-child hierarchy example

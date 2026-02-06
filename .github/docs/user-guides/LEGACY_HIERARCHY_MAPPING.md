# Legacy Hierarchy Mapping

## Overview

The BulkUpload extension supports legacy hierarchy mapping during bulk imports. This feature allows you to preserve hierarchical relationships from legacy CMS systems when migrating content to Umbraco.

**Multi-CSV Support:** The hierarchy mapping works seamlessly across multiple CSV files in a single import, allowing parent-child relationships to span different files while maintaining correct dependency order.

## Features

### Reserved CSV Columns

Two new optional, reserved columns are available:

- **`bulkUploadLegacyId`**: The unique identifier from your legacy CMS for the current item
- **`bulkUploadLegacyParentId`**: The legacy identifier of the parent item

These columns are used exclusively for import logic and are **never persisted** as Umbraco content properties.

### Key Capabilities

1. **Optional Feature**: Completely opt-in. If these columns aren't present, imports work exactly as before.
2. **Automatic Hierarchy Resolution**: The system automatically determines the correct creation order based on parent-child relationships.
3. **Deep Hierarchy Support**: Handles arbitrarily nested hierarchies (great-great-grandchildren work perfectly).
4. **GUID-Based Parent Resolution**: After each item is created, its Umbraco GUID is cached and used for child items.
5. **Comprehensive Validation**:
   - Detects and reports duplicate legacy IDs
   - Identifies circular references
   - Validates that all parent references exist
   - Provides clear, actionable error messages

## How It Works

### 1. CSV Structure

Your CSV file can include the reserved columns alongside standard columns:

```csv
name,docTypeAlias,parent,bulkUploadLegacyId,bulkUploadLegacyParentId,title|text
Blog,contentFolder,/,,blog-root,Blog Root
Article 1,article,/,art-001,blog-root,First Article
Article 2,article,/,art-002,blog-root,Second Article
Sub Article,article,/,art-003,art-001,Nested Article
```

### 2. Import Process

The import process works in three phases:

**Phase 1: Gather All CSV Records**
- **All CSV files** in the ZIP are read together
- Records from all CSVs are parsed into `ImportObject` instances
- Each record is tagged with its source CSV filename for results tracking
- Legacy ID and parent ID are extracted and stored separately
- Reserved columns are excluded from property mapping

**Phase 2: Validate and Sort Across All CSVs**
- The `HierarchyResolver` validates the hierarchy **globally across all CSVs**:
  - Checks for duplicate legacy IDs (across all files)
  - Verifies all parent references exist (across all files)
  - Detects circular references (across all files)
- Items are sorted using **topological sort** (Kahn's algorithm) to ensure parents are created first
- **Critical:** Sorting happens across all items from all CSVs, not per-file

**Phase 3: Import in Dependency Order**
- Items are created in the sorted order (parents before children)
- After each creation, the Umbraco GUID is cached with its legacy ID
- Child items resolve their parent's GUID from the cache
- Works seamlessly even when parent is from a different CSV file

### 3. Parent Resolution

When an item has a `bulkUploadLegacyParentId`:
1. The system looks up the legacy parent ID in the cache
2. Retrieves the corresponding Umbraco content GUID
3. Uses that GUID to create the child under the correct parent

If a legacy parent ID cannot be resolved, a clear error is thrown explaining the issue.

## Example Usage

### Simple Hierarchy

```csv
name,docTypeAlias,parent,bulkUploadLegacyId,bulkUploadLegacyParentId,title|text
Products,contentFolder,/,,prod-folder,Product Catalog
Category A,category,/,cat-a,prod-folder,Category A
Product 1,product,/,prod-001,cat-a,Widget A
Product 2,product,/,prod-002,cat-a,Widget B
```

**Result**: Creates "Products" folder, then "Category A" under it, then both products under the category.

### Mixed Legacy and Standard Imports

```csv
name,docTypeAlias,parent,bulkUploadLegacyId,bulkUploadLegacyParentId,title|text
News,contentFolder,/,,,News Section
Article Old,article,/,legacy-123,legacy-root,Migrated Article
Article New,article,/News,,,New Article
```

**Result**: Items without legacy IDs (News, Article New) are created first using standard parent resolution. Items with legacy IDs are sorted and created afterward.

### Multi-CSV Hierarchy Example

**File 1: `categories.csv`**
```csv
name,docTypeAlias,parent,bulkUploadLegacyId,bulkUploadLegacyParentId,title|text
Blog,contentFolder,/,,blog-root,Blog Section
Products,contentFolder,/,,prod-root,Products Section
Electronics,category,/,cat-electronics,prod-root,Electronics Category
Computers,category,/,cat-computers,cat-electronics,Computer Category
```

**File 2: `articles.csv`**
```csv
name,docTypeAlias,parent,bulkUploadLegacyId,bulkUploadLegacyParentId,title|text
Article 1,article,/,art-001,blog-root,First Blog Post
Article 2,article,/,art-002,blog-root,Second Blog Post
```

**File 3: `products.csv`**
```csv
name,docTypeAlias,parent,bulkUploadLegacyId,bulkUploadLegacyParentId,title|text
Laptop A,product,/,prod-001,cat-computers,Gaming Laptop
Laptop B,product,/,prod-002,cat-computers,Business Laptop
Phone X,product,/,prod-003,cat-electronics,Smartphone X
```

**Result with Global Topological Sort:**

1. **First Pass - No Dependencies (Root Items):**
   - Blog (from `categories.csv`)
   - Products (from `categories.csv`)

2. **Second Pass - Dependent on Roots:**
   - Electronics (parent: `prod-root` from same file)
   - Article 1 (parent: `blog-root` from different file ✓)
   - Article 2 (parent: `blog-root` from different file ✓)

3. **Third Pass - Deeper Dependencies:**
   - Computers (parent: `cat-electronics` from same file)

4. **Fourth Pass - Deepest Dependencies:**
   - Laptop A (parent: `cat-computers` from different file ✓)
   - Laptop B (parent: `cat-computers` from different file ✓)
   - Phone X (parent: `cat-electronics` from different file ✓)

**Key Points:**
- Parent-child relationships work **across CSV files**
- All items are sorted together in a **single global hierarchy**
- Cache persists across all CSVs within the import
- Legacy IDs must be unique across **all CSV files**, not just within one file

## Validation Errors

The system provides clear error messages for common issues:

### Duplicate Legacy ID
```
Duplicate legacy ID found: 'prod-001' appears in multiple items: 'Product A', 'Product B'.
Each legacy ID must be unique.
```

### Circular Reference
```
Circular reference detected in legacy hierarchy: cat-a → cat-b → cat-a.
Please check your legacy parent ID references.
```

### Missing Parent Reference
```
Legacy parent ID 'cat-z' referenced by item 'Product X' (legacy ID: 'prod-001')
was not found in the import data. All legacy parent IDs must reference items
within the same import.
```

### Parent Not Yet Created
```
Legacy parent ID 'cat-a' not found in cache for item 'Product X' (legacy ID: 'prod-001').
The parent must be created before this item.
```

## Architecture

### Components

1. **`ReservedColumns`** (`BulkUpload/Constants/ReservedColumns.cs`)
   - Defines reserved column names
   - Provides helper methods for checking if a column is reserved

2. **`LegacyIdCache`** (`BulkUpload/Services/LegacyIdCache.cs`)
   - Thread-safe in-memory cache
   - Maps legacy IDs to Umbraco GUIDs
   - Similar architecture to existing `ParentLookupCache`

3. **`HierarchyResolver`** (`BulkUpload/Services/HierarchyResolver.cs`)
   - Validates legacy hierarchy
   - Performs topological sort using Kahn's algorithm
   - Detects circular references using depth-first search

4. **Enhanced `ImportObject`** (`BulkUpload/Models/ImportObject.cs`)
   - Added `LegacyId` and `LegacyParentId` properties
   - These properties are NOT persisted to Umbraco

5. **Updated `ImportUtilityService`**
   - Extracts reserved columns in `CreateImportObject()`
   - Filters reserved columns from property mapping
   - Resolves parents via legacy cache in `ImportSingleItem()`
   - Caches created GUIDs for child items

6. **Updated `BulkUploadController`**
   - Validates and sorts import objects before processing
   - Processes items in dependency order

## Extensibility

The architecture supports future reserved columns:

1. Add new column name to `ReservedColumns` class
2. Extract value in `ImportUtilityService.CreateImportObject()`
3. Add processing logic in `ImportUtilityService.ImportSingleItem()`
4. No changes needed to core import logic!

## Backward Compatibility

✅ **No Breaking Changes**
- Existing CSV imports without legacy columns work exactly as before
- Feature is completely opt-in
- No performance impact when legacy columns are not present

## Best Practices

1. **Use Consistent Legacy IDs**: Keep legacy IDs unique and meaningful across **all CSV files**
2. **Plan Your Hierarchy**: Parent items can be in different CSV files - the system handles cross-file dependencies
3. **Organize by Type**: Consider separating categories, articles, and products into different CSVs for better organization
4. **Ensure Global Uniqueness**: Legacy IDs must be unique across **all CSV files in the import**, not just within a single file
5. **Test with Small Samples**: Validate your CSV structure before large imports
6. **Review Logs**: Check debug logs for hierarchy resolution details
7. **Handle Root Items**: Items with no legacy parent should still have a standard `parent` value (e.g., `/` or a GUID)
8. **Track Source Files**: Use meaningful CSV filenames - they appear in result exports for tracking

## Technical Details

### Topological Sort Algorithm

The system uses **Kahn's Algorithm** for topological sorting:
1. Build a directed graph of parent → child relationships
2. Calculate in-degree (number of dependencies) for each node
3. Start with nodes that have zero dependencies (roots)
4. Process each node, removing edges to children
5. Add children to queue when their in-degree reaches zero
6. If any nodes remain unprocessed, a cycle exists

### GUID Caching

After successful content creation:
```csharp
if (!string.IsNullOrWhiteSpace(importObject.LegacyId))
{
    var contentGuid = contentItem.Key;
    _legacyIdCache.TryAdd(importObject.LegacyId, contentGuid);
}
```

When resolving parent:
```csharp
if (!string.IsNullOrWhiteSpace(importObject.LegacyParentId))
{
    if (_legacyIdCache.TryGetGuid(importObject.LegacyParentId, out var parentGuid))
    {
        parent = parentGuid;
    }
}
```

## Troubleshooting

### Import Fails with Validation Error

Check the error message for specific details:
- **Duplicate IDs**: Search your CSV for duplicate `bulkUploadLegacyId` values
- **Circular References**: Review parent-child relationships for cycles
- **Missing Parents**: Ensure all referenced legacy parent IDs exist in the CSV

### Parent Not Found in Cache

This usually means:
1. The items are being processed out of order (shouldn't happen with topological sort)
2. The parent item failed to create (check logs for earlier errors)
3. The parent item doesn't have a legacy ID but is referenced by one

### Items Created in Wrong Order

Check that:
1. `bulkUploadLegacyParentId` values correctly reference `bulkUploadLegacyId` values
2. There are no typos in legacy ID references (comparison is case-insensitive)
3. All parent items are included in the CSV

## Performance Considerations

- **In-Memory Cache**: The legacy ID cache is stored in memory and cleared after each import
- **Thread Safety**: All caches use `ConcurrentDictionary` for thread-safe operations
- **Complexity**: Topological sort is O(V + E) where V = total items from all CSVs, E = total relationships
- **No Database Overhead**: Legacy IDs are never persisted to the database
- **Multi-CSV Efficiency**: All CSVs are processed in a single pass - no per-file overhead
- **Global Validation**: Validation happens once for all items, not per CSV file

## Future Enhancements

Potential future improvements:
- Support for legacy ID persistence (optional content property)
- Cross-import cache persistence for multi-file imports
- Visual hierarchy preview before import
- Legacy ID to Umbraco GUID export/mapping file generation

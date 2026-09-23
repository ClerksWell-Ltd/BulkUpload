# Update Mode Guide

This guide covers how to use BulkUpload to update existing content and media items in bulk.

## Overview

Update mode allows you to modify existing content nodes and media items using CSV files. Instead of creating new items, you can target specific items by their GUID and update only the properties you specify.

## When to Use Update Mode

Update mode is ideal for:
- **Bulk content updates** - Update titles, descriptions, or properties across multiple pages
- **Media metadata updates** - Add or update alt text, tags, or other media properties
- **Content migrations** - Update content after initial import with new data
- **Periodic updates** - Regular bulk updates from external data sources
- **Fixing mistakes** - Correct errors in previously imported content

## Content Update Mode

### Required Fields

To update content items, your CSV must include:

| Field | Description | Example |
|-------|-------------|---------|
| `bulkUploadShouldUpdate` | Must be `true` for the row's data to be written | `true` |
| `bulkUploadContentGuid` | GUID of the content item to update | `a1b2c3d4-e5f6-7890-abcd-ef1234567890` |
| `parent` | Parent ID, GUID, or path (for verification) | `1100` or `/News/2024/` |
| `name` | Content item name (for verification) | `My Article` |

### Optional Fields

| Field | Description | Example |
|-------|-------------|---------|
| `bulkUploadShouldPublish` | `true` to publish the item | `true` |
| `bulkUploadShouldUnpublish` | `true` to unpublish the item | `true` |
| `bulkUploadParentGuid` | GUID of a new parent to move the item to | `b2c3d4e5-f6a7-8901-bcde-f12345678901` |

See [Publish state](#publish-state) for how these combine with `bulkUploadShouldUpdate`.

Any additional columns will update those properties:
- `title` - Update the title property
- `description|text` - Update description using text resolver
- `publishDate|dateTime` - Update publish date with dateTime resolver
- Any other property with appropriate resolver

### Content Update Example

```csv
bulkUploadShouldUpdate,bulkUploadContentGuid,parent,name,title,description|text,publishDate|dateTime
true,a1b2c3d4-e5f6-7890-abcd-ef1234567890,1100,My Article,Updated Article Title,This article content has been updated,2024-02-10
true,b2c3d4e5-f6a7-8901-bcde-f12345678901,1100,Another Article,Fresh New Title,Content updated in bulk,2024-02-11
true,c3d4e5f6-a7b8-9012-cdef-012345678912,/News/2024/,Tech News,Latest Tech Updates,Breaking tech news updated via bulk upload,2024-02-12
```

### Step-by-Step: Content Update

1. **Export existing content** (optional but recommended)
   - Perform a content import to get GUIDs
   - Download the results CSV which includes `bulkUploadContentGuid` values

2. **Prepare your update CSV**
   - Add the `bulkUploadShouldUpdate` column and set to `true`
   - Add the `bulkUploadContentGuid` column with item GUIDs
   - Add `parent` and `name` for verification
   - Add any properties you want to update

3. **Upload the CSV**
   - Create a ZIP file containing your CSV
   - Go to Bulk Upload dashboard
   - Upload the ZIP file

4. **Review results**
   - Check the results summary
   - Download the results CSV for detailed status
   - Verify updates in Umbraco backoffice

## Media Update Mode

### Required Fields

To update media items, your CSV must include:

| Field | Description | Example |
|-------|-------------|---------|
| `bulkUploadShouldUpdate` | Must be `true` to update the media item | `true` |
| `bulkUploadMediaGuid` | GUID of the media item to update | `d4e5f6a7-b8c9-0123-def0-123456789abc` |
| `parent` | Parent folder ID, GUID, or path (for verification) | `1150` or `/Media/Images/` |
| `name` | Media item name (for verification) | `Company Logo` |

Media has no publish state, so `bulkUploadShouldPublish` and `bulkUploadShouldUnpublish` do not apply to media imports.

### Optional Fields

Any additional columns will update those properties:
- `altText|text` - Update alt text
- `tags|stringArray` - Update tags as comma-separated values
- Any other media property with appropriate resolver

### Media Update Example

```csv
bulkUploadShouldUpdate,bulkUploadMediaGuid,parent,name,altText|text,tags|stringArray
true,d4e5f6a7-b8c9-0123-def0-123456789abc,1150,Company Logo,Updated company logo with new branding,"branding,logo,corporate"
true,e5f6a7b8-c9d0-1234-ef01-234567890bcd,/Media/Images/,Hero Banner,Updated hero image for homepage,"hero,banner,homepage"
true,f6a7b8c9-d0e1-2345-f012-34567890cdef,/Products/Gallery/,Product Photo,Enhanced product photography,"product,gallery,photography"
```

### Step-by-Step: Media Update

1. **Get media GUIDs**
   - Option A: Perform a media import and download results CSV
   - Option B: Query Umbraco database for media GUIDs
   - Option C: Use Umbraco API to retrieve media GUIDs

2. **Prepare your update CSV**
   - Add the `bulkUploadShouldUpdate` column and set to `true`
   - Add the `bulkUploadMediaGuid` column with item GUIDs
   - Add `parent` and `name` for verification
   - Add any properties you want to update

3. **Upload the CSV**
   - Create a ZIP file containing your CSV
   - Go to Bulk Upload dashboard
   - Upload the ZIP file

4. **Review results**
   - Check the results summary
   - Download the results CSV for detailed status
   - Verify updates in Umbraco Media section

## Publish State

Three columns control what happens to each content row, and each does one job:

| Column | Governs |
|---|---|
| `bulkUploadShouldUpdate` | Whether data is written: name, parent, and property values |
| `bulkUploadShouldPublish` | Publishing, and nothing else |
| `bulkUploadShouldUnpublish` | Unpublishing, and nothing else |

`true`, `yes` and `1` (any casing, surrounding spaces ignored) are treated as true.

### Existing content

The row carries a `bulkUploadContentGuid` that resolves to a node.

| `bulkUploadShouldUpdate` | `bulkUploadShouldPublish` | `bulkUploadShouldUnpublish` | Data | Publish state |
|---|---|---|---|---|
| true | false | false | Updated | Unchanged |
| true | true | false | Updated | Published |
| true | false | true | Updated | Unpublished |
| true | true | true | Updated | Unpublished |
| false | false | false | Unchanged | Unchanged (row skipped) |
| false | true | false | Unchanged | Published |
| false | false | true | Unchanged | Unpublished |
| false | true | true | Unchanged | Unpublished |

False means the column is absent or holds any value other than `true`, `yes` or `1`.

When data is updated but the publish state is unchanged, the changes are saved as a draft and the published version keeps serving. When both `bulkUploadShouldPublish` and `bulkUploadShouldUnpublish` are true, unpublish wins and a warning is logged. A skipped row produces no result and is not counted.

If a row has property columns but `bulkUploadShouldUpdate` is not true, the property values are not written; a warning is logged and the row's result carries an info message saying so.

### New content

The row has no `bulkUploadContentGuid`.

| `bulkUploadShouldPublish` | `bulkUploadShouldUnpublish` | Result |
|---|---|---|
| false | false | Created, saved as a draft |
| true | false | Created and published |
| false | true | Created, saved as a draft |
| true | true | Created, saved as a draft |

False means the column is absent or holds any value other than `true`, `yes` or `1`.

**Note:** on a new content row, a `bulkUploadShouldUpdate` column that is present with a false value skips the row entirely, and nothing is created. This is the one place where an absent column and a false value differ: absent means "create normally", false means "skip this row".

## How Update Mode Works

### 1. Detection Phase

When you upload a CSV file, BulkUpload automatically detects the mode:

- **Create Mode** detected when CSV has `docTypeAlias` + `name` (content) or `fileName` / `mediaSource` (media)
- **Content update mode** detected when CSV has `bulkUploadContentGuid` plus at least one of `bulkUploadShouldUpdate`, `bulkUploadShouldPublish` or `bulkUploadShouldUnpublish`
- **Media update mode** detected when CSV has `bulkUploadShouldUpdate` + `bulkUploadMediaGuid`

The detection happens before processing, and you'll see a badge indicating the detected mode.

### 2. Validation Phase

For each row in update mode:
- Verifies the GUID exists in Umbraco
- Checks that `parent` and `name` match (for safety)
- Validates property types and values

### 3. Update Phase

For each valid row:
- Loads the existing content/media item
- Updates only the properties specified in the CSV
- Properties not in the CSV remain unchanged (partial updates)
- Content: saves, then publishes or unpublishes as described in [Publish state](#publish-state)
- Media: saves

### 4. Results Phase

After processing:
- Success/failure count displayed
- Results CSV includes update status for each item
- Errors logged with detailed messages

## Partial Updates

Update mode supports partial updates - you only need to include the properties you want to change:

### Example: Update Only Titles

```csv
bulkUploadShouldUpdate,bulkUploadContentGuid,parent,name,title
true,a1b2c3d4-...,1100,Article 1,New Title for Article 1
true,b2c3d4e5-...,1100,Article 2,New Title for Article 2
```

All other properties (description, images, dates, etc.) remain unchanged.

### Example: Update Only Alt Text

```csv
bulkUploadShouldUpdate,bulkUploadMediaGuid,parent,name,altText|text
true,d4e5f6a7-...,1150,Logo,Better alt text description
true,e5f6a7b8-...,1150,Banner,Improved accessibility description
```

Tags, file, and other properties remain unchanged.

## Best Practices

### 1. Always Verify Before Updating

- Include `parent` and `name` fields to verify you're updating the correct items
- Do a small test batch before bulk updating hundreds of items
- Review the sample data and verify GUIDs are correct

### 2. Export Results from Create Operations

The easiest way to get GUIDs for updates:

1. Perform a create import (content or media)
2. Download the results CSV
3. The results CSV includes `bulkUploadContentGuid` or `bulkUploadMediaGuid`
4. Use those GUIDs for subsequent updates

### 3. Use Partial Updates

Only include columns for properties you want to change:
- Reduces risk of accidentally overwriting data
- Makes CSV files cleaner and easier to maintain
- Faster processing for large batches

### 4. Keep Backup Before Large Updates

Before updating hundreds of items:
- Export content to CSV first (or use Umbraco's built-in backup)
- Test with a few items first
- Verify results before proceeding with full batch

### 5. Use Descriptive Names in Results

The results CSV includes all your input columns plus status columns:
- Keep meaningful names in your CSV for easier tracking
- Add a `notes` column for your own reference (not processed by BulkUpload)

## Common Scenarios

### Scenario 1: Update Content After Initial Import

**Situation**: You imported 500 articles but need to update the publish dates.

**Solution**:
1. Download your original import results CSV
2. Create a new CSV with: `bulkUploadShouldUpdate`, `bulkUploadContentGuid`, `parent`, `name`, `publishDate|dateTime`
3. Update only the `publishDate|dateTime` column values
4. Upload and process

### Scenario 2: Add Alt Text to Media

**Situation**: You imported 200 images but forgot to add alt text.

**Solution**:
1. Download your media import results CSV
2. Create a new CSV with: `bulkUploadShouldUpdate`, `bulkUploadMediaGuid`, `parent`, `name`, `altText|text`
3. Add alt text for each image
4. Upload and process

### Scenario 3: Update Content from External Data Source

**Situation**: You have a nightly feed from a PIM system with product updates.

**Solution**:
1. Map your PIM data to BulkUpload CSV format
2. Include `bulkUploadShouldUpdate=true` and `bulkUploadContentGuid` columns
3. Add columns for properties that need updating
4. Automate the CSV generation and upload process

### Scenario 4: Fix Bulk Import Mistakes

**Situation**: You imported content with wrong category values.

**Solution**:
1. Use your original import results to get GUIDs
2. Create update CSV with correct category values
3. Update only the category property
4. Verify results and re-run if needed

## Troubleshooting

### "Item not found" Errors

**Problem**: CSV processing fails with "Content/Media item not found" errors.

**Solutions**:
- Verify GUIDs are correct and not truncated
- Check items weren't deleted after original import
- Ensure you're using the correct environment (staging vs production)

### "Verification Failed" Errors

**Problem**: Update fails even though GUID exists.

**Solutions**:
- Check that `parent` value matches the item's actual parent
- Verify `name` matches the item's current name (case-sensitive)
- If item was moved or renamed, update those fields first

### Partial Success

**Problem**: Some rows update successfully, others fail.

**Solutions**:
- Download the results CSV to see which rows failed
- Check error messages in the results
- Fix failed rows and re-upload only those rows

### Properties Not Updating

**Problem**: Update completes but properties don't change.

**Solutions**:
- Verify property aliases match your document/media type
- Check that resolver syntax is correct (e.g., `|text`, `|dateTime`)
- Ensure values are in correct format for property type

## Sample Files

Download these sample files to get started:

- [content-upload-basic.csv](../../../samples/content-upload-basic.csv) - Content create example
- [content-unpublish-basic.csv](../../../samples/content-unpublish-basic.csv) - Unpublish existing content without changing its data

## Additional Resources

- [Sample Files Directory](../../../samples/) - More CSV examples
- [Custom Resolvers Guide](../custom-resolvers-guide.md) - Create custom property transformers
- [Media Import Guide](media-import-guide.md) - Media import documentation
- [Main README](../../../README.md) - Package overview

## Support

If you encounter issues with update mode:

1. Check the [Troubleshooting Guide](../troubleshooting.md)
2. Review your CSV format against examples in this guide
3. Download and examine the results CSV for error details
4. [Report issues](https://github.com/ClerksWell-Ltd/BulkUpload/issues) with sample data and error messages

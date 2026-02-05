# Custom Resolvers Guide

Learn how to create custom resolvers to transform CSV values for your specific needs.

## Table of Contents

- [What are Resolvers?](#what-are-resolvers)
- [Built-in Resolvers](#built-in-resolvers)
- [Creating Custom Resolvers](#creating-custom-resolvers)
- [Advanced Resolver Patterns](#advanced-resolver-patterns)
- [Best Practices](#best-practices)
- [Examples](#examples)

---

## What are Resolvers?

Resolvers are the core extensibility mechanism in BulkUpload. They transform CSV string values into the appropriate format for Umbraco properties.

### How Resolvers Work

```mermaid
flowchart LR
    CSV[CSV Value<br/>2024-01-15] --> Header[Column Header<br/>publishDate|dateTime]
    Header --> Resolver[DateTimeResolver]
    Resolver --> Output[ISO 8601 Format<br/>2024-01-15T00:00:00]
    Output --> Property[Umbraco Property<br/>publishDate]
```

### Resolver Syntax

Use the pipe (`|`) character in CSV column headers to specify a resolver:

```csv
propertyAlias|resolverAlias
```

**Examples:**
```csv
publishDate|dateTime,isPublished|boolean,tags|stringArray,heroImage|zipFileToMedia
2024-01-15,true,"featured,news",hero.jpg
```

---

## Built-in Resolvers

BulkUpload includes these resolvers out of the box:

### Basic Type Resolvers

| Resolver | Alias | Purpose | Input Example | Output Example |
|----------|-------|---------|---------------|----------------|
| Text | `text` | Plain text (default) | `Hello World` | `Hello World` |
| Boolean | `boolean` | Convert to true/false | `true`, `1`, `yes` | `true` |
| DateTime | `dateTime` | Convert to ISO 8601 | `2024-01-15` | `2024-01-15T00:00:00` |
| StringArray | `stringArray` | Comma-separated to array | `tag1,tag2,tag3` | `["tag1","tag2","tag3"]` |
| ObjectToJson | `objectToJson` | Object to JSON | `{"key":"value"}` | `{"key":"value"}` |

### Media Resolvers

| Resolver | Alias | Purpose | Input Example | Output Example |
|----------|-------|---------|---------------|----------------|
| ZipFileToMedia | `zipFileToMedia` | Create media from ZIP file | `hero.jpg` | `umb://media/guid` |
| UrlToMedia | `urlToMedia` | Download media from URL | `https://example.com/img.jpg` | `umb://media/guid` |
| PathToMedia | `pathToMedia` | Import media from file path | `C:\Images\hero.jpg` | `umb://media/guid` |
| MediaIdToMediaUdi | `mediaIdToMediaUdi` | Convert media ID to UDI | `1234` | `umb://media/guid` |
| GuidToMediaUdi | `guidToMediaUdi` | Convert GUID to media UDI | `abc-123-...` | `umb://media/guid` |

### Content Resolvers

| Resolver | Alias | Purpose | Input Example | Output Example |
|----------|-------|---------|---------------|----------------|
| ContentIdToContentUdi | `contentIdToContentUdi` | Convert content ID to UDI | `5678` | `umb://document/guid` |
| GuidToContentUdi | `guidToContentUdi` | Convert GUID to content UDI | `abc-123-...` | `umb://document/guid` |

### Stream Resolvers

| Resolver | Alias | Purpose | Input Example | Output Example |
|----------|-------|---------|---------------|----------------|
| UrlToStream | `urlToStream` | Download file from URL | `https://example.com/file.pdf` | Stream |
| PathToStream | `pathToStream` | Read file from path | `C:\Files\doc.pdf` | Stream |

---

## Creating Custom Resolvers

### Basic Resolver

Here's a simple resolver that converts text to uppercase:

```csharp
using Umbraco.Community.BulkUpload.Resolvers;

namespace MyProject.Resolvers
{
    public class UpperCaseResolver : IResolver
    {
        public string Alias() => "uppercase";

        public object Resolve(object value)
        {
            if (value == null)
                return string.Empty;

            return value.ToString()?.ToUpper() ?? string.Empty;
        }
    }
}
```

**Usage in CSV:**
```csv
title|uppercase,subtitle
hello world,A subtitle
HELLO WORLD,Another one
```

### Resolver with Dependencies

Inject Umbraco services or your own services:

```csharp
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.BulkUpload.Resolvers;

namespace MyProject.Resolvers
{
    public class CategoryResolver : IResolver
    {
        private readonly IContentService _contentService;
        private readonly ILogger<CategoryResolver> _logger;

        public CategoryResolver(
            IContentService contentService,
            ILogger<CategoryResolver> logger)
        {
            _contentService = contentService;
            _logger = logger;
        }

        public string Alias() => "category";

        public object Resolve(object value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return string.Empty;

            var categoryName = value.ToString();

            // Find or create category
            var category = FindCategoryByName(categoryName);

            if (category == null)
            {
                _logger.LogWarning("Category not found: {CategoryName}", categoryName);
                return string.Empty;
            }

            return $"umb://document/{category.Key:N}";
        }

        private IContent FindCategoryByName(string name)
        {
            // Implementation to find category by name
            var categories = _contentService.GetPagedOfType(
                contentTypeAlias: "category",
                pageIndex: 0,
                pageSize: 1,
                out long totalRecords,
                null,
                Ordering.By("name"));

            return categories.FirstOrDefault(c =>
                c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
```

### Registering Resolvers

Create a composer to register your resolvers:

```csharp
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using MyProject.Resolvers;

namespace MyProject
{
    public class MyResolversComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            // For stateless resolvers (no dependencies), use Singleton
            builder.Services.AddSingleton<IResolver, UpperCaseResolver>();

            // For resolvers with dependencies, use Transient
            builder.Services.AddTransient<IResolver, CategoryResolver>();
        }
    }
}
```

---

## Advanced Resolver Patterns

### Parameterized Resolver

Create a resolver that accepts parameters:

```csharp
public class PrefixResolver : IResolver
{
    public string Alias() => "prefix";

    public object Resolve(object value)
    {
        // Expected format: "value::prefix"
        // Example: "Product Title::PRD"

        var input = value?.ToString() ?? string.Empty;
        var parts = input.Split(new[] { "::" }, StringSplitOptions.None);

        if (parts.Length == 2)
        {
            var text = parts[0];
            var prefix = parts[1];
            return $"{prefix}-{text}";
        }

        return input;
    }
}
```

**Usage:**
```csv
name|prefix
Product Title::PRD,Description
```

**Output:** `PRD-Product Title`

### Caching Resolver

Implement caching for expensive operations:

```csharp
using System.Collections.Concurrent;
using Umbraco.Community.BulkUpload.Resolvers;

public class CachedCategoryResolver : IResolver
{
    private readonly IContentService _contentService;
    private static readonly ConcurrentDictionary<string, string> _cache = new();

    public CachedCategoryResolver(IContentService contentService)
    {
        _contentService = contentService;
    }

    public string Alias() => "cachedCategory";

    public object Resolve(object value)
    {
        var categoryName = value?.ToString();
        if (string.IsNullOrWhiteSpace(categoryName))
            return string.Empty;

        return _cache.GetOrAdd(categoryName, name =>
        {
            var category = FindCategoryByName(name);
            return category != null
                ? $"umb://document/{category.Key:N}"
                : string.Empty;
        });
    }

    private IContent FindCategoryByName(string name)
    {
        // Implementation
    }
}
```

### Multi-Value Resolver

Handle multiple values and create complex structures:

```csharp
using System.Text.Json;
using Umbraco.Community.BulkUpload.Resolvers;

public class TagsToJsonResolver : IResolver
{
    public string Alias() => "tagsToJson";

    public object Resolve(object value)
    {
        var input = value?.ToString() ?? string.Empty;
        var tags = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .ToArray();

        var tagObjects = tags.Select(tag => new
        {
            name = tag,
            slug = tag.ToLower().Replace(" ", "-")
        });

        return JsonSerializer.Serialize(tagObjects);
    }
}
```

**Usage:**
```csv
tags|tagsToJson
"Umbraco, Package, Community"
```

**Output:**
```json
[{"name":"Umbraco","slug":"umbraco"},{"name":"Package","slug":"package"},{"name":"Community","slug":"community"}]
```

---

## Best Practices

### 1. Error Handling

Always handle null values and invalid input gracefully:

```csharp
public object Resolve(object value)
{
    if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        return string.Empty;

    try
    {
        // Your transformation logic
        return TransformValue(value.ToString());
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error resolving value: {Value}", value);
        return string.Empty; // Or throw, depending on your needs
    }
}
```

### 2. Performance

- Use `AddSingleton` for stateless resolvers
- Implement caching for expensive operations
- Avoid N+1 queries - batch when possible

### 3. Logging

Include logging for debugging:

```csharp
public class MyResolver : IResolver
{
    private readonly ILogger<MyResolver> _logger;

    public MyResolver(ILogger<MyResolver> logger)
    {
        _logger = logger;
    }

    public object Resolve(object value)
    {
        _logger.LogDebug("Resolving value: {Value}", value);

        var result = TransformValue(value);

        _logger.LogDebug("Resolved to: {Result}", result);
        return result;
    }
}
```

### 4. Unit Testing

Write tests for your resolvers:

```csharp
using Xunit;

public class UpperCaseResolverTests
{
    [Fact]
    public void Resolve_WithValidInput_ReturnsUpperCase()
    {
        // Arrange
        var resolver = new UpperCaseResolver();
        var input = "hello world";

        // Act
        var result = resolver.Resolve(input);

        // Assert
        Assert.Equal("HELLO WORLD", result);
    }

    [Fact]
    public void Resolve_WithNullInput_ReturnsEmptyString()
    {
        // Arrange
        var resolver = new UpperCaseResolver();

        // Act
        var result = resolver.Resolve(null);

        // Assert
        Assert.Equal(string.Empty, result);
    }
}
```

---

## Examples

### Example 1: Slug Generator

```csharp
using System.Text.RegularExpressions;
using Umbraco.Community.BulkUpload.Resolvers;

public class SlugResolver : IResolver
{
    public string Alias() => "slug";

    public object Resolve(object value)
    {
        if (value == null)
            return string.Empty;

        var text = value.ToString()?.ToLowerInvariant() ?? string.Empty;

        // Remove invalid characters
        text = Regex.Replace(text, @"[^a-z0-9\s-]", "");

        // Replace spaces with hyphens
        text = Regex.Replace(text, @"\s+", "-");

        // Remove duplicate hyphens
        text = Regex.Replace(text, @"-+", "-");

        return text.Trim('-');
    }
}
```

**Usage:**
```csv
title,slug|slug
My Product Title,My Product Title
```

**Output:** `my-product-title`

### Example 2: Color Code Validator

```csharp
using System.Text.RegularExpressions;
using Umbraco.Community.BulkUpload.Resolvers;

public class ColorCodeResolver : IResolver
{
    private static readonly Regex HexColorRegex = new(@"^#?([0-9A-Fa-f]{6})$");

    public string Alias() => "colorCode";

    public object Resolve(object value)
    {
        if (value == null)
            return "#000000"; // Default to black

        var input = value.ToString() ?? string.Empty;
        var match = HexColorRegex.Match(input);

        if (match.Success)
        {
            return $"#{match.Groups[1].Value.ToUpper()}";
        }

        return "#000000"; // Default to black for invalid input
    }
}
```

### Example 3: External API Resolver

```csharp
using System.Net.Http;
using System.Text.Json;
using Umbraco.Community.BulkUpload.Resolvers;

public class CurrencyConverterResolver : IResolver
{
    private readonly IHttpClientFactory _httpClientFactory;

    public CurrencyConverterResolver(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public string Alias() => "convertCurrency";

    public object Resolve(object value)
    {
        // Expected format: "100::USD::EUR"
        var input = value?.ToString() ?? string.Empty;
        var parts = input.Split("::");

        if (parts.Length == 3 &&
            decimal.TryParse(parts[0], out var amount))
        {
            var fromCurrency = parts[1];
            var toCurrency = parts[2];

            // Call external API to convert
            var convertedAmount = ConvertCurrency(amount, fromCurrency, toCurrency);
            return convertedAmount;
        }

        return value;
    }

    private decimal ConvertCurrency(decimal amount, string from, string to)
    {
        // Implementation to call currency conversion API
        // This is a simplified example
        return amount * 0.85m; // Example conversion rate
    }
}
```

### Example 4: Block List Item Resolver

```csharp
using System.Text.Json;
using Umbraco.Community.BulkUpload.Resolvers;

public class SimpleBlockListResolver : IResolver
{
    public string Alias() => "simpleBlockList";

    public object Resolve(object value)
    {
        if (value == null)
            return "[]";

        var items = value.ToString()?.Split('|') ?? Array.Empty<string>();

        var blockList = new
        {
            layout = new
            {
                items = items.Select((item, index) => new
                {
                    contentUdi = $"umb://element/{Guid.NewGuid():N}",
                    settingsUdi = (string?)null
                })
            },
            contentData = items.Select(item => new
            {
                contentTypeKey = "your-block-type-key",
                udi = $"umb://element/{Guid.NewGuid():N}",
                text = item
            })
        };

        return JsonSerializer.Serialize(blockList);
    }
}
```

---

## Troubleshooting

### Resolver Not Found

**Error:** `Resolver 'myResolver' not found`

**Solution:** Ensure your resolver is registered in a composer:

```csharp
builder.Services.AddSingleton<IResolver, MyResolver>();
```

### Resolver Alias Conflict

**Error:** Multiple resolvers with the same alias

**Solution:** Each resolver must have a unique alias:

```csharp
public string Alias() => "uniqueName";
```

### Value Not Transforming

1. Check that the column header uses correct syntax: `property|resolver`
2. Verify the resolver is registered
3. Add logging to your resolver to debug
4. Check Umbraco log viewer for errors

---

## Need Help?

- See [Built-in Resolvers Source Code](../../src/BulkUpload/Resolvers/)
- Check [Troubleshooting Guide](troubleshooting.md)
- Open an [Issue](https://github.com/ClerksWell-Ltd/BulkUpload/issues)

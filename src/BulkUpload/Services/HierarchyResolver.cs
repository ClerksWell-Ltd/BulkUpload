using BulkUpload.Models;

namespace BulkUpload.Services;

/// <summary>
/// Validates and resolves import hierarchy based on legacy parent-child relationships.
/// Uses topological sorting to ensure parents are created before children.
/// </summary>
public class HierarchyResolver : IHierarchyResolver
{
    /// <inheritdoc />
    public List<ImportObject> ValidateAndSort(List<ImportObject> items)
    {
        if (items == null || items.Count == 0)
        {
            return new List<ImportObject>();
        }

        // Separate items with legacy IDs from those without
        var itemsWithLegacyId = items.Where(i => !string.IsNullOrWhiteSpace(i.LegacyId)).ToList();
        var itemsWithoutLegacyId = items.Where(i => string.IsNullOrWhiteSpace(i.LegacyId)).ToList();

        // If no items have legacy IDs, return original list (no sorting needed)
        if (itemsWithLegacyId.Count == 0)
        {
            return items;
        }

        // Validate legacy hierarchy
        ValidateDuplicateLegacyIds(itemsWithLegacyId);
        ValidateLegacyParentReferences(itemsWithLegacyId);

        // Build dependency graph and perform topological sort
        var sortedLegacyItems = TopologicalSort(itemsWithLegacyId);

        // Combine: items without legacy IDs first, then sorted legacy items
        var result = new List<ImportObject>(items.Count);
        result.AddRange(itemsWithoutLegacyId);
        result.AddRange(sortedLegacyItems);

        return result;
    }

    /// <summary>
    /// Validates that all legacy IDs are unique.
    /// </summary>
    private void ValidateDuplicateLegacyIds(List<ImportObject> items)
    {
        var duplicates = items
            .GroupBy(i => i.LegacyId, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicates.Any())
        {
            var duplicateId = duplicates.First().Key;
            var duplicateNames = string.Join(", ", duplicates.First().Select(i => $"'{i.Name}'"));
            throw new InvalidOperationException(
                $"Duplicate legacy ID found: '{duplicateId}' appears in multiple items: {duplicateNames}. " +
                "Each legacy ID must be unique.");
        }
    }

    /// <summary>
    /// Validates that all legacy parent references exist within the import set or are null/empty.
    /// </summary>
    private void ValidateLegacyParentReferences(List<ImportObject> items)
    {
        var legacyIds = new HashSet<string>(
            items.Select(i => i.LegacyId!),
            StringComparer.OrdinalIgnoreCase);

        var invalidReferences = items
            .Where(i => !string.IsNullOrWhiteSpace(i.LegacyParentId))
            .Where(i => !legacyIds.Contains(i.LegacyParentId!))
            .ToList();

        if (invalidReferences.Any())
        {
            var first = invalidReferences.First();
            throw new InvalidOperationException(
                $"Legacy parent ID '{first.LegacyParentId}' referenced by item '{first.Name}' " +
                $"(legacy ID: '{first.LegacyId}') was not found in the import data. " +
                "All legacy parent IDs must reference items within the same import.");
        }
    }

    /// <summary>
    /// Performs topological sort on items based on legacy parent-child relationships.
    /// Ensures parents are always before children in the result.
    /// </summary>
    private List<ImportObject> TopologicalSort(List<ImportObject> items)
    {
        // Build lookup dictionary: legacyId -> ImportObject
        var itemsByLegacyId = items.ToDictionary(
            i => i.LegacyId!,
            StringComparer.OrdinalIgnoreCase);

        // Build dependency graph: legacyId -> list of children's legacy IDs
        var children = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var inDegree = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Initialize all nodes
        foreach (var item in items)
        {
            children[item.LegacyId!] = new List<string>();
            inDegree[item.LegacyId!] = 0;
        }

        // Build edges: parent -> child
        foreach (var item in items)
        {
            if (!string.IsNullOrWhiteSpace(item.LegacyParentId))
            {
                // This item has a parent in the legacy hierarchy
                children[item.LegacyParentId!].Add(item.LegacyId!);
                inDegree[item.LegacyId!]++;
            }

            // Also add dependencies from content picker properties
            if (item.ContentPickerDependencies != null && item.ContentPickerDependencies.Count > 0)
            {
                foreach (var dependencyLegacyId in item.ContentPickerDependencies)
                {
                    // Only add dependency if the referenced item is in this import batch
                    if (children.ContainsKey(dependencyLegacyId))
                    {
                        children[dependencyLegacyId].Add(item.LegacyId!);
                        inDegree[item.LegacyId!]++;
                    }
                    // If dependency is not in this batch, ignore it (may already exist in Umbraco)
                }
            }
        }

        // Find all root nodes (items with no legacy parent or parent outside this import)
        var queue = new Queue<string>();
        foreach (var kvp in inDegree)
        {
            if (kvp.Value == 0)
            {
                queue.Enqueue(kvp.Key);
            }
        }

        var sortedIds = new List<string>();

        // Kahn's algorithm for topological sort
        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            sortedIds.Add(currentId);

            // Process all children
            foreach (var childId in children[currentId])
            {
                inDegree[childId]--;
                if (inDegree[childId] == 0)
                {
                    queue.Enqueue(childId);
                }
            }
        }

        // Check for circular references
        if (sortedIds.Count != items.Count)
        {
            // There's a cycle - find it for error message
            var unsortedIds = items
                .Select(i => i.LegacyId!)
                .Except(sortedIds, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var cycle = FindCycle(unsortedIds.First(), itemsByLegacyId);

            throw new InvalidOperationException(
                $"Circular reference detected in legacy hierarchy: {cycle}. " +
                "Please check your legacy parent ID references.");
        }

        // Convert sorted IDs back to ImportObjects
        return sortedIds.Select(id => itemsByLegacyId[id]).ToList();
    }

    /// <summary>
    /// Finds and formats a cycle in the dependency graph for error reporting.
    /// </summary>
    private string FindCycle(string startId, Dictionary<string, ImportObject> itemsByLegacyId)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var path = new List<string>();

        string? currentId = startId;

        while (currentId != null && !visited.Contains(currentId))
        {
            visited.Add(currentId);
            path.Add(currentId);

            if (itemsByLegacyId.TryGetValue(currentId, out var item))
            {
                currentId = item.LegacyParentId;
            }
            else
            {
                break;
            }
        }

        // If we found a cycle, currentId will be in the path
        if (currentId != null && visited.Contains(currentId))
        {
            var cycleStartIndex = path.IndexOf(currentId);
            var cyclePath = path.Skip(cycleStartIndex).ToList();
            cyclePath.Add(currentId); // Close the cycle
            return string.Join(" → ", cyclePath);
        }

        // Fallback if we can't determine exact cycle
        return string.Join(" → ", path);
    }
}
using Umbraco.Community.BulkUpload.Models;

namespace Umbraco.Community.BulkUpload.Services
{
    /// <summary>
    /// Validates and resolves import hierarchy based on legacy parent-child relationships.
    /// Ensures items are sorted in dependency order (parents before children).
    /// </summary>
    public interface IHierarchyResolver
    {
        /// <summary>
        /// Validates the legacy hierarchy and returns items sorted in dependency order.
        /// Items without legacy IDs are placed first, followed by items sorted so that
        /// parents are created before their children.
        /// </summary>
        /// <param name="items">The list of import objects to validate and sort.</param>
        /// <returns>A new list with items sorted in dependency order.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when validation fails due to:
        /// - Duplicate legacy IDs
        /// - Circular references in the hierarchy
        /// - Invalid legacy parent references
        /// </exception>
        List<ImportObject> ValidateAndSort(List<ImportObject> items);
    }
}

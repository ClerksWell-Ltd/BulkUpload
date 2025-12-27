namespace BulkUpload.Core.Models;

public class BlockList //this class is to mock the correct JSON structure when the object is serialized
{
    public required BlockListUdi layout { get; set; }
    public required List<Dictionary<string, string>> contentData { get; set; }
    public required List<Dictionary<string, string>> settingsData { get; set; }
}
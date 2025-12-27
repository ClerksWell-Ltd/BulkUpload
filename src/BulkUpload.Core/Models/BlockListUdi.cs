using Newtonsoft.Json;

namespace BulkUpload.Core.Models;

public class BlockListUdi //this is a subclass which corresponds to the "Umbraco.BlockList" section in JSON
{
    [JsonProperty("Umbraco.BlockList")]  //we mock the Umbraco.BlockList name with JsonPropertyAttribute to match the requested JSON structure
    public List<Dictionary<string, string>> contentUdi { get; set; }

    public BlockListUdi(List<Dictionary<string, string>> items)
    {
        this.contentUdi = items;
    }
}
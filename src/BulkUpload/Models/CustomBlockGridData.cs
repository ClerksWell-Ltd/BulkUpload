using Newtonsoft.Json;

using Umbraco.Cms.Core;

namespace BulkUpload.Models;

public class CustomBlockGridData
{
    public CustomBlockGridData(BlockGridLayout layout, BlockGridElementData[] contentData)
    {
        Layout = layout;
        ContentData = contentData;
    }

    [JsonProperty("layout")]
    public BlockGridLayout Layout { get; }

    [JsonProperty("contentData")]
    public BlockGridElementData[] ContentData { get; }

}

public class BlockGridLayout
{
    public BlockGridLayout(BlockGridLayoutItem[] layoutItems) => LayoutItems = layoutItems;

    [JsonProperty("Umbraco.BlockGrid")]
    public BlockGridLayoutItem[] LayoutItems { get; }
}

public class BlockGridLayoutItem
{
    public BlockGridLayoutItem(Udi contentUdi, Udi settingsUdi, int columnSpan, int rowSpan)
    {
        ContentUdi = contentUdi;
        ColumnSpan = columnSpan;
        RowSpan = rowSpan;
    }

    [JsonProperty("contentUdi")]
    public Udi ContentUdi { get; }

    [JsonProperty("areas")]
    public object[] Areas { get; } = { };

    [JsonProperty("columnSpan")]
    public int ColumnSpan { get; }

    [JsonProperty("rowSpan")]
    public int RowSpan { get; }

}

public class BlockGridElementData
{
    public BlockGridElementData(Guid contentTypeKey, Udi udi, Dictionary<string, object> data)
    {
        ContentTypeKey = contentTypeKey;
        Udi = udi;
        Data = data;
    }

    [JsonProperty("contentTypeKey")]
    public Guid ContentTypeKey { get; }

    [JsonProperty("udi")]
    public Udi Udi { get; }

    [JsonExtensionData]
    public Dictionary<string, object> Data { get; }
}
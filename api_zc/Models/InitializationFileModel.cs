using System.Text.Json.Serialization;

namespace Accura_MES.Models
{
    public class InitializationFileModel
    {
    }

    /// <summary>
    /// json mapping for propertyAndMenuList.json
    /// </summary>
    public class MappingItem_propertyAndMenuList
    {
        public string ItemTypeName { get; set; }
        public string PropertyName { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// json mapping for MenuListAndDropList.json
    /// </summary>
    public class Mapping_MenuListAndDropList
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public bool IsItemList { get; set; }
        public bool IsDelete { get; set; }
        public I18n? I18n { get; set; }
        public List<DropListItem> DropList { get; set; } = new(); // 預設為空集合
    }

    public class I18n
    {
        [JsonPropertyName("zh-TW")]
        public string? ZhTW { get; set; } // "zh-TW"
        [JsonPropertyName("en")]
        public string? En { get; set; }   // "en"
    }

    public class DropListItem
    {
        public I18n? I18n { get; set; }
        public string? Value { get; set; }
        public string? Descripetion { get; set; }
        public string? Type { get; set; }
        public bool IsDelete { get; set; }
    }

}

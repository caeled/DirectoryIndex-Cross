using System.Text.Json.Serialization;

namespace DirectoryIndexGenerator.Models;

public class FolderEntry
{
    [JsonPropertyName("name")]       public string          Name       { get; set; } = "";
    [JsonPropertyName("path")]       public string          Path       { get; set; } = "";
    [JsonPropertyName("cover")]      public string?         CoverImage { get; set; }
    [JsonPropertyName("files")]      public List<FileEntry> Files      { get; set; } = [];
    // Array of subfolder name strings — matches Python output exactly
    [JsonPropertyName("subfolders")] public List<string>    SubFolders { get; set; } = [];

    // Not serialized — used internally to recurse
    [JsonIgnore] public List<FolderEntry> Children { get; set; } = [];
}

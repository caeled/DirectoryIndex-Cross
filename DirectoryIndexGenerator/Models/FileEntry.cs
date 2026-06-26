using System.Text.Json.Serialization;

namespace DirectoryIndexGenerator.Models;

public class FileEntry
{
    [JsonPropertyName("name")]     public string  Name      { get; set; } = "";
    [JsonPropertyName("path")]     public string  Path      { get; set; } = "";
    [JsonPropertyName("fullPath")] public string  FullPath  { get; set; } = "";
    [JsonPropertyName("size")]     public long    Size      { get; set; }
    [JsonPropertyName("type")]     public string  FileType  { get; set; } = "other";
    [JsonPropertyName("extension")]public string  Extension { get; set; } = "";

    [JsonPropertyName("title")]    public string? Title     { get; set; }
    [JsonPropertyName("artist")]   public string? Artist    { get; set; }
    [JsonPropertyName("album")]    public string? Album     { get; set; }
    [JsonPropertyName("cover")]    public string? Cover     { get; set; }
}

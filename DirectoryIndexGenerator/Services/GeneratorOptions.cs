namespace DirectoryIndexGenerator.Services;

public class GeneratorOptions
{
    public string FolderPath       { get; set; } = "";
    public string PageTitle        { get; set; } = "Folder Index";
    public string AuthorName       { get; set; } = "";
    public string HeaderImagePath  { get; set; } = "";
    public string ImageStyle       { get; set; } = "Cover";
    public int    ImageHeight      { get; set; } = 220;
    public string ImageOverlayText { get; set; } = "";
    public bool   ShowOverlayText  { get; set; } = false;
    public string TextAlign        { get; set; } = "left";
}

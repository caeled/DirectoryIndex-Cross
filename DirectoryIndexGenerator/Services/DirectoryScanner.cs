using System.IO;
using DirectoryIndexGenerator.Models;

namespace DirectoryIndexGenerator.Services;

public static class DirectoryScanner
{
    private static readonly HashSet<string> IgnoreFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "$RECYCLE.BIN", "System Volume Information", ".git", "__pycache__",
        "node_modules", ".vs", "bin", "obj", ".thumbs", "Thumbs", ".thumbnails"
    };

    private static readonly HashSet<string> SkipFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "index.html", "generate_index.py", "generate_index.spec",
        "generate-index.ps1", "generate-index.js"
    };

    private static string _rootPath = "";

    public static FolderEntry Scan(string rootPath, IProgress<string>? progress = null)
    {
        _rootPath = rootPath;
        return ScanFolder(rootPath, progress);
    }

    private static FolderEntry ScanFolder(string path, IProgress<string>? progress)
    {
        var relPath = Path.GetRelativePath(_rootPath, path).Replace('\\', '/');
        if (relPath == ".") relPath = "./";
        else relPath = "./" + relPath + "/";

        progress?.Report($"Scanning: {relPath}");

        var entry = new FolderEntry
        {
            Name = Path.GetFileName(path) ?? path,
            Path = relPath,
        };

        List<string> allItems;
        try { allItems = [.. Directory.EnumerateFileSystemEntries(path).OrderBy(x => x)]; }
        catch (UnauthorizedAccessException) { return entry; }

        // Subfolders — store name strings only (Python behaviour), recurse via Children
        foreach (var item in allItems)
        {
            if (!Directory.Exists(item)) continue;
            var name = Path.GetFileName(item);
            if (IgnoreFolders.Contains(name)) continue;
            entry.SubFolders.Add(name);
            entry.Children.Add(ScanFolder(item, progress));
        }

        // Files
        foreach (var item in allItems)
        {
            if (!File.Exists(item)) continue;
            var fileName = Path.GetFileName(item);
            if (SkipFiles.Contains(fileName)) continue;

            var baseName  = Path.GetFileNameWithoutExtension(fileName);
            var ext       = Path.GetExtension(fileName);
            var extNoDot  = ext.TrimStart('.').ToLowerInvariant();
            var fileType  = GetFileType(ext);

            long size = 0;
            try { size = new FileInfo(item).Length; } catch { }

            var fe = new FileEntry
            {
                Name      = fileName,
                Path      = fileName,
                FullPath  = (relPath.TrimEnd('/') + "/" + fileName).Replace("//", "/"),
                Size      = size,
                FileType  = fileType,
                Extension = extNoDot,
            };

            // Companion cover (e.g. TrackName.jpg alongside TrackName.mp3)
            foreach (var cExt in new[] { ".jpg", ".jpeg", ".png", ".webp" })
            {
                if (File.Exists(Path.Combine(path, baseName + cExt)))
                {
                    fe.Cover = baseName + cExt;
                    break;
                }
            }

            if (ext.Equals(".mp3",  StringComparison.OrdinalIgnoreCase)) MetadataExtractor.EnrichMp3(fe, path);
            if (ext.Equals(".epub", StringComparison.OrdinalIgnoreCase)) MetadataExtractor.EnrichEpub(fe, path);

            entry.Files.Add(fe);
        }

        entry.CoverImage = MetadataExtractor.FindFolderCover(path);
        return entry;
    }

    private static string GetFileType(string ext) => ext.ToLowerInvariant() switch
    {
        ".mp3" or ".flac" or ".wav" or ".ogg" or ".m4a" or ".aac"
            or ".wma" or ".opus" or ".aiff" => "audio",
        ".mp4" or ".mkv" or ".avi" or ".mov" or ".wmv" or ".webm"
            or ".m4v" or ".flv" or ".ts" or ".m2ts" => "video",
        ".epub" => "epub",
        ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".svg"
            or ".bmp" or ".tiff" => "image",
        ".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx"
            or ".ppt" or ".pptx" or ".txt" or ".md" => "document",
        _ => "other"
    };
}

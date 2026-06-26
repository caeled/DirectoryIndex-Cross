using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using DirectoryIndexGenerator.Models;

namespace DirectoryIndexGenerator.Services;

/// <summary>
/// Extracts metadata from MP3 (ID3v2) and EPUB files without external libraries.
/// Mirrors the logic in the Python generate_index.py scanner.
/// </summary>
public static class MetadataExtractor
{
    private static readonly string[] CoverNames =
        ["cover", "folder", "front", "artwork", "thumbnail"];

    private static readonly string[] CoverExts =
        [".jpg", ".jpeg", ".png", ".webp"];

    // ── Public API ────────────────────────────────────────────────────────

    public static void EnrichMp3(FileEntry entry, string? folderPath = null)
    {
        var fullPath = folderPath != null ? Path.Combine(folderPath, entry.Name) : entry.Name;
        try
        {
            using var fs = File.OpenRead(fullPath);
            var header = new byte[10];
            if (fs.Read(header, 0, 10) < 10) return;
            if (header[0] != 'I' || header[1] != 'D' || header[2] != '3') return;

            int tagSize = ((header[6] & 0x7F) << 21)
                        | ((header[7] & 0x7F) << 14)
                        | ((header[8] & 0x7F) << 7)
                        | (header[9] & 0x7F);

            var tagData = new byte[tagSize];
            fs.Read(tagData, 0, tagSize);

            int pos = 0;
            while (pos + 10 < tagSize)
            {
                var frameId = Encoding.ASCII.GetString(tagData, pos, 4);
                if (frameId == "\0\0\0\0") break;

                int frameSize = (tagData[pos + 4] << 24)
                              | (tagData[pos + 5] << 16)
                              | (tagData[pos + 6] << 8)
                              | tagData[pos + 7];

                if (frameSize <= 0 || pos + 10 + frameSize > tagSize) break;

                var frameData = tagData.AsSpan(pos + 10 + 1, Math.Max(0, frameSize - 1));

                switch (frameId)
                {
                    case "TIT2": entry.Title  = DecodeText(frameData); break;
                    case "TPE1": entry.Artist = DecodeText(frameData); break;
                    case "TALB": entry.Album  = DecodeText(frameData); break;
                }

                pos += 10 + frameSize;
            }
        }
        catch { /* non-critical */ }
    }

    public static void EnrichEpub(FileEntry entry, string? folderPath = null)
    {
        var fullPath = folderPath != null ? Path.Combine(folderPath, entry.Name) : entry.Name;
        try
        {
            using var zip = ZipFile.OpenRead(fullPath);

            // Find OPF path from container.xml
            var container = zip.GetEntry("META-INF/container.xml");
            if (container is null) return;

            string opfPath;
            using (var stream = container.Open())
            {
                var doc = XDocument.Load(stream);
                XNamespace ns = "urn:oasis:names:tc:opendocument:xmlns:container";
                opfPath = doc.Descendants(ns + "rootfile")
                             .FirstOrDefault()
                             ?.Attribute("full-path")?.Value ?? "";
            }

            if (string.IsNullOrEmpty(opfPath)) return;

            var opfEntry = zip.GetEntry(opfPath);
            if (opfEntry is null) return;

            using (var stream = opfEntry.Open())
            {
                var doc = XDocument.Load(stream);
                XNamespace dc  = "http://purl.org/dc/elements/1.1/";
                XNamespace opf = "http://www.idpf.org/2007/opf";

                entry.Title  = doc.Descendants(dc + "title") .FirstOrDefault()?.Value;
                entry.Artist = doc.Descendants(dc + "creator").FirstOrDefault()?.Value;

                // Extract cover image
                TryExtractCover(zip, doc, opf, opfPath, entry, folderPath);
            }
        }
        catch { /* non-critical */ }
    }

    public static string? FindFolderCover(string folderPath)
    {
        foreach (var ext in CoverExts)
            foreach (var name in CoverNames)
            {
                var candidate = Path.Combine(folderPath, name + ext);
                if (File.Exists(candidate)) return candidate;
            }

        // Fall back to first image in folder
        return Directory.EnumerateFiles(folderPath)
                        .FirstOrDefault(f => CoverExts.Contains(Path.GetExtension(f).ToLower()));
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static string DecodeText(ReadOnlySpan<byte> data)
    {
        var bytes = data.ToArray();
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2).TrimEnd('\0');
        return Encoding.UTF8.GetString(bytes).TrimEnd('\0');
    }

    private static void TryExtractCover(ZipArchive zip, XDocument opf,
        XNamespace opfNs, string opfPath, FileEntry entry, string? folderPath)
    {
        var manifestItems = opf.Descendants(opfNs + "item")
            .ToDictionary(
                e => e.Attribute("id")?.Value ?? "",
                e => e.Attribute("href")?.Value ?? "");

        // Look for cover via meta name="cover"
        var coverId = opf.Descendants(opfNs + "meta")
            .FirstOrDefault(e => e.Attribute("name")?.Value == "cover")
            ?.Attribute("content")?.Value;

        string? coverHref = null;
        if (coverId != null && manifestItems.TryGetValue(coverId, out var href))
            coverHref = href;
        else
            coverHref = manifestItems.Values
                .FirstOrDefault(v => CoverNames.Any(n => v.Contains(n, StringComparison.OrdinalIgnoreCase))
                                  && CoverExts.Any(e => v.EndsWith(e, StringComparison.OrdinalIgnoreCase)));

        if (coverHref is null) return;

        var opfDir  = Path.GetDirectoryName(opfPath)?.Replace('\\', '/') ?? "";
        var fullKey = string.IsNullOrEmpty(opfDir) ? coverHref : $"{opfDir}/{coverHref}";
        var zipEntry = zip.GetEntry(fullKey);
        if (zipEntry is null) return;

        var coverFileName = entry.Name + ".cover.jpg";
        var sidecarPath   = folderPath != null
            ? Path.Combine(folderPath, coverFileName)
            : coverFileName;
        using var imgStream = zipEntry.Open();
        using var outStream = File.Create(sidecarPath);
        imgStream.CopyTo(outStream);

        entry.Cover = coverFileName;
    }
}

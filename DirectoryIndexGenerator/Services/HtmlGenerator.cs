using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DirectoryIndexGenerator.Models;

namespace DirectoryIndexGenerator.Services;

public static class HtmlGenerator
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented          = false,
    };

    public static void Generate(GeneratorOptions options, IProgress<string>? progress = null)
    {
        // 1. Scan entire tree
        progress?.Report("Scanning directory…");
        var root = DirectoryScanner.Scan(options.FolderPath, progress);
        root.Name = options.PageTitle;

        // 2. Load template + build shared HTML fragments once
        progress?.Report("Loading HTML template…");
        var template         = LoadTemplate();
        var headerImageHtml  = BuildHeaderImageHtml(options);
        var headerTitleClass = (!string.IsNullOrEmpty(headerImageHtml)
                                && options.ShowOverlayText
                                && !string.IsNullOrEmpty(options.ImageOverlayText))
                               ? "hidden" : "";

        // 3. Write index.html for every folder (root + all subfolders)
        WriteFolder(root, options.FolderPath, template, options, headerImageHtml, headerTitleClass, progress);

        progress?.Report("Done.");
    }

    private static void WriteFolder(FolderEntry folder, string diskPath, string template,
        GeneratorOptions options, string headerImageHtml, string headerTitleClass,
        IProgress<string>? progress)
    {
        progress?.Report($"Writing: {folder.Path}");

        var json = JsonSerializer.Serialize(folder, JsonOpts);

        var html = template
            .Replace("const embeddedFolderData = null;", $"const embeddedFolderData = {json};")
            .Replace("__PAGE_TITLE__",         options.PageTitle)
            .Replace("__AUTHOR_NAME__",        options.AuthorName)
            .Replace("__HEADER_IMAGE__",       headerImageHtml)
            .Replace("__HEADER_TITLE_CLASS__", headerTitleClass);

        File.WriteAllText(Path.Combine(diskPath, "index.html"), html, Encoding.UTF8);

        // Recurse into subfolders
        foreach (var child in folder.Children)
        {
            var childDisk = Path.Combine(diskPath, child.Name);
            WriteFolder(child, childDisk, template, options, headerImageHtml, headerTitleClass, progress);
        }
    }

    private static string BuildHeaderImageHtml(GeneratorOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.HeaderImagePath) ||
            !File.Exists(options.HeaderImagePath))
            return "";

        var ext  = Path.GetExtension(options.HeaderImagePath).TrimStart('.').ToLower();
        var mime = ext switch { "jpg" => "image/jpeg", "jpeg" => "image/jpeg",
                                "png" => "image/png",  "webp" => "image/webp",
                                "gif" => "image/gif",  "svg"  => "image/svg+xml",
                                _ => "image/jpeg" };
        var b64    = Convert.ToBase64String(File.ReadAllBytes(options.HeaderImagePath));
        var imgSrc = $"data:{mime};base64,{b64}";
        var h      = options.ImageHeight;

        var txtHtml = "";
        if (options.ShowOverlayText && !string.IsNullOrEmpty(options.ImageOverlayText))
        {
            var align = options.TextAlign;
            var iconSvg = "<svg style=\"width:24px;height:24px;\" fill=\"none\" viewBox=\"0 0 24 24\" " +
                          "stroke=\"currentColor\" stroke-width=\"2\">" +
                          "<path stroke-linecap=\"round\" stroke-linejoin=\"round\" " +
                          "d=\"M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z\" /></svg>";
            var iconBlock = $"<div style=\"padding:10px;border-radius:12px;background:#4f46e5;" +
                            $"color:#fff;box-shadow:0 2px 8px rgba(0,0,0,.25);flex-shrink:0;\">{iconSvg}</div>";
            txtHtml = $"<div style=\"display:flex;align-items:center;gap:12px;justify-content:{align};\">" +
                      $"{iconBlock}" +
                      $"<div><div style=\"font-size:1.25rem;font-weight:700;letter-spacing:-.02em;line-height:1.2;\">" +
                      $"{options.ImageOverlayText}</div>" +
                      $"<div style=\"font-size:.7rem;font-family:monospace;opacity:.5;\">./</div>" +
                      $"</div></div>";
        }

        return options.ImageStyle.ToLower() switch
        {
            "banner" => BuildBannerHtml(imgSrc, h, txtHtml, options.TextAlign),
            "contain" or "centered" => BuildCenteredHtml(imgSrc, h, txtHtml, options.TextAlign),
            _ => BuildLogoHtml(imgSrc, h, txtHtml, options.TextAlign),  // cover / default
        };
    }

    private static string BuildBannerHtml(string src, int h, string txtHtml, string align)
    {
        var justify = align == "left" ? "flex-start" : "center";
        var overlay = string.IsNullOrEmpty(txtHtml) ? "" :
            $"<div style=\"position:absolute;bottom:0;left:0;right:0;padding:14px 22px;" +
            $"background:linear-gradient(transparent,rgba(0,0,0,.55));color:#fff;" +
            $"display:flex;justify-content:{justify};\">{txtHtml}</div>";
        return $"<div style=\"position:relative;width:100%;overflow:hidden;max-height:{h}px;\">" +
               $"<img src=\"{src}\" alt=\"Page Header\" " +
               $"style=\"width:100%;max-height:{h}px;object-fit:cover;display:block;\" />" +
               $"{overlay}</div>";
    }

    private static string BuildCenteredHtml(string src, int h, string txtHtml, string align)
    {
        var below = string.IsNullOrEmpty(txtHtml) ? "" :
            $"<div style=\"margin-top:10px;color:inherit;width:100%;text-align:{align};\">{txtHtml}</div>";
        return $"<div class=\"border-b border-slate-100 dark:border-zinc-900\" " +
               $"style=\"display:flex;flex-direction:column;align-items:center;padding:20px 16px;background:inherit;\">" +
               $"<img src=\"{src}\" alt=\"Page Header\" style=\"max-height:{h}px;max-width:100%;object-fit:contain;\" />" +
               $"{below}</div>";
    }

    private static string BuildLogoHtml(string src, int h, string txtHtml, string align)
    {
        var right = string.IsNullOrEmpty(txtHtml) ? "" :
            $"<div style=\"margin-left:20px;color:inherit;flex:1;text-align:{align};\">{txtHtml}</div>";
        return $"<div class=\"border-b border-slate-100 dark:border-zinc-900\" " +
               $"style=\"display:flex;flex-direction:row;align-items:center;padding:16px 22px;background:inherit;\">" +
               $"<img src=\"{src}\" alt=\"Page Header\" " +
               $"style=\"max-height:{h}px;max-width:40%;object-fit:contain;flex-shrink:0;\" />" +
               $"{right}</div>";
    }

    private static string LoadTemplate()
    {
        var exeDir   = AppContext.BaseDirectory;
        var external = Path.Combine(exeDir, "HtmlTemplate.html");
        if (File.Exists(external))
            return File.ReadAllText(external, Encoding.UTF8);

        var asm     = typeof(HtmlGenerator).Assembly;
        var resName = "DirectoryIndexGenerator.Resources.HtmlTemplate.html";
        using var stream = asm.GetManifestResourceStream(resName)
            ?? throw new FileNotFoundException(
                "HtmlTemplate.html not found as embedded resource or alongside the executable.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}

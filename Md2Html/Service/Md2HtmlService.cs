using System.IO.Compression;
using System.Xml.Linq;
using System.Xml.XPath;
using Markdig;
using Md2HtmlAPI.Model;

namespace Md2HtmlAPI.Service;

public sealed class Md2HtmlService : IMd2HtmlService
{
    private const string EXTRACTION_DIR = "ZipExtraction";

    private readonly ILogger<Md2HtmlService> _logger;

    private readonly MarkdownPipeline _pipeline;

    //var _pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UseBootstrap().Build();

    public Md2HtmlService(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<Md2HtmlService>();


        _pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UseBootstrap().Build();

    }

    /// <inheritdoc/>
    public async Task<string> ConvertAsync(IFormFile formFile)
    {
        await using var memoryStream = new MemoryStream();
        await formFile.CopyToAsync(memoryStream);

        using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);

        var extractionFolder = ExtractionFolder();

        archive.ExtractToDirectory(extractionFolder.FullName);

        var rootFolder = GetRootFolder(extractionFolder);

        var singleMdFileName = SingleMarkdownFromFolder(rootFolder);

        var html = RenderHTMLFromMD(singleMdFileName, _pipeline);

        html = MakeHtmlImagesInline(rootFolder, html);

        // Remove the directory
        if (extractionFolder.Exists)
        {
            extractionFolder.Delete(true);
        }

        return html;
    }

    public async Task<string> ConvertAsync(IFormFile formFile, string title)
    {
        await using var memoryStream = new MemoryStream();
        await formFile.CopyToAsync(memoryStream);

        using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);

        var extractionFolder = ExtractionFolder();

        archive.ExtractToDirectory(extractionFolder.FullName);

        var rootFolder = GetRootFolder(extractionFolder);

        var singleMdFileName = SingleMarkdownFromFolder(rootFolder);

        var html = RenderHTMLFromMD(singleMdFileName, _pipeline, title);

        html = MakeHtmlImagesInline(rootFolder, html);

        // Remove the directory
        if (extractionFolder.Exists)
        {
            extractionFolder.Delete(true);
        }

        return html;
    }

    private DirectoryInfo ExtractionFolder()
    {
        var executionPath = System.AppDomain.CurrentDomain.BaseDirectory;
        return new DirectoryInfo(Path.Join(executionPath, EXTRACTION_DIR, DateTime.Now.ToString("yyyyMMdd_HH-mm-ss-ffff")));
    }

    /// <summary>
    /// Retrieve the root folder of the extracted ZIP, since it is a good practice
    /// for a ZIP file to produce a single parent folder when extracted.
    /// If no single parent folder, the folder itself is considered as the root
    /// </summary>
    /// <param name="extractionFolder"></param>
    /// <returns></returns>
    private DirectoryInfo GetRootFolder(DirectoryInfo extractionFolder)
    {
        if (extractionFolder.GetFiles()?.Count() == 0)
        {
            var folders = extractionFolder.GetDirectories();
            if (folders?.Count() == 1)
            {
                return folders[0];
            }
        }

        return extractionFolder;
    }

    private string RenderHTMLFromMD(FileInfo mdFile, MarkdownPipeline pipeline)
    {
        string markdownContent = System.IO.File.ReadAllText(mdFile.FullName);

        //HtmlDocument 

        var result = Markdown.ToHtml(markdownContent, pipeline);

        var header = $"<!DOCTYPE html><html><head><title>{mdFile.Name}</title>";
        header += "<meta http-equiv=\"Content-type\" content=\"text/html;charset=UTF-8\" />";
        header += "</head><body>";
        var footer = "</body></html>";
        result = $"{header}{result}{footer}";

        return result;
    }

    private string RenderHTMLFromMD(FileInfo mdFile, MarkdownPipeline pipeline, string title)
    {
        string markdownContent = System.IO.File.ReadAllText(mdFile.FullName);

        //HtmlDocument 

        var result = Markdown.ToHtml(markdownContent, pipeline);

        var header = $"<!DOCTYPE html><html><head><title>{title}</title>";
        header += "<meta http-equiv=\"Content-type\" content=\"text/html;charset=UTF-8\" />";
        header += "</head><body>";
        var footer = "</body></html>";
        result = $"{header}{result}{footer}";

        return result;
    }

    /// <summary>
    /// Get the single Markdown file at the root of the folder
    /// </summary>
    /// <param name="folder"></param>
    /// <returns></returns>
    /// <exception cref="Exception">When not exactly one Markdown file at the root of the folder</exception>
    private FileInfo SingleMarkdownFromFolder(DirectoryInfo folder)
    {
        var files = folder.GetFiles("*.md");
        if (files.Count() == 0)
        {
            throw new Exception($"No Markdown file found in Zip file");
        }
        else if (files.Count() > 1)
        {
            var concatfileName = String.Empty;
            foreach (var file in files)
            {
                concatfileName += $", {file.Name}";
            }
            throw new Exception($"More than one Markdown file ({files.Count()}) found in Zip file {concatfileName}");
        }

        return files[0];
    }

    private string MakeHtmlImagesInline(DirectoryInfo folder, string htmlContent)
    {
        var doc = XDocument.Parse(htmlContent);

        var imageNodes = doc.XPathSelectElements("//img");

        foreach (var imageNode in imageNodes)
        {
            var relativePathToImageFile = imageNode.Attribute("src")?.Value;
            var base64Img = Base64ImgString(relativePathToImageFile, folder);
            imageNode.SetAttributeValue("src", base64Img);
        }

        return doc.ToString();
    }

    /// <summary>
    /// Return the base 64 displayable content of a file name that 
    /// is supposed to be in CONFIGURATION_FOLDER/ICON_FOLDER with respect
    /// to the execution directory
    /// </summary>
    /// <param name="relativePathToImageFile"></param>
    /// <returns></returns>
    private string Base64ImgString(string relativePathToImageFile, DirectoryInfo folder)
    {
        var file = new FileInfo(Path.Join(folder.FullName, relativePathToImageFile));

        if (!file.Exists)
        {
            throw new Exception($"Image with relative path '{relativePathToImageFile}' cannot be found !");
        }

        return Base64ImgStringFromFile(file);
    }

    /// <summary>
    /// Return the base 64 displayable content of a file
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    private string Base64ImgStringFromFile(FileInfo file)
    {
        if (file != null && file.Exists && !String.IsNullOrEmpty(Base64MimeType(file)))
        {
            var contents = System.IO.File.ReadAllBytes(file.FullName);

            var contentStr = $"{Base64MimeType(file)},{Convert.ToBase64String(contents)}";
            return contentStr;
        }

        return null;
    }

    private string Base64MimeType(FileInfo file)
    {

        if (file != null && file.Extension.ToLower() == ".png")
        {
            return $"data:image/png;base64";
        }
        if (file != null && file.Extension.ToLower() == ".jpeg")
        {
            return $"data:image/jpg;base64";
        }
        if (file != null && file.Extension.ToLower() == ".jpg")
        {
            return $"data:image/jpg;base64";
        }

        return string.Empty;
    }
}

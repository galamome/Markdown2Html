using Md2HtmlAPI.Model.DataAnnotations;
using Md2HtmlAPI.Model.DataAnnotations;

namespace Md2HtmlAPI.Dto;

/// <summary>
/// Descriptor Data Transfer Object
/// </summary>
public sealed class ZipWithDataDto
{
    public string Title { get; init; }

    [ZipFileExtensionsAttribute]
    public IFormFile FormFile { get; init; }
}
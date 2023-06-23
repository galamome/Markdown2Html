using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Md2HtmlAPI.Dto;

public sealed class ErrorDto
{
    /// <summary>
    /// HTTP Error code
    /// </summary>
    /// <example>404</example>
    [Required]
    public int Code { get; init; }

    /// <summary>
    /// Message
    /// </summary>
    /// <example>No emergency telephon number found for facility 'Toronto'</example>
    [Required]
    public string? Message { get; init; }

    /// <summary>
    /// Complementary information
    /// </summary>
    /// <example>Facility 'Toronto' does not exist</example>
    [Required]
    public string? Info { get; init; }
}
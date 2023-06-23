using System.Collections.ObjectModel;

namespace Md2HtmlAPI.Model;

public interface IRecord
{
    /// <summary>
    /// Creation Date and Time of the record
    /// </summary>
    /// <example>2023-05-02T10:30:00Z</example>
    public DateTime? CreationDateTime { get; }

    /// <summary>
    /// Topic
    /// </summary>
    /// <example>18T_Press_Temperature</example>
    public string Topic { get; }

    /// <summary>
    /// Value
    /// </summary>
    /// <example>18°C</example>
    public string Value { get; }
}

public sealed class Record : IRecord
{
    /// <inheritdoc/>
    public DateTime? CreationDateTime { get; init; }

    /// <inheritdoc/>
    public string Topic { get; init; }

    /// <inheritdoc/>
    public string Value { get; init; }
}
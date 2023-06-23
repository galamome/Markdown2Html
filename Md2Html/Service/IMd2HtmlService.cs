using Md2HtmlAPI.Model;

namespace Md2HtmlAPI.Service;

public interface IMd2HtmlService
{
    /// <summary>
    /// Convert a 
    /// </summary>
    /// <returns></returns>
    public Task<string> ConvertAsync(IFormFile formFile);

    public Task<string> ConvertAsync(IFormFile formFile, string title);
}
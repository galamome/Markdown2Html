using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Md2HtmlAPI.Dto;
using Md2HtmlAPI.Service;
using Md2HtmlAPI.Model.DataAnnotations;
using System.IO.Compression;

namespace Md2HtmlAPI.Controllers;


[ApiController]
//[RequiredScope(RequiredScopesConfigurationKey = "AzureAd:scopes")]
[Route("[controller]")]
public class Md2HtmlController : ControllerBase
{
    private const string MimeType = "application/json";

    private readonly ILogger<Md2HtmlController> _logger;

    private readonly IMd2HtmlService _md2HtmlService;

    public Md2HtmlController(ILoggerFactory loggerFactory,
                IMd2HtmlService md2HtmlService)
    {
        _logger = loggerFactory.CreateLogger<Md2HtmlController>();
        _md2HtmlService = md2HtmlService;
    }

    [HttpPost("convertToHtml")]
    public async Task<ActionResult<string>> ConvertAsync([ZipFileExtensionsAttribute] IFormFile formFile)
    {
        try
        {
            var html = await _md2HtmlService.ConvertAsync(formFile);

            return html;
        }
        catch (InvalidDataException ide)
        {
            return BadRequest(new ErrorDto()
            {
                Code = StatusCodes.Status400BadRequest,
                Message = $"ZIP file is invalid",
                Info = $"Invalid data exception: '{ide.Message}'"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorDto()
                {
                    Code = StatusCodes.Status500InternalServerError,
                    Message = ex.Message,
                    Info = ex.InnerException?.ToString()
                });
        }
    }

    [HttpPost("convertToHtmlWithTitle")]
    public async Task<ActionResult<string>> ConvertWithTitleAsync([FromForm] ZipWithDataDto data)
    {
        try
        {
            var html = await _md2HtmlService.ConvertAsync(data.FormFile, data.Title);

            return html;
        }
        catch (InvalidDataException ide)
        {
            return BadRequest(new ErrorDto()
            {
                Code = StatusCodes.Status400BadRequest,
                Message = $"ZIP file is invalid",
                Info = $"Invalid data exception: '{ide.Message}'"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorDto()
                {
                    Code = StatusCodes.Status500InternalServerError,
                    Message = ex.Message,
                    Info = ex.InnerException?.ToString()
                });
        }
    }

}

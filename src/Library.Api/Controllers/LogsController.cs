using Library.Application.Common.Errors;
using Library.Application.Common.Logging;
using Library.Application.Common.Options;
using Library.Infrastructure.Logging;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

/// <summary>
/// Lets an operator/tester pull a specific day's log file for manual
/// inspection - e.g. "today's exception log" ->
/// GET /api/logs/download?category=exception-logs&amp;date=2026-09-03
/// downloads logs/exception-logs/exception-logs-03-09-2026.logs.
/// </summary>
[ApiController]
[Route("api/logs")]
public sealed class LogsController(
    ObservabilitySettings settings,
    IWebHostEnvironment environment) : ControllerBase
{
    private static readonly Dictionary<string, LogCategory> CategoriesBySlug =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["runtime-error-logs"] = LogCategory.RuntimeError,
            ["build-error-logs"] = LogCategory.BuildError,
            ["query-logs"] = LogCategory.Query,
            ["exception-logs"] = LogCategory.Exception
        };

    /// <summary>Lists the log categories and, optionally, the files available for one of them.</summary>
    /// <param name="category">Optional category slug to list files for (runtime-error-logs, build-error-logs, query-logs, exception-logs).</param>
    [HttpGet("available")]
    public IActionResult ListAvailable([FromQuery] string? category = null)
    {
        if (category is null)
        {
            return Ok(new { categories = CategoriesBySlug.Keys });
        }

        if (!CategoriesBySlug.TryGetValue(category, out var logCategory))
            return BadRequest(ApiErrorResponse.Single(
                new ApiError(
                    "UNSUPPORTED_LOG_CATEGORY",
                    $"Unsupported log category '{category}'.",
                    Field: nameof(category),
                    Required: true,
                    SupportedValues: string.Join(", ", CategoriesBySlug.Keys))));

        var directory = ResolveDirectory(logCategory);

        if (!Directory.Exists(directory))
            return Ok(new { category, files = Array.Empty<string>() });

        var files = Directory
            .GetFiles(directory)
            .Select(Path.GetFileName)
            .OrderDescending();

        return Ok(new { category, files });
    }

    /// <summary>
    /// Downloads the exact log file for a category/date so it can be
    /// inspected manually.
    /// </summary>
    /// <param name="category">runtime-error-logs | build-error-logs | query-logs | exception-logs</param>
    /// <param name="date">Date of the log file, format yyyy-MM-dd. Defaults to today (UTC).</param>
    [HttpGet("download")]
    public IActionResult Download(
        [FromQuery] string category,
        [FromQuery] string? date = null)
    {
        if (!settings.EnableLogDownloadEndpoint)
            return StatusCode(
                StatusCodes.Status403Forbidden,
                ApiErrorResponse.Single(new ApiError(
                    "FEATURE_DISABLED",
                    "Log download is disabled via FeatureFlags.EnableLogDownloadEndpoint.")));

        if (!CategoriesBySlug.TryGetValue(category, out var logCategory))
            return BadRequest(ApiErrorResponse.Single(
                new ApiError(
                    "UNSUPPORTED_LOG_CATEGORY",
                    $"Unsupported log category '{category}'.",
                    Field: nameof(category),
                    Required: true,
                    SupportedValues: string.Join(", ", CategoriesBySlug.Keys))));

        var targetDate = DateTime.UtcNow;

        if (date is not null &&
            !DateTime.TryParse(date, out targetDate))
        {
            return BadRequest(ApiErrorResponse.Single(
                new ApiError(
                    "INVALID_DATE",
                    $"'{date}' is not a valid date. Expected format: yyyy-MM-dd.",
                    Field: nameof(date),
                    Required: false)));
        }

        var (folder, filePrefix, extension) =
            FileAppLogWriter.GetFileTarget(logCategory);

        var fileName = $"{filePrefix}-{targetDate:dd-MM-yyyy}{extension}";
        var directory = ResolveDirectory(logCategory);
        var fullPath = Path.GetFullPath(Path.Combine(directory, fileName));

        // Path-traversal guard: the resolved path must still live
        // inside the resolved category directory.
        var resolvedDirectory = Path.GetFullPath(directory) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(resolvedDirectory, StringComparison.Ordinal))
            return BadRequest(ApiErrorResponse.Single(
                new ApiError("INVALID_PATH", "The resolved log path is invalid.")));

        if (!System.IO.File.Exists(fullPath))
            return NotFound(ApiErrorResponse.Single(
                new ApiError(
                    "LOG_FILE_NOT_FOUND",
                    $"No log file found for '{category}' on {targetDate:yyyy-MM-dd}.")));

        return PhysicalFile(fullPath, "text/plain", fileName);
    }

    private string ResolveDirectory(LogCategory category)
    {
        var (folder, _, _) = FileAppLogWriter.GetFileTarget(category);

        return Path.IsPathRooted(settings.LogsRootPath)
            ? Path.Combine(settings.LogsRootPath, folder)
            : Path.Combine(environment.ContentRootPath, settings.LogsRootPath, folder);
    }
}

using System.Text;
using System.Text.Json;
using Library.Application.Common.Logging;
using Library.Application.Common.Options;

namespace Library.Infrastructure.Logging;

/// <summary>
/// Writes each <see cref="AppLogEntry"/> as one JSON line into the
/// dated log file for its category:
///
///   logs/runtime-error-logs/runtime-error-logs-dd-MM-yyyy.txt
///   logs/build-error-logs/build-error-logs-dd-MM-yyyy.txt
///   logs/query-logs/query-logs-dd-MM-yyyy.txt
///   logs/exception-logs/exception-logs-dd-MM-yyyy.logs
///
/// JSON-lines (one compact JSON object per line) keeps the files both
/// human-readable (via any text editor / `tail -f`) and trivially
/// machine-parseable for future tooling (slow-query dashboards,
/// alerting, log shipping).
///
/// This writer NEVER throws: a logging failure must never take down
/// the request or job that triggered it. Failures fall back to
/// stderr so they are still visible in container/host logs.
/// </summary>
public sealed class FileAppLogWriter : IAppLogWriter
{
    private static readonly SemaphoreSlim WriteLock = new(1, 1);

    private readonly ObservabilitySettings _settings;
    private readonly string _contentRootPath;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false
    };

    public FileAppLogWriter(
        ObservabilitySettings settings,
        string contentRootPath)
    {
        _settings = settings;
        _contentRootPath = contentRootPath;
    }

    public async Task WriteAsync(
        AppLogEntry entry,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsEnabled(entry.Category))
                return;

            var (folder, fileName, extension) = GetFileTarget(entry.Category);

            var directory = Path.IsPathRooted(_settings.LogsRootPath)
                ? Path.Combine(_settings.LogsRootPath, folder)
                : Path.Combine(_contentRootPath, _settings.LogsRootPath, folder);

            Directory.CreateDirectory(directory);

            var fullFileName =
                $"{fileName}-{entry.TimestampUtc:dd-MM-yyyy}{extension}";
            var fullPath = Path.Combine(directory, fullFileName);

            var line = JsonSerializer.Serialize(entry, SerializerOptions);

            await WriteLock.WaitAsync(cancellationToken);
            try
            {
                await File.AppendAllTextAsync(
                    fullPath,
                    line + Environment.NewLine,
                    Encoding.UTF8,
                    cancellationToken);
            }
            finally
            {
                WriteLock.Release();
            }
        }
        catch (Exception ex)
        {
            // Logging must never crash the caller.
            await Console.Error.WriteLineAsync(
                $"[FileAppLogWriter] Failed to write {entry.Category} log entry: {ex.Message}");
        }
    }

    private bool IsEnabled(LogCategory category) => category switch
    {
        LogCategory.RuntimeError => _settings.EnableRuntimeErrorLogging,
        LogCategory.BuildError => _settings.EnableBuildErrorLogging,
        LogCategory.Query => _settings.EnableQueryLogging,
        LogCategory.Exception => _settings.EnableExceptionLogging,
        _ => true
    };

    /// <summary>
    /// Maps a category to its folder/file-prefix/extension. Kept in
    /// one place so <see cref="Api.Controllers.LogsController"/> (log
    /// download endpoint) can resolve the exact same file names.
    /// </summary>
    public static (string Folder, string FilePrefix, string Extension) GetFileTarget(
        LogCategory category) => category switch
    {
        LogCategory.RuntimeError => ("runtime-error-logs", "runtime-error-logs", ".txt"),
        LogCategory.BuildError => ("build-error-logs", "build-error-logs", ".txt"),
        LogCategory.Query => ("query-logs", "query-logs", ".txt"),
        LogCategory.Exception => ("exception-logs", "exception-logs", ".logs"),
        _ => throw new ArgumentOutOfRangeException(nameof(category))
    };
}

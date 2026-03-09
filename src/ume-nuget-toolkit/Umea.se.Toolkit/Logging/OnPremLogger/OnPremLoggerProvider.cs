using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Umea.se.Toolkit.Configuration;

namespace Umea.se.Toolkit.Logging.OnPremLogger;

/// <summary>
/// Creates an instance of <see cref="OnPremLogger"/>.
/// </summary>
[ProviderAlias("OnPrem")]
public class OnPremLoggerProvider : ILoggerProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApplicationConfigOnPremBase _config;
    private readonly ConcurrentDictionary<string, OnPremLogger> _loggers;
    private bool _disposed;

    public OnPremLoggerProvider(IHttpClientFactory httpClientFactory, ApplicationConfigOnPremBase config)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _loggers = new ConcurrentDictionary<string, OnPremLogger>();
    }

    public ILogger CreateLogger(string categoryName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _loggers.GetOrAdd(categoryName, new OnPremLogger(_httpClientFactory, _config, categoryName));
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _loggers.Clear();
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

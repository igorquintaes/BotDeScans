using Serilog;
using System.Diagnostics;

namespace BotDeScans.App.Infra.Logging;

public class HttpLoggingHandler(HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Log.Information("HTTP [{Method}] iniciado para: {Url}", request.Method, request.RequestUri);

        var sw = Stopwatch.StartNew();

        var response = await base.SendAsync(request, cancellationToken);

        sw.Stop();

        if (response.IsSuccessStatusCode)
        {
            Log.Information("HTTP [{Method}] sucesso ({StatusCode}) em {Elapsed}ms para: {Url}",
                request.Method, (int)response.StatusCode, sw.ElapsedMilliseconds, request.RequestUri);
        }
        else
        {
            Log.Warning("HTTP [{Method}] falha ({StatusCode}) em {Elapsed}ms para: {Url}",
                request.Method, (int)response.StatusCode, sw.ElapsedMilliseconds, request.RequestUri);
        }

        return response;
    }
}
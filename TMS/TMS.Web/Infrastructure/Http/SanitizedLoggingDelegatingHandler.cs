using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TMS.Web.Infrastructure.Http
{
    public class SanitizedLoggingDelegatingHandler : DelegatingHandler
    {
        private readonly ILogger<SanitizedLoggingDelegatingHandler> _logger;

        public SanitizedLoggingDelegatingHandler(ILogger<SanitizedLoggingDelegatingHandler> logger)
        {
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var correlationId = request.Headers.Contains(CorrelationIdDelegatingHandler.HeaderName)
                ? request.Headers.GetValues(CorrelationIdDelegatingHandler.HeaderName).FirstOrDefault()
                : "[none]";

            var sanitizedUrl = IntegrationUrlSanitizer.SanitizeUri(request.RequestUri);
            var sw = Stopwatch.StartNew();

            _logger.LogInformation("[OUTBOUND HTTP START] {Method} {Url} [CorrelationId: {CorrelationId}]",
                request.Method.Method, sanitizedUrl, correlationId);

            HttpResponseMessage response;
            try
            {
                response = await base.SendAsync(request, cancellationToken);
                sw.Stop();

                var level = (int)response.StatusCode >= 500 ? LogLevel.Error :
                            (int)response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

                _logger.Log(level, "[OUTBOUND HTTP END] {Method} {Url} -> {StatusCode} in {ElapsedMs}ms [CorrelationId: {CorrelationId}]",
                    request.Method.Method, sanitizedUrl, (int)response.StatusCode, sw.ElapsedMilliseconds, correlationId);

                return response;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "[OUTBOUND HTTP FAILED] {Method} {Url} -> Exception: {Message} in {ElapsedMs}ms [CorrelationId: {CorrelationId}]",
                    request.Method.Method, sanitizedUrl, ex.Message, sw.ElapsedMilliseconds, correlationId);
                throw;
            }
        }
    }
}

using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace TMS.Web.Infrastructure.Http
{
    public class CorrelationIdDelegatingHandler : DelegatingHandler
    {
        public const string HeaderName = "X-Correlation-ID";
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!request.Headers.Contains(HeaderName))
            {
                var correlationId = ResolveCurrentCorrelationId();
                request.Headers.TryAddWithoutValidation(HeaderName, correlationId);
            }

            return await base.SendAsync(request, cancellationToken);
        }

        private string ResolveCurrentCorrelationId()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    if (httpContext.Request.Headers.TryGetValue(HeaderName, out var headerVal) && !string.IsNullOrWhiteSpace(headerVal))
                    {
                        return headerVal.ToString().Trim();
                    }

                    if (!string.IsNullOrWhiteSpace(httpContext.TraceIdentifier))
                    {
                        return httpContext.TraceIdentifier;
                    }
                }
            }
            catch
            {
                // Fall back gracefully to GUID
            }

            return Guid.NewGuid().ToString("N");
        }
    }
}

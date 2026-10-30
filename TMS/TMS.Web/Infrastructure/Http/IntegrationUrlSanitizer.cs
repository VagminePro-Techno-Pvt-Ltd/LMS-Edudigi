using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TMS.Web.Infrastructure.Http
{
    public static class IntegrationUrlSanitizer
    {
        private static readonly HashSet<string> SensitiveQueryKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "key", "apikey", "api_key", "token", "access_token", "jwt",
            "pwd", "password", "secret", "client_secret", "sig", "signature",
            "auth", "authorization", "code"
        };

        private static readonly Regex QueryParamRegex = new(
            @"(?<=[?&])(?<key>[a-zA-Z0-9_\-]+)=(?<val>[^&#\s]*)",
            RegexOptions.Compiled);

        public static string SanitizeUrl(string? rawUrl)
        {
            if (string.IsNullOrWhiteSpace(rawUrl)) return string.Empty;

            try
            {
                if (!rawUrl.Contains('?') && !rawUrl.Contains('&'))
                {
                    return rawUrl;
                }

                return QueryParamRegex.Replace(rawUrl, match =>
                {
                    var key = match.Groups["key"].Value;
                    if (SensitiveQueryKeys.Contains(key))
                    {
                        return $"{key}=[REDACTED]";
                    }
                    return match.Value;
                });
            }
            catch
            {
                return "[REDACTED_URL]";
            }
        }

        public static string SanitizeUri(Uri? uri)
        {
            if (uri == null) return string.Empty;
            return SanitizeUrl(uri.ToString());
        }

        public static string MaskSecret(string? secret, int unmaskedPrefixLength = 3, int unmaskedSuffixLength = 3)
        {
            if (string.IsNullOrWhiteSpace(secret)) return "[NOT CONFIGURED]";
            var trimmed = secret.Trim();
            if (trimmed.Length <= (unmaskedPrefixLength + unmaskedSuffixLength + 2))
            {
                return "••••••••";
            }
            var prefix = trimmed.Substring(0, unmaskedPrefixLength);
            var suffix = trimmed.Substring(trimmed.Length - unmaskedSuffixLength);
            return $"{prefix}••••••••{suffix}";
        }
    }
}

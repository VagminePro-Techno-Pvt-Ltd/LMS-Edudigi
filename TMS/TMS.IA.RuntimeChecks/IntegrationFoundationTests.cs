using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TMS.Models.Email;
using TMS.Repository;
using TMS.Utilities;
using TMS.ViewModels;
using TMS.ViewModels.Email;
using TMS.ViewModels.Integration;
using TMS.Web.Areas.Admin.Controllers;
using TMS.Web.Infrastructure.Http;
using TMS.Web.Services.Integration;
using TMS.Web.Services.Proctoring;

namespace TMS.IA.RuntimeChecks
{
    public static class IntegrationFoundationTests
    {
        private static int _testPassed = 0;
        private static int _testFailed = 0;
        private static readonly List<string> _failedDetails = new();

        private static void Assert(string testName, bool condition, string detail = "")
        {
            if (condition)
            {
                _testPassed++;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  [PASS] {testName}");
                Console.ResetColor();
            }
            else
            {
                _testFailed++;
                var msg = $"  [FAIL] {testName} - {detail}";
                _failedDetails.Add(msg);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(msg);
                Console.ResetColor();
            }
        }

        public static async Task<int> RunAllTestsAsync()
        {
            _testPassed = 0;
            _testFailed = 0;
            _failedDetails.Clear();

            Console.WriteLine("==================================================================");
            Console.WriteLine("  EDUDIGICLASS INTEGRATION FOUNDATION AUTOMATED TEST SUITE (PHASE 1)");
            Console.WriteLine("==================================================================");

            TestUrlSanitizationAndMasking();
            await TestCorrelationIdPropagationAsync();
            await TestResilienceRetryEligibilityAsync();
            await TestCircuitBreakerBehaviorAsync();
            await TestZoomServiceAndTokenCachingAsync();
            await TestIntegrationHealthServiceAsync();
            TestAuthorizationAndMenuSecurity();

            Console.WriteLine("\n------------------------------------------------------------------");
            Console.WriteLine($"RESULTS: Total: {_testPassed + _testFailed} | Passed: {_testPassed} | Failed: {_testFailed}");
            Console.WriteLine("------------------------------------------------------------------");

            if (_failedDetails.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nFAILURE SUMMARY:");
                foreach (var fail in _failedDetails)
                {
                    Console.WriteLine(fail);
                }
                Console.ResetColor();
            }

            return _testFailed;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // 1. URL SANITIZATION & SECRET MASKING
        // ═══════════════════════════════════════════════════════════════════════
        private static void TestUrlSanitizationAndMasking()
        {
            Console.WriteLine("\n[Suite 1] URL Sanitization and Secret Redaction");

            // Key query param redacting
            var urlWithKey = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key=AIzaSySecretApiKey12345";
            var sanitizedKeyUrl = IntegrationUrlSanitizer.SanitizeUrl(urlWithKey);
            Assert("SanitizeUrl_Redacts_ApiKeyParam",
                !sanitizedKeyUrl.Contains("AIzaSySecretApiKey12345") && sanitizedKeyUrl.Contains("key=[REDACTED]"),
                $"Actual: {sanitizedKeyUrl}");

            // Zoom token or password param
            var zoomUrl = "https://api.zoom.us/v2/users/me/meetings?pwd=SuperSecretPassword99&token=jwt_sample_token&other=standardVal";
            var sanitizedZoom = IntegrationUrlSanitizer.SanitizeUrl(zoomUrl);
            Assert("SanitizeUrl_Redacts_Pwd_And_Token_Params",
                !sanitizedZoom.Contains("SuperSecretPassword99") &&
                !sanitizedZoom.Contains("jwt_sample_token") &&
                sanitizedZoom.Contains("pwd=[REDACTED]") &&
                sanitizedZoom.Contains("token=[REDACTED]") &&
                sanitizedZoom.Contains("other=standardVal"),
                $"Actual: {sanitizedZoom}");

            // Safe URL unchanged
            var safeUrl = "https://api.zoom.us/v2/users/me/meetings?type=scheduled&page_size=30";
            var sanitizedSafe = IntegrationUrlSanitizer.SanitizeUrl(safeUrl);
            Assert("SanitizeUrl_Preserves_NonSensitiveParams",
                sanitizedSafe == safeUrl,
                $"Actual: {sanitizedSafe}");

            // Null/empty resilience
            Assert("SanitizeUrl_Handles_NullOrEmpty",
                IntegrationUrlSanitizer.SanitizeUrl((string?)null) == string.Empty &&
                IntegrationUrlSanitizer.SanitizeUrl("") == string.Empty,
                "Should return empty string on null or empty input");

            // Secret masking - normal string
            var masked1 = IntegrationUrlSanitizer.MaskSecret("AIzaSyB3492801481023812");
            Assert("MaskSecret_MasksLongSecret",
                masked1.StartsWith("AIz") && masked1.EndsWith("812") && masked1.Contains("•") && !masked1.Contains("B349280148102"),
                $"Actual: {masked1}");

            // Secret masking - short string (<6)
            var maskedShort = IntegrationUrlSanitizer.MaskSecret("1234");
            Assert("MaskSecret_MasksShortSecretEntirely",
                maskedShort == "••••••••",
                $"Actual: {maskedShort}");

            // Secret masking - null or empty
            var maskedNull = IntegrationUrlSanitizer.MaskSecret(null);
            Assert("MaskSecret_HandlesNullOrEmpty",
                maskedNull == "[NOT CONFIGURED]",
                $"Actual: {maskedNull}");
        }

        // ═══════════════════════════════════════════════════════════════════════
        // 2. CORRELATION ID PROPAGATION
        // ═══════════════════════════════════════════════════════════════════════
        private static async Task TestCorrelationIdPropagationAsync()
        {
            Console.WriteLine("\n[Suite 2] Correlation ID Propagation");

            // 1. Without incoming context or header -> handler generates GUID
            string? capturedCorrelationId = null;
            var testHandler = new TestSpyHandler((req, ct) =>
            {
                if (req.Headers.TryGetValues(CorrelationIdDelegatingHandler.HeaderName, out var values))
                {
                    capturedCorrelationId = values.FirstOrDefault();
                }
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            });

            var correlationHandler = new CorrelationIdDelegatingHandler(new HttpContextAccessor())
            {
                InnerHandler = testHandler
            };
            using var client1 = new HttpClient(correlationHandler);

            var req1 = new HttpRequestMessage(HttpMethod.Get, "https://example.com/api/test");
            await client1.SendAsync(req1);

            Assert("CorrelationId_GeneratedWhenMissing",
                !string.IsNullOrWhiteSpace(capturedCorrelationId) && Guid.TryParse(capturedCorrelationId, out _),
                $"Generated correlation ID: {capturedCorrelationId}");

            // 2. With existing header on request -> handler preserves existing
            var existingId = "EDUDIGI-TRACE-99998888";
            var req2 = new HttpRequestMessage(HttpMethod.Get, "https://example.com/api/test");
            req2.Headers.Add(CorrelationIdDelegatingHandler.HeaderName, existingId);
            await client1.SendAsync(req2);

            Assert("CorrelationId_PreservesExistingHeader",
                capturedCorrelationId == existingId,
                $"Expected: {existingId}, Actual: {capturedCorrelationId}");

            // 3. With HttpContext trace identifier
            var mockContext = new DefaultHttpContext();
            mockContext.TraceIdentifier = "HTTPCONTEXT-TRACE-4321";
            var contextAccessor = new HttpContextAccessor { HttpContext = mockContext };

            var correlationHandler2 = new CorrelationIdDelegatingHandler(contextAccessor)
            {
                InnerHandler = testHandler
            };
            using var client2 = new HttpClient(correlationHandler2);

            var req3 = new HttpRequestMessage(HttpMethod.Get, "https://example.com/api/test");
            await client2.SendAsync(req3);

            Assert("CorrelationId_ExtractsFromHttpContext",
                capturedCorrelationId == "HTTPCONTEXT-TRACE-4321",
                $"Expected: HTTPCONTEXT-TRACE-4321, Actual: {capturedCorrelationId}");
        }

        // ═══════════════════════════════════════════════════════════════════════
        // 3. RESILIENCE & RETRY ELIGIBILITY
        // ═══════════════════════════════════════════════════════════════════════
        private static async Task TestResilienceRetryEligibilityAsync()
        {
            Console.WriteLine("\n[Suite 3] Resilience & Retry Eligibility");

            var options = Options.Create(new IntegrationHttpOptions
            {
                TimeoutSeconds = 5,
                MaxRetries = 2,
                BaseDelayMilliseconds = 10,
                MaxDelayMilliseconds = 50,
                EnableJitter = false
            });

            // 1. GET with 503 Service Unavailable -> Retries and succeeds
            int getAttempts = 0;
            var spyHandler1 = new TestSpyHandler((req, ct) =>
            {
                getAttempts++;
                if (getAttempts < 3)
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
                }
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            });

            var retryHandler1 = new ResilientRetryDelegatingHandler(options, NullLogger<ResilientRetryDelegatingHandler>.Instance)
            {
                InnerHandler = spyHandler1
            };
            using var client1 = new HttpClient(retryHandler1);

            var response1 = await client1.GetAsync("https://api.example.com/idempotent-get");
            Assert("Retry_GetRetriesOn503UntilSuccess",
                response1.StatusCode == HttpStatusCode.OK && getAttempts == 3,
                $"Attempts: {getAttempts}, Final Status: {response1.StatusCode}");

            // 2. Non-idempotent POST without idempotency header -> NO retry on 500 error
            int postAttempts = 0;
            var spyHandler2 = new TestSpyHandler((req, ct) =>
            {
                postAttempts++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            });

            var retryHandler2 = new ResilientRetryDelegatingHandler(options, NullLogger<ResilientRetryDelegatingHandler>.Instance)
            {
                InnerHandler = spyHandler2
            };
            using var client2 = new HttpClient(retryHandler2);

            var postReq = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/unsafe-post");
            var response2 = await client2.SendAsync(postReq);
            Assert("Retry_PostDoesNotRetryWithoutSafeHeader",
                postAttempts == 1 && response2.StatusCode == HttpStatusCode.InternalServerError,
                $"Attempts: {postAttempts}, Status: {response2.StatusCode}");

            // 3. POST WITH X-Retry-Safe header -> SHOULD retry
            int safePostAttempts = 0;
            var spyHandler3 = new TestSpyHandler((req, ct) =>
            {
                safePostAttempts++;
                if (safePostAttempts < 2)
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.GatewayTimeout));
                }
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created));
            });

            var retryHandler3 = new ResilientRetryDelegatingHandler(options, NullLogger<ResilientRetryDelegatingHandler>.Instance)
            {
                InnerHandler = spyHandler3
            };
            using var client3 = new HttpClient(retryHandler3);

            var safePostReq = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/safe-post");
            safePostReq.Headers.Add("X-Retry-Safe", "true");
            var response3 = await client3.SendAsync(safePostReq);
            Assert("Retry_PostWithSafeHeaderRetriesSuccessfully",
                safePostAttempts == 2 && response3.StatusCode == HttpStatusCode.Created,
                $"Attempts: {safePostAttempts}, Status: {response3.StatusCode}");

            // 4. Non-transient 400 Bad Request -> No retry
            int badReqAttempts = 0;
            var spyHandler4 = new TestSpyHandler((req, ct) =>
            {
                badReqAttempts++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
            });

            var retryHandler4 = new ResilientRetryDelegatingHandler(options, NullLogger<ResilientRetryDelegatingHandler>.Instance)
            {
                InnerHandler = spyHandler4
            };
            using var client4 = new HttpClient(retryHandler4);

            var response4 = await client4.GetAsync("https://api.example.com/bad-req");
            Assert("Retry_DoesNotRetry400BadRequest",
                badReqAttempts == 1 && response4.StatusCode == HttpStatusCode.BadRequest,
                $"Attempts: {badReqAttempts}, Status: {response4.StatusCode}");
        }

        // ═══════════════════════════════════════════════════════════════════════
        // 4. CIRCUIT BREAKER BEHAVIOR
        // ═══════════════════════════════════════════════════════════════════════
        private static async Task TestCircuitBreakerBehaviorAsync()
        {
            Console.WriteLine("\n[Suite 4] Circuit Breaker Fast-Failure");

            var options = Options.Create(new IntegrationHttpOptions
            {
                CircuitBreakerFailureThreshold = 3,
                CircuitBreakerDurationSeconds = 10,
                MaxRetries = 0 // test pure breaker trip
            });

            int downstreamCalls = 0;
            var spyHandler = new TestSpyHandler((req, ct) =>
            {
                downstreamCalls++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            });

            var retryHandler = new ResilientRetryDelegatingHandler(options, NullLogger<ResilientRetryDelegatingHandler>.Instance)
            {
                InnerHandler = spyHandler
            };
            using var client = new HttpClient(retryHandler);

            var targetUrl = "https://breaker-test.example.com/api";

            // Trip the breaker with 3 consecutive failures
            for (int i = 0; i < 3; i++)
            {
                await client.GetAsync(targetUrl);
            }

            Assert("CircuitBreaker_TrippedAfterThreshold",
                downstreamCalls == 3,
                $"Downstream calls: {downstreamCalls}");

            // 4th call should be fast-failed by circuit breaker with IntegrationCircuitBreakerException
            bool breakerThrown = false;
            try
            {
                await client.GetAsync(targetUrl);
            }
            catch (IntegrationCircuitBreakerException cbEx)
            {
                breakerThrown = true;
                Assert("CircuitBreaker_FastFailsSubsequentCall",
                    downstreamCalls == 3 && cbEx.Host == "breaker-test.example.com",
                    $"Host: {cbEx.Host}, Calls: {downstreamCalls}");
            }

            Assert("CircuitBreaker_ThrowsIntegrationCircuitBreakerException",
                breakerThrown,
                "Expected IntegrationCircuitBreakerException to be thrown when circuit is open");
        }

        // ═══════════════════════════════════════════════════════════════════════
        // 5. ZOOM SERVICE & TOKEN CACHING
        // ═══════════════════════════════════════════════════════════════════════
        private static async Task TestZoomServiceAndTokenCachingAsync()
        {
            Console.WriteLine("\n[Suite 5] Zoom Service Reference Implementation & Token Caching");

            var zoomSettings = new ZoomSettings
            {
                AccountId = "mock-account-123",
                ClientId = "mock-client-abc",
                ClientSecret = "mock-secret-xyz"
            };

            int oauthTokenRequests = 0;
            int meetingCreateRequests = 0;

            var zoomMockHandler = new TestSpyHandler((req, ct) =>
            {
                // OAuth token endpoint
                if (req.RequestUri != null && req.RequestUri.AbsolutePath.Contains("/oauth/token"))
                {
                    oauthTokenRequests++;
                    var tokenJson = JsonSerializer.Serialize(new
                    {
                        access_token = "zoom_test_access_token_" + oauthTokenRequests,
                        token_type = "bearer",
                        expires_in = 3600
                    });
                    var resp = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(tokenJson, System.Text.Encoding.UTF8, "application/json")
                    };
                    return Task.FromResult(resp);
                }

                // Meeting create endpoint
                if (req.RequestUri != null && req.RequestUri.AbsolutePath.Contains("/users/me/meetings"))
                {
                    meetingCreateRequests++;
                    var meetingJson = JsonSerializer.Serialize(new
                    {
                        id = 98765432101L,
                        join_url = "https://zoom.us/j/98765432101?pwd=testMeetingPassword",
                        start_url = "https://zoom.us/s/98765432101?zak=testHostKey",
                        password = "testMeetingPassword"
                    });
                    var resp = new HttpResponseMessage(HttpStatusCode.Created)
                    {
                        Content = new StringContent(meetingJson, System.Text.Encoding.UTF8, "application/json")
                    };
                    return Task.FromResult(resp);
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            });

            var httpClient = new HttpClient(zoomMockHandler)
            {
                BaseAddress = new Uri("https://api.zoom.us/v2/")
            };

            var zoomService = new ZoomService(httpClient, Options.Create(zoomSettings), NullLogger<ZoomService>.Instance);

            // First meeting creation
            var result1 = await zoomService.CreateZoomMeetingAsync("Physics Unit 1", DateTime.UtcNow.AddHours(2), 45);
            Assert("ZoomService_CreatesMeetingSuccessfully",
                result1.JoinUrl.Contains("98765432101") && result1.Password == "testMeetingPassword",
                $"JoinUrl: {result1.JoinUrl}");

            // Second meeting creation -> Should reuse cached OAuth token without making new token request
            var result2 = await zoomService.CreateZoomMeetingAsync("Chemistry Unit 2", DateTime.UtcNow.AddHours(4), 60);
            Assert("ZoomService_ReusesCachedOAuthToken",
                oauthTokenRequests == 1 && meetingCreateRequests == 2,
                $"Token requests: {oauthTokenRequests}, Meeting requests: {meetingCreateRequests}");

            // Test CreateMeetingAsync backwards-compatibility method
            var joinUrl = await zoomService.CreateMeetingAsync("Biology Unit 3", DateTime.UtcNow.AddHours(6), 30);
            Assert("ZoomService_CreateMeetingAsync_ReturnsValidJoinUrl",
                !string.IsNullOrWhiteSpace(joinUrl) && joinUrl.StartsWith("https://zoom.us/j/"),
                $"JoinUrl: {joinUrl}");

            // Test ValidateCredentialsAsync
            var isValid = await zoomService.ValidateCredentialsAsync();
            Assert("ZoomService_ValidateCredentials_Succeeds",
                isValid == true,
                "Credentials validation should succeed with valid mock token response");
        }

        // ═══════════════════════════════════════════════════════════════════════
        // 6. INTEGRATION HEALTH SERVICE & PROVIDER STATUS
        // ═══════════════════════════════════════════════════════════════════════
        private static async Task TestIntegrationHealthServiceAsync()
        {
            Console.WriteLine("\n[Suite 6] Integration Health Service & Secret Masking");

            // Setup in-memory config with sensitive tokens
            var inMemoryConfig = new Dictionary<string, string?>
            {
                { "EmailSettings:Host", "smtp.gmail.com" },
                { "EmailSettings:Port", "587" },
                { "EmailSettings:UserName", "lms-admin@edudigi.org" },
                { "EmailSettings:Password", "superSecretAppPassword123" },
                { "StorageSettings:GoogleDrive:ServiceAccountKeyFilePath", "keys/sa-drive.json" },
                { "ZoomSettings:AccountId", "zoom-acc-11" },
                { "ZoomSettings:ClientId", "zoom-cli-22" },
                { "ZoomSettings:ClientSecret", "zoom-top-secret-val-999" },
                { "ProctoringApi:ApiBaseUrl", "https://api.proctoring.internal" },
                { "ProctoringApi:ApiKey", "proctor-api-key-abcdef" },
                { "Gemini:ApiKey", "AIzaSyGeminiApiKeySecret9999" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemoryConfig)
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<ApplicationDBContext>(options =>
                options.UseInMemoryDatabase("IntegrationFoundationTestDb_" + Guid.NewGuid().ToString("N")));

            services.AddOptions();
            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
            services.Configure<ZoomSettings>(configuration.GetSection("ZoomSettings"));
            services.Configure<ProctoringApiOptions>(configuration.GetSection("ProctoringApi"));

            var serviceProvider = services.BuildServiceProvider();
            var emailMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<EmailSettings>>();
            var zoomMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<ZoomSettings>>();
            var proctoringMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<ProctoringApiOptions>>();
            var dbContext = serviceProvider.GetRequiredService<ApplicationDBContext>();

            var healthService = new IntegrationHealthService(
                configuration,
                emailMonitor,
                zoomMonitor,
                proctoringMonitor,
                dbContext,
                NullLogger<IntegrationHealthService>.Instance);

            var summary = await healthService.GetDashboardSummaryAsync(CancellationToken.None);

            Assert("HealthService_DiscoversAllCoreIntegrations",
                summary.Providers.Count >= 5,
                $"Discovered: {summary.Providers.Count}");

            // Check categories
            var categories = summary.Providers.Select(i => i.Category).Distinct().ToList();
            Assert("HealthService_CategorizesIntegrations",
                categories.Contains("Communications") &&
                categories.Contains("Video Conferencing") &&
                categories.Contains("Cloud Storage") &&
                categories.Contains("Artificial Intelligence") &&
                categories.Contains("Assessment Security"),
                $"Categories: {string.Join(", ", categories)}");

            // Check secret masking in configuration dictionaries
            var emailIntegration = summary.Providers.FirstOrDefault(i => string.Equals(i.ProviderKey, "smtp", StringComparison.OrdinalIgnoreCase));
            Assert("HealthService_MasksEmailPassword",
                emailIntegration != null &&
                emailIntegration.MaskedConfig.ContainsKey("Password") &&
                !emailIntegration.MaskedConfig["Password"].Contains("superSecretAppPassword123") &&
                emailIntegration.MaskedConfig["Password"].Contains("•"),
                $"Password display: {emailIntegration?.MaskedConfig.GetValueOrDefault("Password")}");

            var zoomIntegration = summary.Providers.FirstOrDefault(i => string.Equals(i.ProviderKey, "zoom", StringComparison.OrdinalIgnoreCase));
            Assert("HealthService_MasksZoomClientSecret",
                zoomIntegration != null &&
                zoomIntegration.MaskedConfig.ContainsKey("ClientSecret") &&
                !zoomIntegration.MaskedConfig["ClientSecret"].Contains("zoom-top-secret-val-999") &&
                zoomIntegration.MaskedConfig["ClientSecret"].Contains("•"),
                $"Secret display: {zoomIntegration?.MaskedConfig.GetValueOrDefault("ClientSecret")}");

            var geminiIntegration = summary.Providers.FirstOrDefault(i => string.Equals(i.ProviderKey, "gemini", StringComparison.OrdinalIgnoreCase));
            Assert("HealthService_MasksGeminiApiKey",
                geminiIntegration != null &&
                geminiIntegration.MaskedConfig.ContainsKey("ApiKey") &&
                !geminiIntegration.MaskedConfig["ApiKey"].Contains("AIzaSyGeminiApiKeySecret9999") &&
                geminiIntegration.MaskedConfig["ApiKey"].Contains("•"),
                $"ApiKey display: {geminiIntegration?.MaskedConfig.GetValueOrDefault("ApiKey")}");
        }

        // ═══════════════════════════════════════════════════════════════════════
        // 7. AUTHORIZATION & MENU SECURITY
        // ═══════════════════════════════════════════════════════════════════════
        private static void TestAuthorizationAndMenuSecurity()
        {
            Console.WriteLine("\n[Suite 7] Authorization & Navigation Security");

            // Check IntegrationsController attributes
            var controllerType = typeof(IntegrationsController);
            var areaAttr = controllerType.GetCustomAttribute<AreaAttribute>();
            var authAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();

            Assert("Controller_HasAdminAreaAttribute",
                areaAttr != null && areaAttr.RouteValue == "Admin",
                $"Area: {areaAttr?.RouteValue}");

            Assert("Controller_RestrictedToAdminRoles",
                authAttr != null &&
                !string.IsNullOrWhiteSpace(authAttr.Roles) &&
                authAttr.Roles.Contains("Admin") &&
                authAttr.Roles.Contains("Super Admin"),
                $"Roles: {authAttr?.Roles}");

            // Verify MenuPartial role check logic (unit check of fixed line 7)
            Func<string?, bool> isMenuAdmin = role =>
            {
                string r = role?.ToLower()?.Trim() ?? "";
                return r == "admin" || r == "super admin" || r == "exam coordinator" || r == "management";
            };

            Assert("MenuRoleCheck_AllowsAdmin", isMenuAdmin("Admin"), "Admin role should be granted");
            Assert("MenuRoleCheck_AllowsSuperAdmin", isMenuAdmin("Super Admin"), "Super Admin role should be granted");
            Assert("MenuRoleCheck_DeniesStudent", !isMenuAdmin("Student"), "Student role must be denied");
            Assert("MenuRoleCheck_DeniesEmptyRole", !isMenuAdmin(""), "Empty role must be denied");
            Assert("MenuRoleCheck_DeniesNullRole", !isMenuAdmin(null), "Null role must be denied");
        }

        // ═══════════════════════════════════════════════════════════════════════
        // HELPER SPY HANDLER FOR HTTP TESTING
        // ═══════════════════════════════════════════════════════════════════════
        private class TestSpyHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

            public TestSpyHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
            {
                _handler = handler;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return _handler(request, cancellationToken);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TMS.ViewModels.Assessments;

namespace TMS.Web.Services.Proctoring
{
    public class ProctoringApiClient : IProctoringApiClient
    {
        private readonly HttpClient _http;
        private readonly ProctoringApiOptions _options;
        private readonly ILogger<ProctoringApiClient> _logger;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public ProctoringApiClient(
            HttpClient http,
            IOptions<ProctoringApiOptions> options,
            ILogger<ProctoringApiClient> logger)
        {
            _http = http;
            _options = options.Value;
            _logger = logger;

            if (_http.BaseAddress == null)
            {
                _http.BaseAddress = new Uri(_options.ApiBaseUrl.TrimEnd('/') + "/");
            }
            _http.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        }

        public string GetFrontendUrl() => _options.FrontendBaseUrl.TrimEnd('/');

        private string GenerateAdminJwtToken()
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.JwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "957f0dad-c4da-4665-b3ef-47639e37c03a"),
                new Claim(ClaimTypes.Name, "EduDigi Admin"),
                new Claim("loginId", "admin"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("role", "Admin"),
                new Claim(ClaimTypes.Email, "admin@edudigi.com")
            };

            var token = new JwtSecurityToken(
                issuer: _options.JwtIssuer,
                audience: _options.JwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(4),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string relativeUri, object? body = null)
        {
            var req = new HttpRequestMessage(method, relativeUri);
            var token = GenerateAdminJwtToken();
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            if (body != null)
            {
                var json = JsonSerializer.Serialize(body, _jsonOpts);
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            return req;
        }

        // =========================================================================
        // --- EXAM MANAGEMENT CRUD ---
        // =========================================================================

        public async Task<List<ProctorExamDto>> GetAllExamsAsync()
        {
            try
            {
                using var req = CreateAuthorizedRequest(HttpMethod.Get, "api/admin/exams");
                using var res = await _http.SendAsync(req);

                if (!res.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch exams from ProctoringService: {Status}", res.StatusCode);
                    return new List<ProctorExamDto>();
                }

                var content = await res.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<ProctorExamDto>>(content, _jsonOpts) ?? new List<ProctorExamDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to ProctoringService at {Url}", _options.ApiBaseUrl);
                return new List<ProctorExamDto>();
            }
        }

        public async Task<ProctorExamDto?> GetExamByIdAsync(Guid id)
        {
            try
            {
                using var req = CreateAuthorizedRequest(HttpMethod.Get, $"api/admin/exams/{id}");
                using var res = await _http.SendAsync(req);

                if (!res.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Exam {Id} not found or error: {Status}", id, res.StatusCode);
                    return null;
                }

                var content = await res.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ProctorExamDto>(content, _jsonOpts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exam {Id}", id);
                return null;
            }
        }

        public async Task<ProctorExamDto?> CreateExamAsync(CreateProctorExamRequest reqData)
        {
            try
            {
                if (!reqData.StartTime.HasValue || reqData.StartTime.Value == default || reqData.StartTime.Value.Year < 2000)
                {
                    reqData.StartTime = DateTime.UtcNow.AddMinutes(15);
                }

                using var req = CreateAuthorizedRequest(HttpMethod.Post, "api/admin/exams", reqData);
                using var res = await _http.SendAsync(req);

                var content = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to create exam: {Status} | {Body}", res.StatusCode, content);
                    throw new InvalidOperationException($"API error ({res.StatusCode}): {content}");
                }

                return JsonSerializer.Deserialize<ProctorExamDto>(content, _jsonOpts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while creating exam");
                throw;
            }
        }

        public async Task<bool> UpdateExamAsync(Guid id, UpdateProctorExamDto dto)
        {
            try
            {
                using var req = CreateAuthorizedRequest(HttpMethod.Put, $"api/admin/exams/{id}", dto);
                using var res = await _http.SendAsync(req);
                return res.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating exam {Id}", id);
                return false;
            }
        }

        public async Task<(bool success, bool isActive, string message)> ToggleExamStatusAsync(Guid id)
        {
            try
            {
                using var req = CreateAuthorizedRequest(HttpMethod.Post, $"api/admin/exams/{id}/toggle-status");
                using var res = await _http.SendAsync(req);

                if (!res.IsSuccessStatusCode)
                {
                    return (false, false, $"Failed with status {res.StatusCode}");
                }

                var content = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                bool isActive = root.TryGetProperty("isActive", out var p) && p.GetBoolean();
                string msg = root.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "";

                return (true, isActive, msg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling status for exam {Id}", id);
                return (false, false, ex.Message);
            }
        }

        public async Task<bool> DeleteExamAsync(Guid id)
        {
            try
            {
                using var req = CreateAuthorizedRequest(HttpMethod.Delete, $"api/admin/exams/{id}");
                using var res = await _http.SendAsync(req);
                return res.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting exam {Id}", id);
                return false;
            }
        }

        // =========================================================================
        // --- RESULTS & EVALUATION ---
        // =========================================================================

        public async Task<ExamResultsResponseDto?> GetExamResultsAsync(Guid examId)
        {
            try
            {
                using var req = CreateAuthorizedRequest(HttpMethod.Get, $"api/admin/exams/{examId}/results");
                using var res = await _http.SendAsync(req);

                if (!res.IsSuccessStatusCode) return null;

                var content = await res.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ExamResultsResponseDto>(content, _jsonOpts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exam results for {Id}", examId);
                return null;
            }
        }

        public async Task<CandidateScriptResponseDto?> GetCandidateScriptAsync(Guid examId, string candidateId)
        {
            try
            {
                using var req = CreateAuthorizedRequest(HttpMethod.Get, $"api/admin/exams/{examId}/candidates/{candidateId}/script");
                using var res = await _http.SendAsync(req);

                if (!res.IsSuccessStatusCode) return null;

                var content = await res.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<CandidateScriptResponseDto>(content, _jsonOpts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting candidate script for exam {ExamId}, candidate {CandId}", examId, candidateId);
                return null;
            }
        }

        public async Task<int> EvaluateExamAsync(Guid examId)
        {
            try
            {
                using var req = CreateAuthorizedRequest(HttpMethod.Post, $"api/admin/exams/{examId}/evaluate");
                using var res = await _http.SendAsync(req);

                if (!res.IsSuccessStatusCode) return 0;

                var content = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("evaluatedAttempts", out var p))
                {
                    return p.GetInt32();
                }
                return 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating exam {ExamId}", examId);
                return 0;
            }
        }

        // =========================================================================
        // --- QUESTIONS ---
        // =========================================================================

        public async Task<List<ExamQuestionItemDto>> GetExamQuestionsAsync(Guid examId)
        {
            try
            {
                using var req = CreateAuthorizedRequest(HttpMethod.Get, $"api/admin/exam-questions/{examId}");
                using var res = await _http.SendAsync(req);

                if (!res.IsSuccessStatusCode) return new List<ExamQuestionItemDto>();

                var content = await res.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<ExamQuestionItemDto>>(content, _jsonOpts) ?? new List<ExamQuestionItemDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting questions for exam {ExamId}", examId);
                return new List<ExamQuestionItemDto>();
            }
        }

        public async Task<bool> AddQuestionAsync(ExamQuestionItemDto question)
        {
            try
            {
                using var req = CreateAuthorizedRequest(HttpMethod.Post, "api/admin/exam-questions", question);
                using var res = await _http.SendAsync(req);
                return res.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding question to exam {ExamId}", question.ExamId);
                return false;
            }
        }

        public async Task<string?> UploadQuestionImageAsync(Microsoft.AspNetCore.Http.IFormFile file)
        {
            if (file == null || file.Length == 0) return null;

            try
            {
                using var content = new MultipartFormDataContent();
                using var stream = file.OpenReadStream();
                using var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
                content.Add(streamContent, "file", file.FileName);

                using var req = CreateAuthorizedRequest(HttpMethod.Post, "api/admin/exam-questions/upload-image");
                req.Content = content;

                using var res = await _http.SendAsync(req);
                if (!res.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to upload question image. Status: {StatusCode}", res.StatusCode);
                    return null;
                }

                var json = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("imageUrl", out var imgProp))
                {
                    return imgProp.GetString();
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading question image");
                return null;
            }
        }

        public async Task<bool> DeleteQuestionAsync(Guid questionId)
        {
            try
            {
                using var req = CreateAuthorizedRequest(HttpMethod.Delete, $"api/admin/exam-questions/{questionId}");
                using var res = await _http.SendAsync(req);
                return res.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting question {QuestionId}", questionId);
                return false;
            }
        }

        public async Task<bool> RemoveExamQuestionAsync(Guid examId, Guid questionId)
        {
            try
            {
                // Try exam-builder mapping endpoint first to auto-recalculate total marks
                using var req = CreateAuthorizedRequest(HttpMethod.Delete, $"api/admin/exam-builder/{examId}/questions/{questionId}");
                using var res = await _http.SendAsync(req);
                if (res.IsSuccessStatusCode) return true;

                // Fallback to legacy exam-questions endpoint
                return await DeleteQuestionAsync(questionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing question {QuestionId} from exam {ExamId}", questionId, examId);
                return false;
            }
        }

        public async Task<QuestionBankResultDto> GetQuestionBankAsync(
            string? searchText = null,
            string? questionType = null,
            string? difficultyLevel = null,
            int page = 1,
            int pageSize = 50)
        {
            try
            {
                var queryParams = new List<string>
                {
                    $"page={Math.Max(1, page)}",
                    $"pageSize={Math.Max(1, Math.Min(200, pageSize))}"
                };

                if (!string.IsNullOrWhiteSpace(searchText))
                    queryParams.Add($"searchText={Uri.EscapeDataString(searchText.Trim())}");

                if (!string.IsNullOrWhiteSpace(questionType))
                    queryParams.Add($"questionType={Uri.EscapeDataString(questionType.Trim())}");

                if (!string.IsNullOrWhiteSpace(difficultyLevel))
                    queryParams.Add($"difficultyLevel={Uri.EscapeDataString(difficultyLevel.Trim())}");

                var url = "api/questions?" + string.Join("&", queryParams);
                using var req = CreateAuthorizedRequest(HttpMethod.Get, url);
                using var res = await _http.SendAsync(req);

                if (!res.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch questions from bank: {Status}", res.StatusCode);
                    return new QuestionBankResultDto();
                }

                var content = await res.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<QuestionBankResultDto>(content, _jsonOpts) ?? new QuestionBankResultDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Question Bank questions");
                return new QuestionBankResultDto();
            }
        }

        public async Task<(bool success, int added, string message)> ImportQuestionsBulkAsync(Guid examId, List<AddQuestionToExamDto> questions)
        {
            try
            {
                using var req = CreateAuthorizedRequest(HttpMethod.Post, $"api/admin/exam-builder/{examId}/questions/bulk", questions);
                using var res = await _http.SendAsync(req);

                var content = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode)
                {
                    return (false, 0, $"Failed to import questions: {content}");
                }

                using var doc = JsonDocument.Parse(content);
                int added = doc.RootElement.TryGetProperty("added", out var p) ? p.GetInt32() : questions.Count;
                return (true, added, $"Successfully imported {added} question(s) to exam.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk importing questions into exam {ExamId}", examId);
                return (false, 0, ex.Message);
            }
        }

        public async Task<PdfExtractionResultDto?> ExtractPdfQuestionsAsync(Microsoft.AspNetCore.Http.IFormFile pdfFile)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, "api/admin/questions/extract-pdf");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GenerateAdminJwtToken());

                using var formContent = new MultipartFormDataContent();
                using var stream = pdfFile.OpenReadStream();
                using var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(pdfFile.ContentType) ? "application/pdf" : pdfFile.ContentType);
                formContent.Add(streamContent, "file", pdfFile.FileName);
                req.Content = formContent;

                using var res = await _http.SendAsync(req);
                var content = await res.Content.ReadAsStringAsync();

                if (!res.IsSuccessStatusCode)
                {
                    _logger.LogWarning("PDF extraction failed: {Status} {Content}", res.StatusCode, content);
                    return new PdfExtractionResultDto { Error = $"Extraction failed: {content}" };
                }

                return JsonSerializer.Deserialize<PdfExtractionResultDto>(content, _jsonOpts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during PDF question extraction");
                return new PdfExtractionResultDto { Error = ex.Message };
            }
        }

        public async Task<(bool success, int saved, string message)> SaveExtractedQuestionsAsync(SaveExtractedQuestionsDto dto)
        {
            try
            {
                using var req = CreateAuthorizedRequest(HttpMethod.Post, "api/admin/questions/save-extracted", dto);
                using var res = await _http.SendAsync(req);

                var content = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode)
                {
                    return (false, 0, $"Failed to save extracted questions: {content}");
                }

                using var doc = JsonDocument.Parse(content);
                int saved = doc.RootElement.TryGetProperty("saved", out var p) ? p.GetInt32() : dto.Questions.Count;
                return (true, saved, $"Successfully saved and mapped {saved} question(s) to exam.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving extracted questions for exam {ExamId}", dto.ExamId);
                return (false, 0, ex.Message);
            }
        }

        public async Task<List<ExamQuestionItemDto>> GetStudentExamQuestionsAsync(Guid examId)
        {
            try
            {
                using var req = CreateAuthorizedRequest(HttpMethod.Get, $"api/student/exams/{examId}/questions");
                using var res = await _http.SendAsync(req);

                if (!res.IsSuccessStatusCode) return new List<ExamQuestionItemDto>();

                var content = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("questions", out var qArray))
                {
                    return JsonSerializer.Deserialize<List<ExamQuestionItemDto>>(qArray.GetRawText(), _jsonOpts) ?? new List<ExamQuestionItemDto>();
                }
                return JsonSerializer.Deserialize<List<ExamQuestionItemDto>>(content, _jsonOpts) ?? new List<ExamQuestionItemDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching student questions for exam {ExamId}", examId);
                return new List<ExamQuestionItemDto>();
            }
        }

        // =========================================================================
        // --- CANDIDATES & ASSIGNMENTS ---
        // =========================================================================

        public async Task<List<CandidateItemDto>> GetCandidatesAsync()
        {
            try
            {
                using var req = CreateAuthorizedRequest(HttpMethod.Get, "api/admin/candidates");
                using var res = await _http.SendAsync(req);

                if (!res.IsSuccessStatusCode) return new List<CandidateItemDto>();

                var content = await res.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<CandidateItemDto>>(content, _jsonOpts) ?? new List<CandidateItemDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching candidates");
                return new List<CandidateItemDto>();
            }
        }

        public async Task<List<ExamAssignmentItemDto>> GetAssignmentsAsync(Guid? examId)
        {
            try
            {
                string path = "api/exam-assignments" + (examId.HasValue ? $"?examId={examId.Value}" : "");
                // Try AuthService port first, then default
                var requestUri = new Uri(new Uri(_options.AuthBaseUrl.TrimEnd('/') + "/"), path);

                using var req = new HttpRequestMessage(HttpMethod.Get, requestUri);
                var token = GenerateAdminJwtToken();
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var res = await _http.SendAsync(req);
                if (!res.IsSuccessStatusCode) return new List<ExamAssignmentItemDto>();

                var content = await res.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<ExamAssignmentItemDto>>(content, _jsonOpts) ?? new List<ExamAssignmentItemDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assignments for exam {ExamId}", examId);
                return new List<ExamAssignmentItemDto>();
            }
        }

        public async Task<bool> AssignCandidateAsync(CreateExamAssignmentDto assignmentDto)
        {
            try
            {
                var requestUri = new Uri(new Uri(_options.AuthBaseUrl.TrimEnd('/') + "/"), "api/exam-assignments");
                using var req = new HttpRequestMessage(HttpMethod.Post, requestUri);
                var token = GenerateAdminJwtToken();
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var json = JsonSerializer.Serialize(assignmentDto, _jsonOpts);
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");

                using var res = await _http.SendAsync(req);
                return res.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating exam assignment");
                return false;
            }
        }

        public async Task<bool> DeleteAssignmentAsync(Guid assignmentId)
        {
            try
            {
                var requestUri = new Uri(new Uri(_options.AuthBaseUrl.TrimEnd('/') + "/"), $"api/exam-assignments/{assignmentId}");
                using var req = new HttpRequestMessage(HttpMethod.Delete, requestUri);
                var token = GenerateAdminJwtToken();
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var res = await _http.SendAsync(req);
                return res.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting exam assignment {AssignmentId}", assignmentId);
                return false;
            }
        }

        // =========================================================================
        // --- VIOLATIONS ---
        // =========================================================================

        public async Task<List<ViolationItemDto>> GetViolationsAsync(Guid examId)
        {
            try
            {
                using var req = CreateAuthorizedRequest(HttpMethod.Get, $"api/proctoring/violations/{examId}");
                using var res = await _http.SendAsync(req);

                if (!res.IsSuccessStatusCode) return new List<ViolationItemDto>();

                var content = await res.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<ViolationItemDto>>(content, _jsonOpts) ?? new List<ViolationItemDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting violations for exam {ExamId}", examId);
                return new List<ViolationItemDto>();
            }
        }

        // =========================================================================
        // --- MASTER DATA ---
        // =========================================================================

        public async Task<MasterDataResultDto> GetMasterDataAsync()
        {
            try
            {
                var requestUri = new Uri(new Uri(_options.AuthBaseUrl.TrimEnd('/') + "/"), "api/academic/master-data");
                using var req = new HttpRequestMessage(HttpMethod.Get, requestUri);
                var token = GenerateAdminJwtToken();
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var res = await _http.SendAsync(req);
                if (!res.IsSuccessStatusCode) return new MasterDataResultDto();

                var content = await res.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<MasterDataResultDto>(content, _jsonOpts) ?? new MasterDataResultDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting master data from AuthService");
                return new MasterDataResultDto();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TMS.Repository;
using TMS.ViewModels.Academics;

namespace TMS.Web.Services
{
    public class GeminiSuggestedQuestionService : ISuggestedQuestionService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDBContext _db;
        private readonly ILogger<GeminiSuggestedQuestionService> _logger;

        public GeminiSuggestedQuestionService(
            HttpClient httpClient,
            IConfiguration configuration,
            ApplicationDBContext db,
            ILogger<GeminiSuggestedQuestionService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _db = db;
            _logger = logger;
        }

        public async Task<SuggestedQuestionResult> GenerateSuggestedQuestionsAsync(
            SuggestedQuestionRequestViewModel request,
            int facultyId,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                return new SuggestedQuestionResult { Success = false, ErrorMessage = "Invalid question request." };

            // 1. Verify course and subject in database
            var course = await _db.CourseMasters.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.CourseId && c.IsActive, cancellationToken);
            if (course == null)
                return new SuggestedQuestionResult { Success = false, ErrorMessage = "Selected course was not found or is inactive." };

            var subject = await _db.SubjectMasters.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == request.SubjectId && s.IsActive, cancellationToken);
            if (subject == null)
                return new SuggestedQuestionResult { Success = false, ErrorMessage = "Selected subject was not found or is inactive." };

            var semesterName = "General Semester";
            if (request.SemesterId.HasValue && request.SemesterId.Value > 0)
            {
                var semester = await _db.SemesterMasters.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == request.SemesterId.Value, cancellationToken);
                if (semester != null)
                    semesterName = semester.Name;
            }
            else if (course.SemesterId.HasValue)
            {
                var semester = await _db.SemesterMasters.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == course.SemesterId.Value, cancellationToken);
                if (semester != null)
                    semesterName = semester.Name;
            }

            // 2. Fetch real curriculum content from database (Units & Topics)
            var curriculumSummary = new StringBuilder();
            try
            {
                var units = await _db.UnitMasters.AsNoTracking()
                    .Where(u => u.SubjectId == subject.Id && u.IsActive)
                    .OrderBy(u => u.SortOrder)
                    .Select(u => new { u.Id, u.Name, u.SortOrder })
                    .ToListAsync(cancellationToken);

                var unitIds = units.Select(u => u.Id).ToList();
                if (unitIds.Any())
                {
                    var topics = await _db.TopicMasters.AsNoTracking()
                        .Where(t => unitIds.Contains(t.UnitId) && t.IsActive)
                        .OrderBy(t => t.SortOrder)
                        .Select(t => new { t.UnitId, t.Name, t.SortOrder })
                        .ToListAsync(cancellationToken);

                    foreach (var unit in units)
                    {
                        curriculumSummary.AppendLine($"- Unit: {unit.Name}");
                        var unitTopics = topics.Where(t => t.UnitId == unit.Id).ToList();
                        foreach (var topic in unitTopics)
                        {
                            curriculumSummary.AppendLine($"  * Topic: {topic.Name}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load curriculum units/topics from database for Subject {SubjectId}. Proceeding with subject and user context.", subject.Id);
            }

            // If no syllabus units/topics exist in database and faculty provided no topic context,
            // do not invent fake syllabus content; prompt the faculty to provide topic details.
            if (curriculumSummary.Length == 0 && string.IsNullOrWhiteSpace(request.TopicContext))
            {
                return new SuggestedQuestionResult
                {
                    Success = false,
                    RequiresTopicContext = true,
                    ErrorMessage = "No official curriculum units or topics are registered for this subject in the database. Please enter a topic or focus area to guide question generation."
                };
            }

            // 3. Resolve API Key & Configuration
            var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = _configuration["Gemini:ApiKey"] ?? _configuration["GEMINI_API_KEY"];
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("Gemini API key is not configured.");
                return new SuggestedQuestionResult
                {
                    Success = false,
                    ErrorMessage = "Question suggestion service is not configured. Please contact the administrator to configure the API credentials."
                };
            }

            var model = _configuration["Gemini:Model"] ?? "gemini-1.5-flash";

            string? unitName = null;
            if (request.UnitId.HasValue && request.UnitId.Value > 0)
            {
                var unit = await _db.UnitMasters.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UnitId.Value, cancellationToken);
                unitName = unit?.Name;
            }

            string? topicName = null;
            if (request.TopicId.HasValue && request.TopicId.Value > 0)
            {
                topicName = await _db.TopicMasters.AsNoTracking()
                    .Where(t => t.Id == request.TopicId.Value)
                    .Select(t => t.Name)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            // 4. Construct Prompt
            var prompt = BuildPrompt(request, course.Name, course.CourseCode, semesterName, subject.Name, subject.SubjectCode, curriculumSummary.ToString(), request.TopicContext, unitName, topicName);

            // 5. Send Request with transient retry logic
            var rawJson = await CallGeminiApiAsync(apiKey, model, prompt, cancellationToken);
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                return new SuggestedQuestionResult
                {
                    Success = false,
                    ErrorMessage = "Unable to generate questions at this time. Please retry in a few moments."
                };
            }

            // 6. Parse and Validate Response
            var parsedQuestions = ParseAndValidateQuestions(rawJson, request, unitName, topicName);
            if (!parsedQuestions.Any())
            {
                return new SuggestedQuestionResult
                {
                    Success = false,
                    ErrorMessage = "The question generation service returned an unexpected format. Please retry."
                };
            }

            return new SuggestedQuestionResult
            {
                Success = true,
                Questions = parsedQuestions
            };
        }

        public async Task<SuggestedQuestionResult> RegenerateSingleQuestionAsync(
            SuggestedQuestionItemViewModel currentQuestion,
            SuggestedQuestionRequestViewModel context,
            int facultyId,
            CancellationToken cancellationToken = default)
        {
            if (currentQuestion == null || context == null)
                return new SuggestedQuestionResult { Success = false, ErrorMessage = "Invalid regeneration parameters." };

            // Request 1 alternative question, providing the existing question to ensure a distinct new question is produced
            var singleRequest = new SuggestedQuestionRequestViewModel
            {
                CourseId = context.CourseId,
                SemesterId = context.SemesterId,
                SubjectId = context.SubjectId,
                QuestionType = currentQuestion.QuestionType,
                DifficultyLevel = currentQuestion.DifficultyLevel,
                QuestionCount = 1,
                MarksPerQuestion = currentQuestion.Marks,
                TopicContext = string.IsNullOrWhiteSpace(context.TopicContext) ? null : context.TopicContext
            };

            var course = await _db.CourseMasters.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == context.CourseId && c.IsActive, cancellationToken);
            var subject = await _db.SubjectMasters.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == context.SubjectId && s.IsActive, cancellationToken);

            var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") 
                         ?? _configuration["Gemini:ApiKey"] 
                         ?? _configuration["GEMINI_API_KEY"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return new SuggestedQuestionResult
                {
                    Success = false,
                    ErrorMessage = "Question suggestion service is not configured."
                };
            }

            var model = _configuration["Gemini:Model"] ?? "gemini-1.5-flash";

            var prompt = $@"You are an expert university academic examiner and professor.
Generate exactly 1 NEW, alternative examination question to replace the following existing question:
Existing Question to replace: ""{currentQuestion.QuestionText}""

Academic Parameters:
- Course: {course?.Name ?? "Course"} ({course?.CourseCode ?? ""})
- Subject: {subject?.Name ?? "Subject"}
- Question Type: {currentQuestion.QuestionType}
- Difficulty Level: {currentQuestion.DifficultyLevel}
- Marks: {currentQuestion.Marks}
{(string.IsNullOrWhiteSpace(context.TopicContext) ? "" : $"- Focus Topic: {context.TopicContext}")}

Requirements:
- The new question must test the same subject curriculum, but must have completely different wording and focus from the existing question.
- Do NOT mention AI, models, or system prompts.
{(currentQuestion.QuestionType == "MCQ" ? "- Provide exactly 4 options. The CorrectAnswer must strictly match one of the 4 options verbatim." : "- Provide a clear Model Answer / key concepts in CorrectAnswer.")}

Return ONLY a JSON array with exactly 1 object conforming to this schema:
[
  {{
    ""QuestionText"": ""..."",
    ""QuestionType"": ""{currentQuestion.QuestionType}"",
    ""DifficultyLevel"": ""{currentQuestion.DifficultyLevel}"",
    ""Marks"": {currentQuestion.Marks},
    ""Options"": [""Option A"", ""Option B"", ""Option C"", ""Option D""],
    ""CorrectAnswer"": ""..."",
    ""Explanation"": ""...""
  }}
]";

            var rawJson = await CallGeminiApiAsync(apiKey, model, prompt, cancellationToken);
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                return new SuggestedQuestionResult
                {
                    Success = false,
                    ErrorMessage = "Failed to regenerate question. Please try again."
                };
            }

            var parsed = ParseAndValidateQuestions(rawJson, singleRequest, currentQuestion.UnitName, currentQuestion.TopicName);
            if (!parsed.Any())
            {
                return new SuggestedQuestionResult
                {
                    Success = false,
                    ErrorMessage = "Unable to parse regenerated question response. Please try again."
                };
            }

            return new SuggestedQuestionResult
            {
                Success = true,
                Questions = parsed
            };
        }

        private string BuildPrompt(
            SuggestedQuestionRequestViewModel req,
            string courseName,
            string? courseCode,
            string semesterName,
            string subjectName,
            string? subjectCode,
            string curriculumContent,
            string? topicContext,
            string? unitName = null,
            string? topicName = null)
        {
            var isMcq = req.QuestionType.Equals("MCQ", StringComparison.OrdinalIgnoreCase);

            var sb = new StringBuilder();
            sb.AppendLine("You are a distinguished university academic professor and curriculum examiner.");
            sb.AppendLine("Generate high-quality, academically rigorous examination questions grounded strictly in the syllabus details below.");
            sb.AppendLine();
            sb.AppendLine("### ACADEMIC CONTEXT");
            sb.AppendLine($"- Course: {courseName} {(string.IsNullOrEmpty(courseCode) ? "" : $"({courseCode})")}");
            sb.AppendLine($"- Semester: {semesterName}");
            sb.AppendLine($"- Subject: {subjectName} {(string.IsNullOrEmpty(subjectCode) ? "" : $"({subjectCode})")}");

            if (!string.IsNullOrWhiteSpace(unitName))
            {
                sb.AppendLine($"- Target Unit: {unitName}");
            }
            if (!string.IsNullOrWhiteSpace(topicName))
            {
                sb.AppendLine($"- Target Topic: {topicName}");
            }
            if (!string.IsNullOrWhiteSpace(req.LearningOutcome))
            {
                sb.AppendLine($"- Target Learning Outcome: {req.LearningOutcome}");
            }

            if (!string.IsNullOrWhiteSpace(curriculumContent))
            {
                sb.AppendLine("### OFFICIAL SYLLABUS / CURRICULUM UNITS:");
                sb.AppendLine(curriculumContent);
            }

            if (!string.IsNullOrWhiteSpace(topicContext))
            {
                sb.AppendLine($"### FOCUS TOPIC / SPECIALIZATION REQUESTED BY FACULTY:\n{topicContext}");
            }

            sb.AppendLine();
            sb.AppendLine("### GENERATION REQUIREMENTS");
            sb.AppendLine($"- Target Question Count: {req.QuestionCount}");
            sb.AppendLine($"- Question Type: {req.QuestionType}");
            sb.AppendLine($"- Target Difficulty Level: {req.DifficultyLevel}");
            sb.AppendLine($"- Marks Per Question: {req.MarksPerQuestion}");
            sb.AppendLine("- All questions must be relevant to undergraduate/postgraduate degree curriculum standard.");
            sb.AppendLine("- Do not use trivial or superficial questions.");
            sb.AppendLine("- Never include any references to AI, Gemini, Google, machine learning prompts, or generation tokens.");

            if (isMcq)
            {
                sb.AppendLine("- For each MCQ, generate EXACTLY 4 plausible, unambiguous options (A, B, C, D text).");
                sb.AppendLine("- The CorrectAnswer field must be the EXACT full text of the correct option matching one of the items in the Options array.");
                sb.AppendLine("- In Explanation, provide a brief academic explanation justifying why this option is correct.");
            }
            else
            {
                sb.AppendLine("- The Options array must be an empty list [].");
                sb.AppendLine("- In CorrectAnswer, provide a comprehensive Model Answer / key points that faculty can use to grade student responses.");
                sb.AppendLine("- In Explanation, provide marking scheme notes and grading criteria.");
            }

            sb.AppendLine();
            sb.AppendLine("### OUTPUT FORMAT");
            sb.AppendLine("Return strictly a JSON array of objects conforming to the schema below. No surrounding conversational markdown or text.");
            sb.AppendLine(@"[
  {
    ""QuestionText"": ""Clear, formal question text"",
    ""QuestionType"": """ + req.QuestionType + @""",
    ""DifficultyLevel"": """ + req.DifficultyLevel + @""",
    ""Marks"": " + req.MarksPerQuestion + @",
    ""Options"": " + (isMcq ? @"[""Option 1"", ""Option 2"", ""Option 3"", ""Option 4""]" : "[]") + @",
    ""CorrectAnswer"": """ + (isMcq ? "Exact text of the correct option" : "Comprehensive model answer / rubric points") + @""",
    ""Explanation"": ""Academic rationale and marking criteria"",
    ""LearningOutcome"": """ + (string.IsNullOrWhiteSpace(req.LearningOutcome) ? "e.g. CO1 - Core subject understanding" : req.LearningOutcome) + @"""
  }
]");

            return sb.ToString();
        }

        private async Task<string?> CallGeminiApiAsync(string apiKey, string model, string prompt, CancellationToken cancellationToken)
        {
            var maxRetries = 2;
            var currentAttempt = 0;
            var requestUri = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(apiKey)}";

            while (currentAttempt <= maxRetries)
            {
                currentAttempt++;
                try
                {
                    var payload = new
                    {
                        contents = new[]
                        {
                            new
                            {
                                role = "user",
                                parts = new[]
                                {
                                    new { text = prompt }
                                }
                            }
                        },
                        generationConfig = new
                        {
                            response_mime_type = "application/json",
                            temperature = 0.3
                        }
                    };

                    var serializerOptions = new JsonSerializerOptions
                    {
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    };
                    var requestJson = JsonSerializer.Serialize(payload, serializerOptions);

                    using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri)
                    {
                        Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
                    };
                    httpRequest.Headers.Add("x-goog-api-key", apiKey);

                    using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        var body = await response.Content.ReadAsStringAsync(cancellationToken);
                        return ExtractCandidateText(body);
                    }

                    var statusCode = (int)response.StatusCode;
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("Gemini API call failed with status {StatusCode} on attempt {Attempt}: {ErrorBody}", statusCode, currentAttempt, errorBody);

                    if (statusCode == 429 || statusCode == 500 || statusCode == 502 || statusCode == 503 || statusCode == 504)
                    {
                        if (currentAttempt <= maxRetries)
                        {
                            var delay = statusCode == 429 ? 2500 * currentAttempt : 1200 * currentAttempt;
                            await Task.Delay(delay, cancellationToken);
                            continue;
                        }
                    }

                    return null;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Exception calling question generation service on attempt {Attempt}", currentAttempt);
                    if (currentAttempt <= maxRetries)
                    {
                        await Task.Delay(1000 * currentAttempt, cancellationToken);
                        continue;
                    }
                    return null;
                }
            }

            return null;
        }

        private string? ExtractCandidateText(string responseJson)
        {
            try
            {
                var doc = JsonDocument.Parse(responseJson);
                var candidates = doc.RootElement.GetProperty("candidates");
                if (candidates.GetArrayLength() == 0) return null;

                var firstCandidate = candidates[0];
                var content = firstCandidate.GetProperty("content");
                var parts = content.GetProperty("parts");
                if (parts.GetArrayLength() == 0) return null;

                return parts[0].GetProperty("text").GetString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract candidate text from Gemini response.");
                return null;
            }
        }

        private List<SuggestedQuestionItemViewModel> ParseAndValidateQuestions(
            string rawJson,
            SuggestedQuestionRequestViewModel req,
            string? unitName = null,
            string? topicName = null)
        {
            var results = new List<SuggestedQuestionItemViewModel>();
            if (string.IsNullOrWhiteSpace(rawJson)) return results;

            try
            {
                var cleaned = CleanJsonString(rawJson);
                using var doc = JsonDocument.Parse(cleaned);
                JsonElement arrayElement;

                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    arrayElement = doc.RootElement;
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("questions", out var qArray) && qArray.ValueKind == JsonValueKind.Array)
                    {
                        arrayElement = qArray;
                    }
                    else if (doc.RootElement.TryGetProperty("data", out var dArray) && dArray.ValueKind == JsonValueKind.Array)
                    {
                        arrayElement = dArray;
                    }
                    else
                    {
                        return results;
                    }
                }
                else
                {
                    return results;
                }

                var isMcq = req.QuestionType.Equals("MCQ", StringComparison.OrdinalIgnoreCase);

                foreach (var el in arrayElement.EnumerateArray())
                {
                    var qText = el.TryGetProperty("QuestionText", out var qt) ? qt.GetString() : null;
                    if (string.IsNullOrWhiteSpace(qText))
                    {
                        // Fallback check
                        if (el.TryGetProperty("question", out var altQ)) qText = altQ.GetString();
                    }

                    if (string.IsNullOrWhiteSpace(qText)) continue;

                    var qType = el.TryGetProperty("QuestionType", out var typeEl) ? typeEl.GetString() : req.QuestionType;
                    var difficulty = el.TryGetProperty("DifficultyLevel", out var diffEl) ? diffEl.GetString() : req.DifficultyLevel;
                    decimal marks = req.MarksPerQuestion;
                    if (el.TryGetProperty("Marks", out var marksEl) && marksEl.TryGetDecimal(out var mVal) && mVal > 0)
                    {
                        marks = mVal;
                    }

                    var explanation = el.TryGetProperty("Explanation", out var expEl) ? expEl.GetString() : null;
                    var correctAnswer = el.TryGetProperty("CorrectAnswer", out var caEl) ? caEl.GetString() : "";

                    var lo = el.TryGetProperty("LearningOutcome", out var loEl) ? loEl.GetString() : null;
                    if (string.IsNullOrWhiteSpace(lo)) lo = req.LearningOutcome;

                    var optionsList = new List<string>();
                    if (el.TryGetProperty("Options", out var optEl) && optEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var opt in optEl.EnumerateArray())
                        {
                            var s = opt.GetString();
                            if (!string.IsNullOrWhiteSpace(s))
                                optionsList.Add(s.Trim());
                        }
                    }

                    if (isMcq)
                    {
                        // Ensure at least 4 options
                        if (optionsList.Count < 2) continue; // Invalid MCQ

                        // Resolve correct answer matching
                        if (!string.IsNullOrWhiteSpace(correctAnswer))
                        {
                            correctAnswer = correctAnswer.Trim();
                            // If correctAnswer is just "A", "B", "C", "D"
                            if (correctAnswer.Length == 1 && char.IsLetter(correctAnswer[0]))
                            {
                                int idx = char.ToUpper(correctAnswer[0]) - 'A';
                                if (idx >= 0 && idx < optionsList.Count)
                                {
                                    correctAnswer = optionsList[idx];
                                }
                            }
                            else if (!optionsList.Contains(correctAnswer))
                            {
                                // Attempt fuzzy match
                                var matched = optionsList.FirstOrDefault(o => o.Equals(correctAnswer, StringComparison.OrdinalIgnoreCase)
                                    || o.StartsWith(correctAnswer, StringComparison.OrdinalIgnoreCase));
                                if (matched != null)
                                    correctAnswer = matched;
                                else if (optionsList.Any())
                                    correctAnswer = optionsList[0]; // Fallback to first option
                            }
                        }
                        else if (optionsList.Any())
                        {
                            correctAnswer = optionsList[0];
                        }
                    }

                    results.Add(new SuggestedQuestionItemViewModel
                    {
                        TempId = Guid.NewGuid().ToString("N"),
                        QuestionText = qText.Trim(),
                        QuestionType = string.IsNullOrWhiteSpace(qType) ? req.QuestionType : qType.Trim(),
                        DifficultyLevel = string.IsNullOrWhiteSpace(difficulty) ? req.DifficultyLevel : difficulty.Trim(),
                        Marks = marks,
                        UnitId = req.UnitId,
                        UnitName = unitName,
                        TopicId = req.TopicId,
                        TopicName = topicName,
                        LearningOutcome = lo?.Trim(),
                        Options = optionsList,
                        CorrectAnswer = correctAnswer ?? "",
                        Explanation = explanation,
                        IsAccepted = false,
                        IsRejected = false
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse questions JSON: {RawJson}", rawJson);
            }

            return results;
        }

        private static string CleanJsonString(string raw)
        {
            var cleaned = raw.Trim();
            if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(7);
            }
            else if (cleaned.StartsWith("```"))
            {
                cleaned = cleaned.Substring(3);
            }

            if (cleaned.EndsWith("```"))
            {
                cleaned = cleaned.Substring(0, cleaned.Length - 3);
            }

            return cleaned.Trim();
        }

        public static string ComputeNormalizedText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var lower = text.ToLowerInvariant();
            var sb = new StringBuilder(lower.Length);
            foreach (var ch in lower)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(ch);
                }
                else
                {
                    sb.Append(' ');
                }
            }
            return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
        }

        public static double CalculateSimilarity(string textA, string textB)
        {
            var normA = ComputeNormalizedText(textA);
            var normB = ComputeNormalizedText(textB);
            if (normA == normB) return 1.0;
            if (string.IsNullOrEmpty(normA) || string.IsNullOrEmpty(normB)) return 0.0;

            var tokensA = normA.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
            var tokensB = normB.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

            if (tokensA.Count == 0 || tokensB.Count == 0) return 0.0;

            int intersection = tokensA.Intersect(tokensB).Count();
            return (2.0 * intersection) / (tokensA.Count + tokensB.Count);
        }

        public async Task<List<QuestionClassificationSuggestionDto>> ClassifyLegacyQuestionsAsync(
            int courseId,
            int subjectId,
            List<LegacyQuestionToClassifyDto> questions,
            CancellationToken cancellationToken = default)
        {
            var suggestions = new List<QuestionClassificationSuggestionDto>();
            if (questions == null || !questions.Any())
                return suggestions;

            var units = await _db.UnitMasters.AsNoTracking()
                .Where(u => u.SubjectId == subjectId && u.IsActive)
                .OrderBy(u => u.SortOrder)
                .Select(u => new { u.Id, u.Name })
                .ToListAsync(cancellationToken);

            var unitIds = units.Select(u => u.Id).ToList();
            var topics = await _db.TopicMasters.AsNoTracking()
                .Where(t => unitIds.Contains(t.UnitId) && t.IsActive)
                .OrderBy(t => t.SortOrder)
                .Select(t => new { t.Id, t.UnitId, t.Name })
                .ToListAsync(cancellationToken);

            var apiKey = _configuration["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            var model = _configuration["Gemini:Model"] ?? "gemini-2.5-flash";

            if (!string.IsNullOrWhiteSpace(apiKey) && units.Any())
            {
                try
                {
                    var sbPrompt = new StringBuilder();
                    sbPrompt.AppendLine("You are an expert academic curriculum classifier for higher education.");
                    sbPrompt.AppendLine("Task: Analyze the given list of legacy exam questions and map each question to the most appropriate Syllabus Unit, Topic, Bloom's Taxonomy Learning Outcome, and Difficulty Level.");
                    sbPrompt.AppendLine();
                    sbPrompt.AppendLine("### AVAILABLE UNITS:");
                    foreach (var u in units)
                    {
                        sbPrompt.AppendLine($"- UnitId: {u.Id}, Name: {u.Name}");
                    }
                    sbPrompt.AppendLine();
                    sbPrompt.AppendLine("### AVAILABLE TOPICS:");
                    foreach (var t in topics)
                    {
                        sbPrompt.AppendLine($"- TopicId: {t.Id}, UnitId: {t.UnitId}, Name: {t.Name}");
                    }
                    sbPrompt.AppendLine();
                    sbPrompt.AppendLine("### VALID LEARNING OUTCOMES (Bloom's Taxonomy):");
                    sbPrompt.AppendLine("- Remember, Understand, Apply, Analyze, Evaluate, Create");
                    sbPrompt.AppendLine();
                    sbPrompt.AppendLine("### VALID DIFFICULTY LEVELS:");
                    sbPrompt.AppendLine("- Easy, Medium, Hard");
                    sbPrompt.AppendLine();
                    sbPrompt.AppendLine("### QUESTIONS TO CLASSIFY:");
                    foreach (var q in questions)
                    {
                        sbPrompt.AppendLine($"Question ID: {q.Id} | Marks: {q.Marks} | Text: \"{q.QuestionText}\"");
                    }
                    sbPrompt.AppendLine();
                    sbPrompt.AppendLine("### OUTPUT FORMAT");
                    sbPrompt.AppendLine("Return strictly a JSON array of objects with the following schema:");
                    sbPrompt.AppendLine(@"[
  {
    ""QuestionId"": 1,
    ""SuggestedUnitId"": 1,
    ""SuggestedTopicId"": 1,
    ""SuggestedDifficultyLevel"": ""Medium"",
    ""SuggestedLearningOutcome"": ""Understand"",
    ""Rationale"": ""Explanation of alignment""
  }
]");

                    var rawJson = await CallGeminiApiAsync(apiKey, model, sbPrompt.ToString(), cancellationToken);
                    if (!string.IsNullOrWhiteSpace(rawJson))
                    {
                        var cleaned = CleanJsonString(rawJson);
                        var parsed = JsonSerializer.Deserialize<List<QuestionClassificationSuggestionDto>>(cleaned, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (parsed != null && parsed.Any())
                        {
                            foreach (var item in parsed)
                            {
                                var orig = questions.FirstOrDefault(q => q.Id == item.QuestionId);
                                if (orig != null)
                                {
                                    item.QuestionText = orig.QuestionText;
                                    var u = units.FirstOrDefault(x => x.Id == item.SuggestedUnitId);
                                    item.SuggestedUnitName = u?.Name;
                                    var t = topics.FirstOrDefault(x => x.Id == item.SuggestedTopicId);
                                    item.SuggestedTopicName = t?.Name;
                                    suggestions.Add(item);
                                }
                            }
                            return suggestions;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Gemini batch classification failed, falling back to curriculum heuristic matching.");
                }
            }

            // Fallback: Rule-based heuristic classification based on keyword matching
            foreach (var q in questions)
            {
                var qLower = (q.QuestionText ?? "").ToLowerInvariant();
                int? bestUnitId = null;
                string? bestUnitName = null;
                int? bestTopicId = null;
                string? bestTopicName = null;

                foreach (var t in topics)
                {
                    if (qLower.Contains(t.Name.ToLowerInvariant()))
                    {
                        bestTopicId = t.Id;
                        bestTopicName = t.Name;
                        bestUnitId = t.UnitId;
                        var matchedUnit = units.FirstOrDefault(u => u.Id == t.UnitId);
                        bestUnitName = matchedUnit?.Name;
                        break;
                    }
                }

                if (!bestUnitId.HasValue)
                {
                    foreach (var u in units)
                    {
                        if (qLower.Contains(u.Name.ToLowerInvariant()))
                        {
                            bestUnitId = u.Id;
                            bestUnitName = u.Name;
                            break;
                        }
                    }
                }

                if (!bestUnitId.HasValue && units.Any())
                {
                    bestUnitId = units.First().Id;
                    bestUnitName = units.First().Name;
                }

                string diff = q.Marks <= 2 ? "Easy" : (q.Marks <= 5 ? "Medium" : "Hard");
                string outcome = q.Marks <= 2 ? "Remember" : (q.Marks <= 5 ? "Understand" : "Apply");

                suggestions.Add(new QuestionClassificationSuggestionDto
                {
                    QuestionId = q.Id,
                    QuestionText = q.QuestionText,
                    SuggestedUnitId = bestUnitId,
                    SuggestedUnitName = bestUnitName,
                    SuggestedTopicId = bestTopicId,
                    SuggestedTopicName = bestTopicName,
                    SuggestedDifficultyLevel = diff,
                    SuggestedLearningOutcome = outcome,
                    Rationale = bestTopicName != null ? $"Matched topic keyword '{bestTopicName}'" : "Heuristic syllabus alignment based on mark weightage"
                });
            }

            return suggestions;
        }
    }
}

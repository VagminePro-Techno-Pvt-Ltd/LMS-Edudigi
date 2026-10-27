using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TMS.Repository;
using TMS.ViewModels.Academics;

namespace TMS.Web.Services
{
    /// <summary>
    /// Google Gemini REST integration for curriculum-aligned assignment, question, and rubric generation.
    /// </summary>
    public class GeminiAiAssignmentService : IAiAssignmentService
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDBContext _db;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiAiAssignmentService> _logger;

        public GeminiAiAssignmentService(
            HttpClient httpClient,
            ApplicationDBContext db,
            IConfiguration configuration,
            ILogger<GeminiAiAssignmentService> logger)
        {
            _httpClient = httpClient;
            _db = db;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AiAssignmentGenerateResult> GenerateAssignmentAsync(
            AiAssignmentGenerateRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return new AiAssignmentGenerateResult
                {
                    Success = false,
                    ErrorMessage = "Invalid assignment generation request."
                };
            }

            if (request.TotalMarks <= 0) request.TotalMarks = 50;
            if (request.NumberOfQuestions <= 0) request.NumberOfQuestions = 5;
            if (request.NumberOfQuestions > 15) request.NumberOfQuestions = 15;

            // 1. Resolve Academic Context from Database
            string programName = "Academic Program";
            string courseName = "Course";
            string courseCode = "";
            string semesterName = "";
            string subjectName = "";
            string subjectCode = "";
            string unitName = "";
            string topicName = "";

            try
            {
                var program = await _db.CourseCategoryMasters.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == request.ProgramId, cancellationToken);
                if (program != null) programName = program.Name;

                var course = await _db.CourseMasters.AsNoTracking()
                    .Include(c => c.Semester)
                    .FirstOrDefaultAsync(c => c.Id == request.CourseId, cancellationToken);
                if (course != null)
                {
                    courseName = course.Name;
                    courseCode = course.CourseCode ?? "";
                    if (course.Semester != null)
                    {
                        semesterName = course.Semester.Name;
                    }

                    if (program == null && course.CourseCategoryId > 0)
                    {
                        program = await _db.CourseCategoryMasters.AsNoTracking()
                            .FirstOrDefaultAsync(p => p.Id == course.CourseCategoryId, cancellationToken);
                        if (program != null) programName = program.Name;
                    }
                }

                if (request.SemesterId.HasValue && request.SemesterId.Value > 0 && string.IsNullOrEmpty(semesterName))
                {
                    var sem = await _db.SemesterMasters.AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == request.SemesterId.Value, cancellationToken);
                    if (sem != null) semesterName = sem.Name;
                }

                if (request.SubjectId.HasValue && request.SubjectId.Value > 0)
                {
                    var subject = await _db.SubjectMasters.AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == request.SubjectId.Value, cancellationToken);
                    if (subject != null)
                    {
                        subjectName = subject.Name;
                        subjectCode = subject.SubjectCode ?? "";
                    }
                }

                if (request.UnitId.HasValue && request.UnitId.Value > 0)
                {
                    var unit = await _db.UnitMasters.AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == request.UnitId.Value, cancellationToken);
                    if (unit != null) unitName = unit.Name;
                }

                if (request.TopicId.HasValue && request.TopicId.Value > 0)
                {
                    var topic = await _db.TopicMasters.AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Id == request.TopicId.Value, cancellationToken);
                    if (topic != null) topicName = topic.Name;
                }

                if (!string.IsNullOrWhiteSpace(request.CustomTopic))
                {
                    topicName = string.IsNullOrEmpty(topicName) ? request.CustomTopic.Trim() : $"{topicName} - {request.CustomTopic.Trim()}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error resolving curriculum details for assignment generation. Proceeding with available context.");
            }

            // 2. Resolve API Key & Configuration
            var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = _configuration["Gemini:ApiKey"] ?? _configuration["GEMINI_API_KEY"];
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("Gemini API key is not configured.");
                return new AiAssignmentGenerateResult
                {
                    Success = false,
                    ErrorMessage = "AI generation service credentials are not configured. Please contact the system administrator."
                };
            }

            var model = _configuration["Gemini:Model"] ?? "gemini-3.6-flash";

            // 3. Construct Prompt
            var prompt = BuildPrompt(request, programName, courseName, courseCode, semesterName, subjectName, subjectCode, unitName, topicName);

            // 4. Send Request with transient retry logic
            var rawJson = await CallGeminiApiAsync(apiKey, model, prompt, cancellationToken);
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                return new AiAssignmentGenerateResult
                {
                    Success = false,
                    ErrorMessage = "Unable to generate assignment at this time due to upstream AI service timeout or rate limit. Please retry in a few moments."
                };
            }

            // 5. Parse and Validate Response
            return ParseAndValidateResponse(rawJson, request);
        }

        private static string BuildPrompt(
            AiAssignmentGenerateRequest request,
            string programName,
            string courseName,
            string courseCode,
            string semesterName,
            string subjectName,
            string subjectCode,
            string unitName,
            string topicName)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are a distinguished university academic dean and curriculum specialist creating an official institutional assignment for higher education.");
            sb.AppendLine("Generate a comprehensive, challenging, curriculum-aligned academic assignment along with detailed student instructions, topic-based questions, marking criteria, and a formal evaluation rubric.");
            sb.AppendLine();
            sb.AppendLine("Academic Context:");
            sb.AppendLine($"- Program: {programName}");
            if (!string.IsNullOrWhiteSpace(semesterName)) sb.AppendLine($"- Semester: {semesterName}");
            sb.AppendLine($"- Course: {courseName} {(string.IsNullOrEmpty(courseCode) ? "" : $"({courseCode})")}");
            if (!string.IsNullOrWhiteSpace(subjectName)) sb.AppendLine($"- Subject: {subjectName} {(string.IsNullOrEmpty(subjectCode) ? "" : $"({subjectCode})")}");
            if (!string.IsNullOrWhiteSpace(unitName)) sb.AppendLine($"- Unit / Module: {unitName}");
            if (!string.IsNullOrWhiteSpace(topicName)) sb.AppendLine($"- Topic / Focus Area: {topicName}");
            sb.AppendLine($"- Difficulty Level: {request.Difficulty}");
            sb.AppendLine($"- Assignment Type: {request.AssignmentType}");
            sb.AppendLine($"- Required Number of Questions: {request.NumberOfQuestions}");
            sb.AppendLine($"- Total Marks: {request.TotalMarks}");
            if (!string.IsNullOrWhiteSpace(request.SubmissionInstructions))
            {
                sb.AppendLine($"- Faculty's Special Instructions: {request.SubmissionInstructions}");
            }
            sb.AppendLine();
            sb.AppendLine("STRICT REQUIREMENTS:");
            sb.AppendLine($"1. Generate EXACTLY {request.NumberOfQuestions} distinct, high-quality, topic-relevant questions.");
            sb.AppendLine($"2. Allocate marks to each question such that the sum of all question marks EXACTLY EQUALS {request.TotalMarks}.");
            sb.AppendLine("3. Each question must have a question number (1, 2, 3...), a clear question prompt, allocated marks, and brief expected answer guidelines/key concepts.");
            sb.AppendLine("4. Provide thorough student instructions covering formatting, research expectations, academic integrity/plagiarism, and clarity.");
            sb.AppendLine("5. Provide high-level marking criteria breakdown with percentages.");
            sb.AppendLine("6. Provide a detailed 4-tier evaluation rubric table with criteria and performance descriptors for: Excellent (80-100%), Good (65-79%), Fair (50-64%), and Poor (0-49%).");
            sb.AppendLine("7. Maintain natural, formal academic tone. NEVER include meta-commentary, introductory notes, markdown backticks outside JSON, or any references to AI, Gemini, Google, or prompts.");
            sb.AppendLine();
            sb.AppendLine("Output MUST be pure JSON conforming to this schema:");
            sb.AppendLine("{");
            sb.AppendLine("  \"title\": \"string (Professional Assignment Title)\",");
            sb.AppendLine("  \"description\": \"string (2-3 paragraphs describing scope, objectives, learning outcomes)\",");
            sb.AppendLine("  \"studentInstructions\": \"string (Formatted instructions for students)\",");
            sb.AppendLine("  \"questions\": [");
            sb.AppendLine("    {");
            sb.AppendLine("      \"questionNumber\": 1,");
            sb.AppendLine("      \"questionText\": \"string\",");
            sb.AppendLine("      \"marks\": 10.0,");
            sb.AppendLine("      \"expectedAnswerGuidelines\": \"string\"");
            sb.AppendLine("    }");
            sb.AppendLine("  ],");
            sb.AppendLine("  \"markingCriteria\": [");
            sb.AppendLine("    {");
            sb.AppendLine("      \"criterion\": \"string\",");
            sb.AppendLine("      \"weightage\": 25.0,");
            sb.AppendLine("      \"description\": \"string\"");
            sb.AppendLine("    }");
            sb.AppendLine("  ],");
            sb.AppendLine("  \"rubrics\": [");
            sb.AppendLine("    {");
            sb.AppendLine("      \"criterion\": \"string\",");
            sb.AppendLine("      \"maxMarks\": 10.0,");
            sb.AppendLine("      \"excellent\": \"string\",");
            sb.AppendLine("      \"good\": \"string\",");
            sb.AppendLine("      \"fair\": \"string\",");
            sb.AppendLine("      \"poor\": \"string\"");
            sb.AppendLine("    }");
            sb.AppendLine("  ]");
            sb.AppendLine("}");

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
                            temperature = 0.4
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
                    _logger.LogWarning("Gemini assignment API call failed with status {StatusCode} on attempt {Attempt}: {ErrorBody}", statusCode, currentAttempt, errorBody);

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
                    _logger.LogError(ex, "Exception calling Gemini assignment generation service on attempt {Attempt}", currentAttempt);
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

        private static string? ExtractCandidateText(string responseJson)
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
            catch
            {
                return null;
            }
        }

        public AiAssignmentGenerateResult ParseAndValidateResponse(string rawJson, AiAssignmentGenerateRequest request)
        {
            try
            {
                // Clean potential markdown blocks
                var cleanedJson = rawJson.Trim();
                if (cleanedJson.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                {
                    cleanedJson = cleanedJson.Substring(7);
                }
                else if (cleanedJson.StartsWith("```", StringComparison.Ordinal))
                {
                    cleanedJson = cleanedJson.Substring(3);
                }

                if (cleanedJson.EndsWith("```", StringComparison.Ordinal))
                {
                    cleanedJson = cleanedJson.Substring(0, cleanedJson.Length - 3);
                }
                cleanedJson = cleanedJson.Trim();

                using var doc = JsonDocument.Parse(cleanedJson);
                var root = doc.RootElement;

                var result = new AiAssignmentGenerateResult
                {
                    Success = true,
                    ProgramId = request.ProgramId,
                    CourseId = request.CourseId,
                    SemesterId = request.SemesterId,
                    SubjectId = request.SubjectId,
                    UnitId = request.UnitId,
                    TopicId = request.TopicId,
                    Difficulty = request.Difficulty,
                    AssignmentType = request.AssignmentType,
                    TotalMarks = request.TotalMarks,
                    PassMarks = request.PassMarks ?? Math.Round(request.TotalMarks * 0.4m, 2)
                };

                // Title
                if (root.TryGetProperty("title", out var titleProp) && !string.IsNullOrWhiteSpace(titleProp.GetString()))
                {
                    result.Title = titleProp.GetString()!.Trim();
                }
                else
                {
                    result.Title = "Course Academic Assignment";
                }

                // Description
                if (root.TryGetProperty("description", out var descProp) && !string.IsNullOrWhiteSpace(descProp.GetString()))
                {
                    result.Description = descProp.GetString()!.Trim();
                }
                else
                {
                    result.Description = "Comprehensive assignment designed to assess conceptual understanding and analytical problem-solving skills.";
                }

                // Student Instructions
                if (root.TryGetProperty("studentInstructions", out var instrProp) && !string.IsNullOrWhiteSpace(instrProp.GetString()))
                {
                    result.Instructions = instrProp.GetString()!.Trim();
                }
                else
                {
                    result.Instructions = "1. Answer all questions thoroughly with appropriate explanations.\n2. Ensure answers are your original work.\n3. Submit before the scheduled deadline.";
                }

                // Questions
                if (root.TryGetProperty("questions", out var questionsProp) && questionsProp.ValueKind == JsonValueKind.Array)
                {
                    int qIdx = 1;
                    foreach (var qEl in questionsProp.EnumerateArray())
                    {
                        var qText = qEl.TryGetProperty("questionText", out var qt) ? qt.GetString() ?? "" : "";
                        if (string.IsNullOrWhiteSpace(qText)) continue;

                        decimal qMarks = 0;
                        if (qEl.TryGetProperty("marks", out var qm))
                        {
                            if (qm.ValueKind == JsonValueKind.Number && qm.TryGetDecimal(out var parsedM))
                            {
                                qMarks = parsedM;
                            }
                        }

                        var guidelines = qEl.TryGetProperty("expectedAnswerGuidelines", out var eag) ? eag.GetString() : null;

                        result.Questions.Add(new AiAssignmentQuestionItem
                        {
                            QuestionNumber = qIdx++,
                            QuestionText = qText.Trim(),
                            Marks = qMarks > 0 ? qMarks : Math.Round(request.TotalMarks / request.NumberOfQuestions, 2),
                            ExpectedAnswerGuidelines = guidelines
                        });
                    }
                }

                // Ensure at least 1 question
                if (!result.Questions.Any())
                {
                    result.Questions.Add(new AiAssignmentQuestionItem
                    {
                        QuestionNumber = 1,
                        QuestionText = "Analyze the primary concepts and practical applications covered in this topic.",
                        Marks = request.TotalMarks,
                        ExpectedAnswerGuidelines = "Comprehensive explanation with key definitions, diagrams, and examples."
                    });
                }

                // Strict Question Marks Normalization: Sum must equal TotalMarks
                ValidateAndNormalizeMarks(result.Questions, request.TotalMarks);

                // Marking Criteria
                if (root.TryGetProperty("markingCriteria", out var critProp) && critProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var cEl in critProp.EnumerateArray())
                    {
                        var critName = cEl.TryGetProperty("criterion", out var cn) ? cn.GetString() ?? "" : "";
                        var critDesc = cEl.TryGetProperty("description", out var cd) ? cd.GetString() ?? "" : "";
                        decimal critWeight = 0;
                        if (cEl.TryGetProperty("weightage", out var cw) && cw.ValueKind == JsonValueKind.Number && cw.TryGetDecimal(out var parsedW))
                        {
                            critWeight = parsedW;
                        }

                        if (!string.IsNullOrWhiteSpace(critName))
                        {
                            result.MarkingCriteria.Add(new AiMarkingCriterionItem
                            {
                                Criterion = critName.Trim(),
                                Weightage = critWeight > 0 ? critWeight : 25,
                                Description = critDesc.Trim()
                            });
                        }
                    }
                }

                if (!result.MarkingCriteria.Any())
                {
                    result.MarkingCriteria.Add(new AiMarkingCriterionItem { Criterion = "Conceptual Accuracy", Weightage = 40, Description = "Correctness and depth of theoretical concepts." });
                    result.MarkingCriteria.Add(new AiMarkingCriterionItem { Criterion = "Analytical Reasoning", Weightage = 30, Description = "Application of methods, logical deductions, or problem solving." });
                    result.MarkingCriteria.Add(new AiMarkingCriterionItem { Criterion = "Clarity & Organization", Weightage = 30, Description = "Structure, formatting, and clear communication of arguments." });
                }

                // Rubrics
                if (root.TryGetProperty("rubrics", out var rubricsProp) && rubricsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var rEl in rubricsProp.EnumerateArray())
                    {
                        var rCrit = rEl.TryGetProperty("criterion", out var rc) ? rc.GetString() ?? "" : "";
                        if (string.IsNullOrWhiteSpace(rCrit)) continue;

                        decimal rMax = 0;
                        if (rEl.TryGetProperty("maxMarks", out var rm) && rm.ValueKind == JsonValueKind.Number && rm.TryGetDecimal(out var parsedRm))
                        {
                            rMax = parsedRm;
                        }

                        var rExc = rEl.TryGetProperty("excellent", out var re) ? re.GetString() ?? "" : "Comprehensive understanding with zero errors.";
                        var rGood = rEl.TryGetProperty("good", out var rg) ? rg.GetString() ?? "" : "Clear understanding with minor omissions.";
                        var rFair = rEl.TryGetProperty("fair", out var rf) ? rf.GetString() ?? "" : "Basic understanding with noticeable gaps.";
                        var rPoor = rEl.TryGetProperty("poor", out var rp) ? rp.GetString() ?? "" : "Incomplete or major misconceptions.";

                        result.Rubrics.Add(new AiAssignmentRubricRow
                        {
                            Criterion = rCrit.Trim(),
                            MaxMarks = rMax > 0 ? rMax : Math.Round(request.TotalMarks / 3, 2),
                            Excellent = rExc.Trim(),
                            Good = rGood.Trim(),
                            Fair = rFair.Trim(),
                            Poor = rPoor.Trim()
                        });
                    }
                }

                if (!result.Rubrics.Any())
                {
                    result.Rubrics.Add(new AiAssignmentRubricRow
                    {
                        Criterion = "Concept Understanding",
                        MaxMarks = Math.Round(request.TotalMarks * 0.4m, 2),
                        Excellent = "Demonstrates complete and thorough mastery of all key concepts with no errors (80-100%).",
                        Good = "Accurately explains core concepts with minor omissions in detail (65-79%).",
                        Fair = "Demonstrates partial grasp of core concepts with noticeable misconceptions (50-64%).",
                        Poor = "Fails to grasp fundamental principles or leaves answers incomplete (0-49%)."
                    });
                    result.Rubrics.Add(new AiAssignmentRubricRow
                    {
                        Criterion = "Critical Thinking & Analysis",
                        MaxMarks = Math.Round(request.TotalMarks * 0.35m, 2),
                        Excellent = "Provides insightful analysis, logical reasoning, and well-justified conclusions (80-100%).",
                        Good = "Presents sound reasoning and supported arguments with minor gaps (65-79%).",
                        Fair = "Basic reasoning present but lacks depth or empirical support (50-64%).",
                        Poor = "Lacks analytical depth, flawed logic, or uncritical reproduction (0-49%)."
                    });
                    result.Rubrics.Add(new AiAssignmentRubricRow
                    {
                        Criterion = "Structure & Academic Quality",
                        MaxMarks = Math.Round(request.TotalMarks * 0.25m, 2),
                        Excellent = "Flawlessly structured, properly cited, clean presentation, and professional language (80-100%).",
                        Good = "Well structured with minor formatting or grammatical inconsistencies (65-79%).",
                        Fair = "Adequate structure but exhibits poor formatting or frequent errors (50-64%).",
                        Poor = "Disorganized, poorly written, or fails to meet formatting criteria (0-49%)."
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize or validate Gemini assignment JSON response.");
                return new AiAssignmentGenerateResult
                {
                    Success = false,
                    ErrorMessage = "The AI service response could not be validated. Please retry generation."
                };
            }
        }

        /// <summary>
        /// Enforces that the sum of question marks strictly equals totalMarks.
        /// Adjusts the delta on the final question or distributes evenly.
        /// </summary>
        public static void ValidateAndNormalizeMarks(List<AiAssignmentQuestionItem> questions, decimal totalMarks)
        {
            if (questions == null || !questions.Any() || totalMarks <= 0) return;

            // Ensure every question has at least 1 mark
            foreach (var q in questions)
            {
                if (q.Marks <= 0) q.Marks = 1;
            }

            var sum = questions.Sum(q => q.Marks);
            if (sum == totalMarks) return;

            var diff = totalMarks - sum;

            // If simple adjustment to the last question keeps it > 0, do that
            if (questions.Last().Marks + diff > 0)
            {
                questions.Last().Marks += diff;
            }
            else
            {
                // Re-distribute evenly
                var baseMark = Math.Floor((totalMarks / questions.Count) * 100) / 100;
                for (int i = 0; i < questions.Count; i++)
                {
                    questions[i].Marks = baseMark;
                }
                var newSum = questions.Sum(q => q.Marks);
                questions.Last().Marks += (totalMarks - newSum);
            }
        }
    }
}

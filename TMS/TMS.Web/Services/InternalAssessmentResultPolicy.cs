namespace TMS.Web.Services;

public static class InternalAssessmentResultPolicy
{
    public const decimal PassingPercentage = 33m;

    public static bool IsPass(decimal obtainedMarks, decimal maximumMarks) =>
        maximumMarks > 0m &&
        obtainedMarks * 100m >= maximumMarks * PassingPercentage;

    public static string SubmissionStatus(bool hasSubmission, bool evaluated) =>
        !hasSubmission ? "ABSENT" : evaluated ? "SUBMITTED" : "PENDING";

    public static string AssessmentOutcome(
        bool resultsPublished,
        bool hasSubmission,
        bool evaluated,
        decimal? obtainedMarks,
        decimal maximumMarks)
    {
        if (!resultsPublished || (hasSubmission && !evaluated))
            return "PENDING";
        if (!hasSubmission)
            return "FAIL";
        return obtainedMarks.HasValue && IsPass(obtainedMarks.Value, maximumMarks)
            ? "PASS"
            : "FAIL";
    }
}

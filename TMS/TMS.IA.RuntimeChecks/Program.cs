using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using TMS.Repository;
using TMS.Web.Infrastructure;
using TMS.Web.Services;
using TMS.Web.ViewModels;

var passed = 0;
var failed = 0;

void Equal<T>(string name, T actual, T expected)
{
    if (EqualityComparer<T>.Default.Equals(actual, expected))
    {
        passed++;
        Console.WriteLine($"PASS | {name} | actual={actual} expected={expected}");
        return;
    }

    failed++;
    Console.WriteLine($"FAIL | {name} | actual={actual} expected={expected}");
}

void Throws<TException>(string name, Action action) where TException : Exception
{
    try
    {
        action();
        failed++;
        Console.WriteLine($"FAIL | {name} | expected={typeof(TException).Name}");
    }
    catch (TException)
    {
        passed++;
        Console.WriteLine($"PASS | {name} | expected exception observed");
    }
}

IConfiguration Config(params (string Key, string Value)[] values) =>
    new ConfigurationBuilder().AddInMemoryCollection(
        values.ToDictionary(x => x.Key, x => (string?)x.Value)).Build();

byte[] OpenXml(string requiredPart)
{
    using var stream = new MemoryStream();
    using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
    {
        foreach (var part in new[] { "[Content_Types].xml", requiredPart })
        {
            var entry = archive.CreateEntry(part);
            using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
            writer.Write("<xml />");
        }
    }
    return stream.ToArray();
}

Equal("33 percent boundary passes", InternalAssessmentResultPolicy.IsPass(33m, 100m), true);
Equal("Below 33 percent fails", InternalAssessmentResultPolicy.IsPass(32.99m, 100m), false);
Equal("Published non-submitter submission state", InternalAssessmentResultPolicy.SubmissionStatus(false, false), "ABSENT");
Equal("Published non-submitter outcome", InternalAssessmentResultPolicy.AssessmentOutcome(true, false, false, null, 100m), "FAIL");
Equal("Unpublished evaluated result remains pending", InternalAssessmentResultPolicy.AssessmentOutcome(false, true, true, 75m, 100m), "PENDING");
Equal("Incomplete published evaluation remains pending", InternalAssessmentResultPolicy.AssessmentOutcome(true, true, false, null, 100m), "PENDING");

var exactBoundary = new InternalSemesterGradeCardCourse
{
    PublishedAssessments = 1,
    EvaluatedAssessments = 1,
    Percentage = 33m
};
typeof(InternalSemesterGradeCardCourse).GetProperty("HasFailedAssessment")!.SetValue(exactBoundary, false);
Equal("Exact 33 percent subject boundary", exactBoundary.ResultStatus, "PASS");

var belowBoundary = new InternalSemesterGradeCardCourse
{
    PublishedAssessments = 1,
    EvaluatedAssessments = 1,
    Percentage = 32.99m
};
typeof(InternalSemesterGradeCardCourse).GetProperty("HasFailedAssessment")!.SetValue(belowBoundary, true);
Equal("Below 33 percent subject boundary", belowBoundary.ResultStatus, "FAIL");

var incomplete = new InternalSemesterGradeCardCourse
{
    PublishedAssessments = 2,
    EvaluatedAssessments = 1,
    PendingAssessments = 1,
    Percentage = null
};
typeof(InternalSemesterGradeCardCourse).GetProperty("HasFailedAssessment")!.SetValue(incomplete, false);
Equal("Incomplete subject", incomplete.ResultStatus, "PENDING");

var failedSemester = new InternalSemesterGradeCardViewModel();
failedSemester.Courses.Add(exactBoundary);
failedSemester.Courses.Add(belowBoundary);
Equal("Semester with one failed subject", failedSemester.ResultStatus, "FAIL");

var pendingSemester = new InternalSemesterGradeCardViewModel();
pendingSemester.Courses.Add(exactBoundary);
pendingSemester.Courses.Add(incomplete);
Equal("Semester with no failure and one incomplete subject", pendingSemester.ResultStatus, "PENDING");

var passedSemester = new InternalSemesterGradeCardViewModel();
passedSemester.Courses.Add(exactBoundary);
Equal("Semester with all subjects passed", passedSemester.ResultStatus, "PASS");

var development = new FakeHostEnvironment("Development");
Equal("Database initializer disabled by default", DatabaseInitializationGuard.ShouldRun(Config(), development), false);
Throws<InvalidOperationException>("Initializer API rejects an unconfirmed caller", () =>
    DatabaseInitializer.EnsureTablesCreatedAsync(null!).GetAwaiter().GetResult());
Throws<InvalidOperationException>("Initializer requires disposable confirmation", () =>
    DatabaseInitializationGuard.ShouldRun(Config(
        ("DatabaseInitialization:Enabled", "true"),
        ("DatabaseInitialization:ExpectedDatabaseName", "IA_Scratch"),
        ("ConnectionStrings:DefaultConnection", "Server=(localdb)\\MSSQLLocalDB;Database=IA_Scratch;Integrated Security=true")), development));
Throws<InvalidOperationException>("Initializer rejects database-name mismatch", () =>
    DatabaseInitializationGuard.ShouldRun(Config(
        ("DatabaseInitialization:Enabled", "true"),
        ("DatabaseInitialization:DisposableDatabaseConfirmed", "true"),
        ("DatabaseInitialization:ExpectedDatabaseName", "IA_Scratch"),
        ("ConnectionStrings:DefaultConnection", "Server=(localdb)\\MSSQLLocalDB;Database=Demo;Integrated Security=true")), development));
Equal("Initializer accepts explicitly matched disposable database", DatabaseInitializationGuard.ShouldRun(Config(
    ("DatabaseInitialization:Enabled", "true"),
    ("DatabaseInitialization:DisposableDatabaseConfirmed", "true"),
    ("DatabaseInitialization:ExpectedDatabaseName", "IA_Scratch"),
    ("ConnectionStrings:DefaultConnection", "Server=(localdb)\\MSSQLLocalDB;Database=IA_Scratch;Integrated Security=true")), development), true);
Throws<InvalidOperationException>("Initializer remains prohibited in Production", () =>
    DatabaseInitializationGuard.ShouldRun(Config(
        ("DatabaseInitialization:Enabled", "true"),
        ("DatabaseInitialization:DisposableDatabaseConfirmed", "true"),
        ("DatabaseInitialization:ExpectedDatabaseName", "IA_Scratch"),
        ("ConnectionStrings:DefaultConnection", "Server=(localdb)\\MSSQLLocalDB;Database=IA_Scratch;Integrated Security=true")), new FakeHostEnvironment("Production")));

var pdf = Encoding.ASCII.GetBytes("%PDF-1.7\nminimal\n%%EOF");
Equal("PDF signature and trailer accepted", await InternalAssessmentUploadValidator.IsValidAsync(new MemoryStream(pdf), pdf.Length, ".pdf"), true);
var fakePdf = Encoding.ASCII.GetBytes("not a pdf");
Equal("Renamed PDF rejected", await InternalAssessmentUploadValidator.IsValidAsync(new MemoryStream(fakePdf), fakePdf.Length, ".pdf"), false);
var docx = OpenXml("word/document.xml");
Equal("DOCX OOXML package accepted", await InternalAssessmentUploadValidator.IsValidAsync(new MemoryStream(docx), docx.Length, ".docx"), true);
Equal("DOCX cannot be submitted as PPTX", await InternalAssessmentUploadValidator.IsValidAsync(new MemoryStream(docx), docx.Length, ".pptx"), false);
var genericZip = OpenXml("payload.bin");
Equal("Generic ZIP renamed DOCX rejected", await InternalAssessmentUploadValidator.IsValidAsync(new MemoryStream(genericZip), genericZip.Length, ".docx"), false);
Equal("Oversize upload rejected before read", await InternalAssessmentUploadValidator.IsValidAsync(new MemoryStream(Array.Empty<byte>()), InternalAssessmentUploadValidator.MaximumBytes + 1, ".pdf"), false);

Console.WriteLine($"TOTAL | PASS={passed} FAIL={failed}");
return failed == 0 ? 0 : 1;

sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = environmentName;
    public string ApplicationName { get; set; } = "TMS.IA.RuntimeChecks";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

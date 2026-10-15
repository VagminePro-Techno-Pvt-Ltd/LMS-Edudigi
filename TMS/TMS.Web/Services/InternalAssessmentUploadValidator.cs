using System.IO.Compression;

namespace TMS.Web.Services;

public static class InternalAssessmentUploadValidator
{
    public const long MaximumBytes = 8L * 1024 * 1024;
    private const int MaximumArchiveEntries = 2_000;
    private const long MaximumExpandedBytes = 100L * 1024 * 1024;

    public static async Task<bool> IsValidAsync(
        Stream source,
        long length,
        string extension,
        CancellationToken cancellationToken = default)
    {
        if (length <= 0 || length > MaximumBytes || !source.CanRead)
            return false;

        await using var content = new MemoryStream((int)length);
        await source.CopyToAsync(content, cancellationToken);
        if (content.Length != length)
            return false;

        var bytes = content.GetBuffer().AsSpan(0, checked((int)content.Length));
        extension = extension.ToLowerInvariant();

        return extension switch
        {
            ".pdf" => HasPrefix(bytes, "%PDF-"u8) && ContainsNearEnd(bytes, "%%EOF"u8),
            ".png" => HasPrefix(bytes, new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) &&
                      HasSuffix(bytes, new byte[] { 73, 69, 78, 68, 174, 66, 96, 130 }),
            ".jpg" or ".jpeg" => bytes.Length >= 4 && bytes[0] == 0xff && bytes[1] == 0xd8 &&
                                    bytes[^2] == 0xff && bytes[^1] == 0xd9,
            ".docx" => IsValidOpenXml(content, "word/document.xml"),
            ".pptx" => IsValidOpenXml(content, "ppt/presentation.xml"),
            _ => false
        };
    }

    private static bool IsValidOpenXml(MemoryStream content, string requiredPart)
    {
        try
        {
            content.Position = 0;
            using var archive = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true);
            if (archive.Entries.Count == 0 || archive.Entries.Count > MaximumArchiveEntries)
                return false;

            long expandedBytes = 0;
            var hasContentTypes = false;
            var hasRequiredPart = false;
            foreach (var entry in archive.Entries)
            {
                expandedBytes += entry.Length;
                if (expandedBytes > MaximumExpandedBytes || entry.FullName.Contains("..", StringComparison.Ordinal))
                    return false;
                if (string.Equals(entry.FullName, "[Content_Types].xml", StringComparison.OrdinalIgnoreCase))
                    hasContentTypes = true;
                if (string.Equals(entry.FullName, requiredPart, StringComparison.OrdinalIgnoreCase))
                    hasRequiredPart = true;
            }

            return hasContentTypes && hasRequiredPart;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }

    private static bool HasPrefix(ReadOnlySpan<byte> content, ReadOnlySpan<byte> prefix) =>
        content.StartsWith(prefix);

    private static bool HasSuffix(ReadOnlySpan<byte> content, ReadOnlySpan<byte> suffix) =>
        content.EndsWith(suffix);

    private static bool ContainsNearEnd(ReadOnlySpan<byte> content, ReadOnlySpan<byte> marker)
    {
        var start = Math.Max(0, content.Length - 1_024);
        return content[start..].IndexOf(marker) >= 0;
    }
}

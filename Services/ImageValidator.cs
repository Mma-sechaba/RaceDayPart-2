namespace RaceDay.Api.Services;

public static class ImageValidator
{
    public const long MaximumBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    public static async Task<string?> ValidateAsync(IFormFile? file, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
            return "An image file is required.";
        if (file.Length > MaximumBytes)
            return "The image must not exceed 5 MB.";
        if (!SupportedExtensions.Contains(Path.GetExtension(file.FileName)))
            return "Only JPG, PNG, WEBP, and GIF images are supported.";

        var buffer = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(buffer.AsMemory(), cancellationToken);
        var valid = IsJpeg(buffer, read) || IsPng(buffer, read) || IsGif(buffer, read) || IsWebp(buffer, read);
        return valid ? null : "The uploaded file is not a valid JPG, PNG, WEBP, or GIF image.";
    }

    private static bool IsJpeg(byte[] b, int n) => n >= 3 && b[0] == 0xff && b[1] == 0xd8 && b[2] == 0xff;
    private static bool IsPng(byte[] b, int n) => n >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4e && b[3] == 0x47 && b[4] == 0x0d && b[5] == 0x0a && b[6] == 0x1a && b[7] == 0x0a;
    private static bool IsGif(byte[] b, int n) => n >= 6 && b[0] == 'G' && b[1] == 'I' && b[2] == 'F' && b[3] == '8' && (b[4] == '7' || b[4] == '9') && b[5] == 'a';
    private static bool IsWebp(byte[] b, int n) => n >= 12 && b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F' && b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P';
}

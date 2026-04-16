namespace DOJO2.Application.Common;

public sealed class FileUploadData
{
    public string FileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long Length { get; set; }
    public byte[] Content { get; set; } = Array.Empty<byte>();
}

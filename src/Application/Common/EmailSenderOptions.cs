using System.ComponentModel.DataAnnotations;

namespace DOJO2.Application.Common;

public sealed class EmailSenderOptions
{
    public const string SectionName = "EmailSender";

    [Required]
    [EmailAddress]
    public string FromAddress { get; set; } = "no-reply@dojo.example";

    [Required]
    [MinLength(1)]
    public string FromName { get; set; } = "DOJO Password Recovery";
}


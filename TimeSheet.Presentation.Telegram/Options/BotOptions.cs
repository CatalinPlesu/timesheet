using System.ComponentModel.DataAnnotations;
using TimeSheet.Core.Application.Options;

namespace TimeSheet.Presentation.Telegram.Options;

public class BotOptions : IOptionsWithSectionName
{
    public static string SectionName => "Bot";

    [Required(AllowEmptyStrings = false)]
    public required string Token { get; set; }
}

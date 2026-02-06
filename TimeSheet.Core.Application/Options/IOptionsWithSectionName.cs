namespace TimeSheet.Core.Application.Options;

public interface IOptionsWithSectionName
{
    static abstract string SectionName { get; }
}

namespace Contingent.Models.Domain.Groups;

public class GroupEducationFormat
{

    public string RussianName { get; private init; }
    public string GroupNamePostfix { get; private init; }
    public GroupEducationFormatTypes FormatType { get; private init; }

    private GroupEducationFormat()
    {

    }

    public static IReadOnlyCollection<GroupEducationFormat> ListOfFormats => new List<GroupEducationFormat>{
        new (){
            RussianName = "Не указано",
            GroupNamePostfix = string.Empty,
            FormatType = GroupEducationFormatTypes.NotMentioned,
        },
        new (){
            RussianName = "Очная",
            GroupNamePostfix = string.Empty,
            FormatType = GroupEducationFormatTypes.FullTime,
        },
        new (){
            RussianName = "Заочная",
            GroupNamePostfix = "з",
            FormatType = GroupEducationFormatTypes.Extramural,
        },
        new (){
            RussianName = "Очно-заочная",
            GroupNamePostfix = "зк",
            FormatType = GroupEducationFormatTypes.PartTime,
        },
    };

    public static bool TryGetByTypeCode(int code, out GroupEducationFormat? type)
    {
        type = ListOfFormats.FirstOrDefault(x => (int)x!.FormatType == code, null);
        return type is not null;
    }
    public static GroupEducationFormat GetByTypeCode(int code)
    {
        return ListOfFormats.Where(x => (int)x.FormatType == code).First();
    }

    public bool IsDefined()
    {
        return FormatType != GroupEducationFormatTypes.NotMentioned;
    }
}


public enum GroupEducationFormatTypes
{
    NotMentioned = -1,
    FullTime = 1,
    Extramural = 2,
    PartTime = 3,
}

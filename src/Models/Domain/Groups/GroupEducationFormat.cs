namespace Contingent.Models.Domain.Groups;

public class GroupEducationFormat
{
    public string RussianName { get; private init; }
    public string GroupNamePostfix { get; private init; }
    public GroupEducationFormatTypes FormatType { get; private init; }

    private GroupEducationFormat()
    {
        RussianName = string.Empty;
        GroupNamePostfix = string.Empty;
        FormatType = GroupEducationFormatTypes.NotMentioned;
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
    public static GroupEducationFormat? GetByTypeName(string? name)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
        {
            return ListOfFormats.First(x => x.FormatType == GroupEducationFormatTypes.NotMentioned);
        }
        string correctName = name.Trim().ToLower();
        return ListOfFormats.FirstOrDefault(x => x.RussianName.ToLower() == correctName);
    }

    public static bool TryGetByTypeCode(int code, out GroupEducationFormat? type)
    {
        type = ListOfFormats.FirstOrDefault(x => (int)x!.FormatType == code, null);
        return type is not null;
    }
    public static GroupEducationFormat GetByTypeCode(int code)
    {
        return ListOfFormats.First(x => (int)x.FormatType == code);
    }

    public bool IsDefined()
    {
        return FormatType != GroupEducationFormatTypes.NotMentioned;
    }
    public static bool operator ==(GroupEducationFormat left, GroupEducationFormat right)
    {
        return left.FormatType == right.FormatType;
    }
    public static bool operator !=(GroupEducationFormat left, GroupEducationFormat right)
    {
        return left.FormatType != right.FormatType;
    }

}


public enum GroupEducationFormatTypes
{
    NotMentioned = -1,
    FullTime = 1,
    Extramural = 2,
    PartTime = 3,
}

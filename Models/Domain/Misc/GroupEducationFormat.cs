namespace StudentTracking.Models.Domain.Misc;

public class GroupEducationFormat {
    public enum Formats {
        NotMentioned = -1,
        FullTime = 1,
        Extramural = 2,
        PartTime = 3,
    }

    public static readonly Dictionary<Formats, (string name, string postfix)> Names = new Dictionary<Formats, (string name, string postfix)> {
        {Formats.NotMentioned, ("Не указано", "")},
        {Formats.FullTime, ("Очная", "")},
        {Formats.Extramural, ("Заочная", "з")},
        {Formats.PartTime, ("Очно-заочная", "зк")},
    };
}

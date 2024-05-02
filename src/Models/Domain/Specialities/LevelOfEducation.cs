namespace StudentTracking.Models.Domain.Specialities;
public class LevelOfEducation
{

    public string RussianName { get; protected init; }
    public LevelsOfEducation LevelCode { get; protected init; }
    public int Weight { get; protected init; }

    private string[] _aliases;

    public static LevelOfEducation None => ListOfLevels.First();

    public static IReadOnlyCollection<LevelOfEducation> ListOfLevels => new List<LevelOfEducation>{
        new LevelOfEducation(
            LevelsOfEducation.NotMentioned,
            "Не указан",
            -1,
            new [] {"", "нет"}
        ),
        new LevelOfEducation(
            LevelsOfEducation.BasicGeneralEducation,
            "Основное общее образование",
            1,
            new string[]{"ооо", "основное общее"}
            )
        ,
        new LevelOfEducation(LevelsOfEducation.SecondaryGeneralEducation,
            "Среднее общее образование",
            2,
            new string[]{"соо", "среднее общее"}),
        new LevelOfEducation(
            LevelsOfEducation.SecondaryVocationalEducation,
            "Среднее профессиональное образование",
            3,
            new string[]{"спо", "среднее профессиональное"}
            )
    };

    protected LevelOfEducation(LevelsOfEducation level, string russianName, int weight, string[] aliases)
    {
        LevelCode = level;
        RussianName = russianName;
        _aliases = aliases;
        Weight = weight;
    }

    public static LevelOfEducation GetByLevelCode(int code)
    {
        return ListOfLevels.Where(x => (int)x.LevelCode == code).First();
    }
    public static bool TryGetByLevelCode(int code, out LevelOfEducation? type)
    {
        type = ListOfLevels.FirstOrDefault(x => (int)x.LevelCode == code, null);
        return type is not null;
    }

    public static bool operator ==(LevelOfEducation left, LevelOfEducation rigth)
    {
        return left.LevelCode == rigth.LevelCode;
    }
    public static bool operator !=(LevelOfEducation left, LevelOfEducation rigth)
    {
        return !(left == rigth);
    }
    public static bool operator >=(LevelOfEducation left, LevelOfEducation rigth)
    {
        return left.Weight >= rigth.Weight;
    }
    public static bool operator <=(LevelOfEducation left, LevelOfEducation rigth)
    {
        return left.Weight <= rigth.Weight;
    }

    public static int ImportLevelCode(string? name)
    {
        if (name is null)
        {
            return (int)LevelsOfEducation.NotMentioned;
        }
        var found = ListOfLevels.FirstOrDefault(t => t.RussianName.ToLower() == name.ToLower() || t._aliases.Any(x => x.ToLower() == name.ToLower()), null);
        if (found is not null)
        {
            return (int)found.LevelCode;
        }
        return (int)LevelsOfEducation.NotMentioned;
    }
    public bool IsDefined()
    {
        return LevelCode != LevelsOfEducation.NotMentioned;
    }
}


public enum LevelsOfEducation
{
    NotMentioned = -1,
    BasicGeneralEducation = 1, // основное общее образование
    SecondaryGeneralEducation = 2, // среднее общее образование
    SecondaryVocationalEducation = 3, // среднее профессиональное образование
}
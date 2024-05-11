
namespace Contingent.Models.Domain.Specialties;

public class TeachingDepth
{

    public TeachingDepthLevels Level { get; private init; }
    public string RussianName { get; private init; }

    public static TeachingDepth None => new TeachingDepth(TeachingDepthLevels.NotMentioned, "Не укзаано");

    private TeachingDepth(TeachingDepthLevels level, string russianName)
    {
        Level = level;
        RussianName = russianName;
    }

    public static IReadOnlyCollection<TeachingDepth> Levels => new List<TeachingDepth>{
        new TeachingDepth(TeachingDepthLevels.NotMentioned, "Не указано"),
        new TeachingDepth(TeachingDepthLevels.Common, "Базовый"),
        new TeachingDepth(TeachingDepthLevels.Common, "Углубленный")
    };

    public static TeachingDepth GetByTypeCode(int code)
    {
        return Levels.Where(x => (int)x.Level == code).First();
    }
    public static bool TryGetByTypeCode(int code)
    {
        return Levels.Any(x => (int)x.Level == code);

    }

    public static int ImportTeachingDepthCode(string? name)
    {
        if (name is null)
        {
            return (int)TeachingDepthLevels.NotMentioned;
        }
        var lower = name.ToLower();
        var found = Levels.FirstOrDefault(t => t.RussianName.ToLower() == lower, null);
        if (found is null)
        {
            return (int)TeachingDepthLevels.NotMentioned;
        }
        return (int)found.Level;
    }
}


public enum TeachingDepthLevels
{
    NotMentioned = 0,
    Common = 1,
    Advanced = 2
}
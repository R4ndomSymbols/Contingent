using System.Security.Cryptography.X509Certificates;

namespace StudentTracking.Models.Domain.Misc;

public class TeachingDepth {

    public TeachingDepthLevels Level {get; private init; }
    public string RussianName {get; private init; }

    public static TeachingDepth None => new TeachingDepth(TeachingDepthLevels.NotMentioned, "Не укзаано");

    private TeachingDepth(TeachingDepthLevels level, string russianName){
        Level = level;
        RussianName = russianName;
    }    

    public static IReadOnlyCollection<TeachingDepth> Levels => new List<TeachingDepth>{
        new TeachingDepth(TeachingDepthLevels.NotMentioned, "Не указано"),
        new TeachingDepth(TeachingDepthLevels.Common, "Базовый"),
        new TeachingDepth(TeachingDepthLevels.Common, "Углубленный")
    };

    public static TeachingDepth GetByTypeCode(int code){
        return Levels.Where(x => (int)x.Level == code).First();
    }
    public static bool TryGetByTypeCode(int code){
        return Levels.Any( x => (int)x.Level == code);

    }


}


public enum TeachingDepthLevels {
    NotMentioned = 0,
    Common = 1,
    Advanced = 2
}
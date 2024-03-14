namespace StudentTracking.Models.Domain.Misc;


public class TrainingProgram {

    public string Name {get; private set;}
    public TrainingProgramTypes Type {get; private set;}

    private TrainingProgram(string name, TrainingProgramTypes type ){
        Name = name;
        Type = type;
    } 
    public static IReadOnlyCollection<TrainingProgram> Types = new List<TrainingProgram>() {
        new TrainingProgram("Программа подготовки квалифицированных рабочих, служащих", TrainingProgramTypes.QualifiedWorker),
        new TrainingProgram("Программы подготовки специалистов среднего звена", TrainingProgramTypes.GenericSpecialist),
        new TrainingProgram("Не указано", TrainingProgramTypes.NotMentioned)
    }.AsReadOnly();

    public static bool TryGetByType(int type, out TrainingProgram? result){
        var found = Types.Where(t => (int)t.Type == type); 
        if (found.Any()){
            result = found.First();
            return true;
        }
        else
        {
            result = null;
            return false;
        }
        
    }
    public static TrainingProgram GetByType(int type){
        var result = TryGetByType(type, out TrainingProgram? toReturn);
        if (result){
            return toReturn;
        }
        else {
            throw new Exception("Тип подготовки не определен");
        }
    }
} 


public enum TrainingProgramTypes {
    NotMentioned = 0,
    QualifiedWorker = 1,
    GenericSpecialist = 2,
}

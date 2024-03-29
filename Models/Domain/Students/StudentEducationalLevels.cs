using Npgsql;
using StudentTracking.Controllers.DTO.In;
using Utilities;

namespace StudentTracking.Models.Domain.Misc;

public class StudentEducationalLevelRecord {

    private LevelOfEducation _level;
    public string RussianName => _level.RussianName;
    public LevelOfEducation Level => _level;
    public StudentModel Owner {get; set;}
    private StudentEducationalLevelRecord(LevelOfEducation lvl, StudentModel owner)
    {
       _level = lvl;
       Owner = owner;
    }

    public static Result<StudentEducationalLevelRecord?> Create(StudentEducationRecordDTO dto, StudentModel model){
        
        if (dto == null){
            return Result<StudentEducationalLevelRecord>.Failure(new ValidationError("general", "DTO не может быть null"));
        }
        if (model is null){
            return Result<StudentEducationalLevelRecord>.Failure(new ValidationError(nameof(Owner), "Студент указан неверно"));
        }
        if (!LevelOfEducation.TryGetByLevelCode(dto.Level)){
            return Result<StudentEducationalLevelRecord>.Failure(new ValidationError(nameof(Level), "Неверный тип записи об обучении"));
        }
        var created = new StudentEducationalLevelRecord(LevelOfEducation.GetByLevelCode(dto.Level), model);
       
        return Result<StudentEducationalLevelRecord>.Success(created);
    }

    public async Task SaveRecord(ObservableTransaction? scope = null){

        var byStudent = await GetByOwner(Owner);
        // один и тот же тег не может быть записан на студента дважды
        if (byStudent.Any(x => x == this)){
            throw new Exception("Такая запись об образовании уже существует");
        }
        var cmdText = "INSERT INTO education_tag_history( " +
                " student_id, level_code) VALUES (@p1, @p2)";
        NpgsqlCommand cmd;
        using var conn = await Utils.GetAndOpenConnectionFactory();
        if (scope is null){
            cmd = new NpgsqlCommand(cmdText, conn); 
        }
        else {
            cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", (int)Owner.Id));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p2", (int)_level.LevelCode));

        await cmd.ExecuteNonQueryAsync();
    }
    public static async Task<IReadOnlyCollection<StudentEducationalLevelRecord>> GetByOwner(StudentModel? owner){
        if (owner is null){
            return new List<StudentEducationalLevelRecord>();
        }
        using var conn = await Utils.GetAndOpenConnectionFactory();
        using var command = new NpgsqlCommand("SELECT * FROM education_tag_history WHERE student_id = @p1", conn);
        command.Parameters.Add(new NpgsqlParameter<int>("p1", (int)owner.Id));
        using var reader = await command.ExecuteReaderAsync();
        var found = new List<StudentEducationalLevelRecord>();
        if (!reader.HasRows)
        {
            return found;
        }
        while (reader.Read())
        {   
            found.Add(new StudentEducationalLevelRecord(
                LevelOfEducation.GetByLevelCode((int)reader["level_code"]), owner));
        }
        return found;
    }

    public static bool operator == (StudentEducationalLevelRecord left, StudentEducationalLevelRecord right){
        if (left is null || right is null){
            return false;
        }
        else {
            return left.Owner == right.Owner && left._level == right._level;
        }
    }
    public static bool operator != (StudentEducationalLevelRecord left, StudentEducationalLevelRecord right){
        return !(left == right);
    }
}

public class LevelOfEducation {

    public string RussianName {get; protected init;}
    public LevelsOfEducation LevelCode {get; protected init;}
    public int Weight {get; protected init; }

    public static IReadOnlyCollection<LevelOfEducation> ListOfLevels => new List<LevelOfEducation>{
        new LevelOfEducation(LevelsOfEducation.NotMentioned, "Не указан"){
            Weight = -1,
        },
        new LevelOfEducation(LevelsOfEducation.BasicGeneralEducation, "Основное общее образование"){
            Weight = 1
        },
        new LevelOfEducation(LevelsOfEducation.SecondaryGeneralEducation, "Среднее общее образование")
        {
            Weight = 2
        },
        new LevelOfEducation(LevelsOfEducation.SecondaryVocationalEducation, "Среднее профессиональное образование"){
            Weight = 3
        }
    };

    protected LevelOfEducation(LevelsOfEducation level, string russianName){
        LevelCode = level;
        RussianName = russianName;
    }

    public static LevelOfEducation GetByLevelCode(int code){
        return ListOfLevels.Where(x => (int)x.LevelCode == code).First();
    }
    public static bool TryGetByLevelCode(int code){
        return ListOfLevels.Any(x => (int)x.LevelCode == code);
    }

    public static bool operator == (LevelOfEducation left, LevelOfEducation rigth){
        return left.LevelCode == rigth.LevelCode;
    }
    public static bool operator != (LevelOfEducation left, LevelOfEducation rigth){
        return !(left == rigth);
    }
}


public enum LevelsOfEducation {
    NotMentioned = -1,
    BasicGeneralEducation = 1, // основное общее образование
    SecondaryGeneralEducation = 2, // среднее общее образование
    SecondaryVocationalEducation = 3, // среднее профессиональное образование
}
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Serialization;
using Npgsql;
using StudentTracking.Controllers.DTO.In;
using Utilities;

namespace StudentTracking.Models.Domain.Misc;

public class StudentEducationalLevelRecord : EducationLevel{

    private StudentEducationalLevelRecord(EducationalLevelTypes level) : base(level)
    {
       
    }

    public int OwnerId { get; set; }
    public EducationalLevelTypes Recorded {get; init;} 

    public static Result<StudentEducationalLevelRecord?> Create(StudentEducationRecordDTO dto){
        if (dto == null){
            return Result<StudentEducationalLevelRecord>.Failure(new ValidationError("general", "DTO не может быть null"));
        }
        if (!Enum.TryParse(typeof(EducationalLevelTypes), dto.Level.ToString(), out object? parsed)){
            return Result<StudentEducationalLevelRecord>.Failure(new ValidationError(nameof(Recorded), "Неверный тип записи об обучении"));
        }
        var created = new StudentEducationalLevelRecord((EducationalLevelTypes)dto.Level){OwnerId = dto.StudentId};
       
        return Result<StudentEducationalLevelRecord>.Success(created);
    }

    public async Task SaveRecord(ObservableTransaction? scope = null){

        var byStudent = await GetByOwnerId(OwnerId);
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
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", OwnerId));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p2", (int)Recorded));

        await cmd.ExecuteNonQueryAsync();
    }
    public static async Task<IReadOnlyCollection<StudentEducationalLevelRecord>> GetByOwnerId(int ownerId){
        using var conn = await Utils.GetAndOpenConnectionFactory();
        using var command = new NpgsqlCommand("SELECT * FROM education_tag_history WHERE student_id = @p1", conn);
        command.Parameters.Add(new NpgsqlParameter<int>("p1", ownerId));
        using var reader = await command.ExecuteReaderAsync();
        var found = new List<StudentEducationalLevelRecord>();
        if (!reader.HasRows)
        {
            return found;
        }
        while (reader.Read())
        {
            found.Add(new StudentEducationalLevelRecord((EducationalLevelTypes)(int)reader["level_code"])
            {
                OwnerId = ownerId
            });
        }
        return found;
    }

    public static bool operator == (StudentEducationalLevelRecord left, StudentEducationalLevelRecord right){
        if (left is null || right is null){
            return false;
        }
        else {
            return left.OwnerId == right.OwnerId && left.Recorded == right.Recorded;
        }
    }
    public static bool operator != (StudentEducationalLevelRecord left, StudentEducationalLevelRecord right){
        return !(left == right);
    }
}

public class EducationLevel {

    public enum EducationalLevelTypes {
        NotMentioned = -1,
        BasicGeneralEducation = 1, // основное общее образование
        SecondaryGeneralEducation = 2, // среднее общее образование
        SecondaryVocationalEducation = 3, // среднее профессиональное образование
    }
    private static readonly Dictionary<EducationalLevelTypes, string> _names = new Dictionary<EducationalLevelTypes, string>(){

        {EducationalLevelTypes.NotMentioned, "Не указан"},
        {EducationalLevelTypes.BasicGeneralEducation, "Основное общее образование"},
        {EducationalLevelTypes.SecondaryGeneralEducation, "Среднее общее образование"},
        {EducationalLevelTypes.SecondaryVocationalEducation, "Среднее профессиональное образование"}

    };

    public EducationalLevelTypes Level {get; init;} 

    protected EducationLevel(EducationalLevelTypes level){
        Level = level;
    }

    public virtual string GetLevelName(){
        return _names[Level];
    }

    public static IReadOnlyCollection<EducationLevel> GetAllLevels(){
        return _names.Select(x => new EducationLevel(x.Key)).ToList().AsReadOnly();  
    }

}

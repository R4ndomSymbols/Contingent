using System.Text.Json;
using Npgsql;
using Utilities;

namespace StudentTracking.Models.Domain.Misc;

public class StudentEducationalLevelRecord {

    public enum EducationalLevels {
        NotMentioned = -1,
        BasicGeneralEducation = 1, // основное общее образование
        SecondaryGeneralEducation = 2, // среднее общее образование
        SecondaryVocationalEducation = 3, // среднее профессиональное образование
    }
    public static readonly Dictionary<EducationalLevels, string> Names = new Dictionary<EducationalLevels, string>(){

        {EducationalLevels.NotMentioned, "Не указан"},
        {EducationalLevels.BasicGeneralEducation, "Основное общее образование"},
        {EducationalLevels.SecondaryGeneralEducation, "Среднее общее образование"},
        {EducationalLevels.SecondaryVocationalEducation, "Среднее профессиональное образование"}

    };


    public int OwnerId { get; set; }
    public EducationalLevels Recorded {get; set;} 

    public StudentEducationalLevelRecord()
    {
        OwnerId = Utils.INVALID_ID;
        Recorded = EducationalLevels.NotMentioned;
    }

    public static void SaveRecord(StudentEducationalLevelRecord toSave){
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            using (var command = new NpgsqlCommand("INSERT INTO education_tag_history( " +
                    " student_id, level_code) VALUES (@p1, @p2)", conn)
            {
                Parameters = {
                        new("p1", toSave.OwnerId),
                        new("p2", toSave.Recorded),
                }
            })
            {
                command.ExecuteNonQuery();
            }
        }
    }
    public static List<StudentEducationalLevelRecord>? GetByOwnerId(int ownerId){
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            using (var command = new NpgsqlCommand("SELECT * FROM education_tag_history WHERE student_id = @p1", conn)
            {
                Parameters = {
                    new("p1", ownerId),
                }
            })
            {
                var reader = command.ExecuteReader();
                if (!reader.HasRows){
                    return null;
                }
                var found = new List<StudentEducationalLevelRecord>();
                while(reader.Read()){
                    found.Add(new StudentEducationalLevelRecord{
                        OwnerId = ownerId,
                        Recorded = (EducationalLevels)(int)reader["level_code"],
                    });
                }
                return found;
            }
        }
    }

}

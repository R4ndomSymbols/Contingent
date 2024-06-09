using Npgsql;
using Contingent.Utilities;

namespace Contingent.Models.Domain.Students;


public class StudentStateRecord
{

    public enum States
    {

        NotMentioned = -1,
        Working = 1,
        ContinuedLearning = 2,
        InArmy = 3,

    }

    public static Dictionary<States, string> Names = new Dictionary<States, string>(){
        {States.NotMentioned, "Не указано"},
        {States.Working, "Трудоустроен"},
        {States.InArmy, "В армии"},
        {States.ContinuedLearning, "Продолжает обучение"}
    };

    public int OwnerId { get; set; }
    public DateTime RecordedOn { get; set; }
    public States StateRecorded { get; set; }

    public StudentStateRecord()
    {
        OwnerId = Utils.INVALID_ID;
        RecordedOn = DateTime.Now;
        StateRecorded = States.NotMentioned;
    }

    public static async Task AddStateToHistory(StudentStateRecord toSave)
    {
        await using (var conn = await Utils.GetAndOpenConnectionFactory())
        {
            await using (var command = new NpgsqlCommand("INSERT INTO student_states( " +
                " student_id, status_code, recorded_on) " +
                " VALUES (@p1, @p2, @p3)", conn)
            {
                Parameters = {
                        new("p1", toSave.OwnerId),
                        new("p2", toSave.StateRecorded),
                        new("p3", toSave.RecordedOn),
                    }
            })
            {
                await command.ExecuteNonQueryAsync();
            }
        }
    }
    public static async Task<List<StudentStateRecord>?> GetByOwnerId(int ownerId)
    {
        await using (var conn = await Utils.GetAndOpenConnectionFactory())
        {
            await using (var command = new NpgsqlCommand("SELECT * FROM student_states WHERE student_id = @p1", conn)
            {
                Parameters = {
                    new("p1", ownerId),
                }
            })
            {
                using var reader = await command.ExecuteReaderAsync();
                if (!reader.HasRows)
                {
                    return null;
                }
                var found = new List<StudentStateRecord>();
                while (await reader.ReadAsync())
                {
                    found.Add(new StudentStateRecord
                    {
                        OwnerId = ownerId,
                        RecordedOn = (DateTime)reader["recorded_on"],
                        StateRecorded = (States)(int)reader["status_code"],
                    });
                }
                return found;
            }
        }
    }
}
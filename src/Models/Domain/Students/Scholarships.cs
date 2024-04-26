using Npgsql;
using Utilities;
namespace StudentTracking.Models.Domain.Misc;


public class Scholarship
{

    public enum ScolarshipTypes
    {

        NotMentioned = -1,
        StateAcademic = 1,
        StateSocial = 2,
        FromGovernment = 3,
        Personalized = 4,

    }
    public static readonly Dictionary<ScolarshipTypes, string> Names = new Dictionary<ScolarshipTypes, string>(){

        {ScolarshipTypes.NotMentioned, "Не указан"},
        {ScolarshipTypes.StateAcademic, "Академическая"},
        {ScolarshipTypes.StateSocial, "Социальная"},
        {ScolarshipTypes.FromGovernment, "От правительства РФ"},
        {ScolarshipTypes.Personalized, "Индивидуальная"},

    };


    public int Id {get; set;}
    public int OwnerId { get; set; }
    public ScolarshipTypes Type { get; set; }
    public DateTime? InitialDate { get; set; }
    public DateTime? EndDate { get; set; }

    public Scholarship()
    {
        Type = ScolarshipTypes.NotMentioned;
        InitialDate = null;
        EndDate = null;
        OwnerId = Utils.INVALID_ID;
        Id = Utils.INVALID_ID;
    }

    public async Task<int> RegisterOrUpdateScholarsip(Scholarship toSave)
    {
        await using (var conn = await Utils.GetAndOpenConnectionFactory())
        {
            if (Id == Utils.INVALID_ID){
               await using (var command = new NpgsqlCommand("INSERT INTO scholarship( " +
                    " student_id, scholarship_type, initial_date, end_date) " +
                    " VALUES (@p1, @p2, @p3, @p4) RETURNING id", conn)
            {
                Parameters = {
                    new ("p1", toSave.OwnerId),
                    new ("p2", toSave.Type),
                    new ("p3", toSave.InitialDate == null ? DBNull.Value : (DateTime)toSave.InitialDate),
                    new ("p4", toSave.EndDate == null ? DBNull.Value : (DateTime)toSave.EndDate),
                }
            })
            {
                using var reader = await command.ExecuteReaderAsync();
                await reader.ReadAsync();
                return (int)reader["id"];
            }
            }
            else {
                using (var command = new NpgsqlCommand("UPDATE scholarship " +
                    " SET student_id=@p1, scholarship_type=@p2, initial_date=@p3, end_date=@p4 " +
                    " WHERE id = @p5 ", conn)
            {
                Parameters = {
                    new ("p1", toSave.OwnerId),
                    new ("p2", toSave.Type),
                    new ("p3", toSave.InitialDate == null ? DBNull.Value : (DateTime)toSave.InitialDate),
                    new ("p4", toSave.EndDate == null ? DBNull.Value : (DateTime)toSave.EndDate),
                    new ("p2", toSave.Id),
                }
            })
            {
                command.ExecuteNonQuery();
                return toSave.Id;
            }
            }
        }
    }

    public async Task<List<Scholarship>?> GetScholarshipsByOwnerId(int ownerId)
    {
        await using (var conn = await Utils.GetAndOpenConnectionFactory()){
            await using (var command = new NpgsqlCommand("SELECT * FROM scholarship WHERE student_id = @p1", conn){
                Parameters = {
                    new ("p1",ownerId),
                }
            }){
                using var reader = await command.ExecuteReaderAsync();
                if (!reader.HasRows){
                    return null;
                }
                var found = new List<Scholarship>();
                while (await reader.ReadAsync()){
                    found.Add(new Scholarship(){
                        Id = (int)reader["id"],
                        OwnerId = ownerId,
                        InitialDate = reader["initial_date"].GetType() == typeof(DBNull) ? null : (DateTime)reader["initial_date"],
                        EndDate = reader["end_date"].GetType() == typeof(DBNull) ? null : (DateTime)reader["end_date"],

                    });
                }
                return found;
            }

        }
    }
}
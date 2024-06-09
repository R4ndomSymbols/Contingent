using Npgsql;
using Contingent.Utilities;
namespace Contingent.Models.Domain.Students;


public class MedicalReference
{

    public enum StatusTypes
    {
        NotMentioned = -1,
        DisabledPerson = 1,
        DisabledChild = 2,
        LimitedHealthPossibilities = 3

    }
    public enum HealthDisorderTypes
    {
        NotMentioned = -1,
        Deaf = 1, // глухой
        HearingImpaired = 2, // слабослышащий
        Blind = 3, // слепой
        VisuallyImpaired = 4, //слабовидящий
        MusculoskeletalDisorder = 5, // нарушение опорно-двигательного аппарата
        ImpairedMentalFunction = 6, // с задержкой психического развития
        SevereSpeechImpairment = 7, // тяжелое нарушение речи
        AutismSpectrumDisorder = 8, // расстройство аутистического спектра
        MentalRetardation = 9, // умственная отсталось
    }

    public static readonly Dictionary<HealthDisorderTypes, string> Names = new Dictionary<HealthDisorderTypes, string>{

        {HealthDisorderTypes.NotMentioned, "Не указано"},
        {HealthDisorderTypes.Deaf, "Глухой"},
        {HealthDisorderTypes.HearingImpaired, "Слабослышащий"},
        {HealthDisorderTypes.Blind, "Слепой"},
        {HealthDisorderTypes.VisuallyImpaired, "Слабовидящий"},
        {HealthDisorderTypes.MusculoskeletalDisorder, "Нарушения опорно-двигательного аппарата"},
        {HealthDisorderTypes.ImpairedMentalFunction, "Задержка психического развития"},
        {HealthDisorderTypes.SevereSpeechImpairment, "Тяжелые нарушения речи"},
        {HealthDisorderTypes.AutismSpectrumDisorder, "Расстройство аутистического спектра"},
        {HealthDisorderTypes.MentalRetardation, "Умственная отсталость"}
    };


    public int Id { get; set; }
    public int OwnerId { get; set; }
    public StatusTypes StatusTypeId { get; set; }
    public HealthDisorderTypes DisorderTypeId { get; set; }
    public DateTime? InitialDate { get; set; }
    public DateTime? EndDate { get; set; }

    public MedicalReference()
    {
        Id = Utils.INVALID_ID;
        OwnerId = Utils.INVALID_ID;
        StatusTypeId = StatusTypes.NotMentioned;
        DisorderTypeId = HealthDisorderTypes.NotMentioned;
        InitialDate = null;
    }

    public static async Task<int> SaveOrUpdateReference(MedicalReference toSave)
    {
        await using (var conn = await Utils.GetAndOpenConnectionFactory())
        {
            if (toSave.Id == Utils.INVALID_ID)
            {
                await using (var command = new NpgsqlCommand("INSERT INTO health_status( " +
                         " person_id, initial_date, end_date, status_type, health_disorder_type) " +
                         " VALUES (@p1, @p2, @p3, @p4, @p5) RETURNING id", conn)
                {
                    Parameters = {
                        new ("p1", toSave.OwnerId),
                        new ("p2", toSave.InitialDate == null ? DBNull.Value : (DateTime)toSave.InitialDate),
                        new ("p3", toSave.EndDate == null ? DBNull.Value : (DateTime)toSave.EndDate),
                        new ("p4", (int)toSave.StatusTypeId),
                        new ("p5", (int)toSave.DisorderTypeId),
                    }
                })
                {
                    using var reader = command.ExecuteReader();
                    return (int)reader["id"];
                }
            }
            else
            {
                using (var command = new NpgsqlCommand("UPDATE health_status " +
                " person_id=@p1, initial_date=@p2, end_date=@p3, status_type=@p4, health_disorder_type=@p5 " +
                " WHERE id = @p6", conn)
                {
                    Parameters = {
                        new ("p1", toSave.OwnerId),
                        new ("p2", toSave.InitialDate == null ? DBNull.Value : (DateTime)toSave.InitialDate),
                        new ("p3", toSave.EndDate == null ? DBNull.Value : (DateTime)toSave.EndDate),
                        new ("p4", (int)toSave.StatusTypeId),
                        new ("p5", (int)toSave.DisorderTypeId),
                        new ("p6", toSave.Id),
                    }
                })
                {
                    command.ExecuteNonQuery();
                    return toSave.Id;
                }
            }
        }
    }

    public async Task<List<MedicalReference>?> GetByOwnerId(int ownerId)
    {
        await using (var conn = await Utils.GetAndOpenConnectionFactory())
        {
            await using (var command = new NpgsqlCommand("SELECT * FROM health_status WHERE person_id = @p1", conn)
            {
                Parameters = {
                    new ("p1", ownerId)
                }
            })
            {
                using var reader = await command.ExecuteReaderAsync();
                if (!reader.HasRows)
                {
                    return null;
                }
                var toReturn = new List<MedicalReference>();
                while (await reader.ReadAsync())
                {
                    toReturn.Add(new MedicalReference()
                    {
                        Id = (int)reader["id"],
                        OwnerId = (int)reader["person_id"],
                        InitialDate = reader["initial_date"].GetType() == typeof(DBNull) ? null : (DateTime)reader["initial_date"],
                        EndDate = reader["end_date"].GetType() == typeof(DBNull) ? null : (DateTime)reader["end_date"],
                        StatusTypeId = (StatusTypes)reader["status_type"],
                        DisorderTypeId = (HealthDisorderTypes)reader["health_disorder_type"],
                    });
                }
                return toReturn;
            }
        }
    }
}



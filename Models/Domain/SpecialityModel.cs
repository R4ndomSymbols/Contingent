using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Npgsql;
using Utilities;

namespace StudentTracking.Models;

public class SpecialityModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("fgosCode")]
    public string FgosCode { get; set; }
    [JsonPropertyName("fgosName")]
    public string FgosName { get; set; }
    [JsonPropertyName("qualification")]
    public string Qualification { get; set; }
    [JsonPropertyName("mainNamePrefix")]
    public string MainNamePrefix { get; set; }
    [JsonPropertyName("qualificationPostfix")]
    public string QualificationPostfix { get; set; }
    [JsonPropertyName("courseCount")]
    public int CourseCount { get; set; }
    [JsonPropertyName("specialityTypeId")]
    public int SpecialityTypeId { get; set; }

    public SpecialityModel()
    {
        Id = -1;
        FgosCode = "";
        FgosName = "";
        Qualification = "";
        MainNamePrefix = "";
        QualificationPostfix = "";
        CourseCount = 0;
        SpecialityTypeId = -1;
    }

    public static async Task<SpecialityModel?> GetById(int id)
    {
        await using (var conn = await Utils.GetAndOpenConnectionFactory())
        {
            await using (var cmd = new NpgsqlCommand("SELECT * FROM specialities WHERE id = @p1", conn)
            {
                Parameters = {
                    new ("p1", id)
                }
            })
            {
                using var reader = await cmd.ExecuteReaderAsync();
                if (!reader.HasRows)
                {
                    return null;
                }
                else
                {   
                    await reader.ReadAsync();          
                    return new SpecialityModel()
                    {
                        Id = id,
                        FgosCode = (string)reader["fgos_code"],
                        FgosName = (string)reader["fgos_name"],
                        Qualification = (string)reader["qualification"],
                        MainNamePrefix = (string)reader["main_name_prefix"],
                        QualificationPostfix = (string)reader["qualification_postfix"],
                        CourseCount = (int)reader["course_count"],
                        SpecialityTypeId = (int)reader["speciality_type"],
                    };
                }
            }
        }
    }
    public static async Task<List<SpecialityModel>?> GetAllGroupView()
    {
        await using (var conn = await Utils.GetAndOpenConnectionFactory())
        {
            await using (var cmd = new NpgsqlCommand("SELECT * FROM specialities", conn))
            {
                using var reader = await cmd.ExecuteReaderAsync();
                if (!reader.HasRows)
                {
                    return null;
                }
                else
                {
                    var toReturn = new List<SpecialityModel>();
                    while (await reader.ReadAsync())
                    {
                        toReturn.Add(new SpecialityModel()
                        {
                            Id = (int)reader["id"],
                            FgosCode = (string)reader["fgos_code"],
                            FgosName = (string)reader["fgos_name"],
                            Qualification = (string)reader["qualification"],
                            MainNamePrefix = (string)reader["main_name_prefix"],
                            QualificationPostfix = (string)reader["qualification_postfix"],
                            CourseCount = (int)reader["course_count"],
                            SpecialityTypeId = (int)reader["speciality_type"],
                        });
                    }
                    return toReturn;
                }
            }
        }
    }
    public static async Task<List<SpecialityTypeModel>?> GetAllTypes()
    {
        await using (var conn = await Utils.GetAndOpenConnectionFactory())
        {
            await using (var cmd = new NpgsqlCommand("SELECT * FROM speciality_types", conn))
            {
                using var reader = await cmd.ExecuteReaderAsync();
                if (!reader.HasRows)
                {
                    return null;
                }
                else
                {
                    await reader.ReadAsync();
                    List<SpecialityTypeModel> toReturn = new List<SpecialityTypeModel>();
                    do
                    {
                        toReturn.Add(new SpecialityTypeModel()
                        {
                            Id = (int)reader["id"],
                            Name = (string)reader["name"]
                        }
                        );
                    }
                    while (await reader.ReadAsync());
                    return toReturn;
                }
            }
        }
    }
    public static async Task<List<string>?> GetAllFGOSCodes()
    {
        await using (var conn = await Utils.GetAndOpenConnectionFactory())
        {
            await using (var cmd = new NpgsqlCommand("SELECT DISTINCT fgos_code FROM specialities", conn))
            {
                using var reader = await cmd.ExecuteReaderAsync();
                if (!reader.HasRows)
                {
                    return null;
                }
                else
                {
                    List<string> toReturn = new List<string>();
                    while (await reader.ReadAsync())
                    {
                        toReturn.Add((string)reader["fgos_code"]);
                    }
                    return toReturn;
                }

            }
        }
    }
    public static async Task<List<string>?> GetAllFGOSNames()
    {
        await using (var conn = await Utils.GetAndOpenConnectionFactory())
        {
            await using (var cmd = new NpgsqlCommand("SELECT DISTINCT fgos_name FROM specialities", conn))
            {
                using var reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    return null;
                }
                else
                {
                    List<string> toReturn = new List<string>();
                    while (reader.Read())
                    {
                        toReturn.Add((string)reader["fgos_name"]);
                    }
                    return toReturn;
                }

            }
        }
    }
    public static async Task<int> CreateOrUpdateSpeciality(SpecialityModel toProcess)
    {
        await using (var conn = await Utils.GetAndOpenConnectionFactory())
        {
            if (toProcess.Id == -1)
            {
                await using (var cmd = new NpgsqlCommand("INSERT INTO specialities (fgos_code, fgos_name, qualification, main_name_prefix, qualification_postfix, " +
                " course_count, speciality_type) VALUES (@p1,@p2,@p3,@p4,@p5,@p6,@p7) RETURNING id", conn)
                {
                    Parameters = {
                        new ("p1", toProcess.FgosCode),
                        new ("p2", toProcess.FgosName),
                        new ("p3", toProcess.Qualification),
                        new ("p4", toProcess.MainNamePrefix),
                        new ("p5", toProcess.QualificationPostfix),
                        new ("p6", toProcess.CourseCount),
                        new ("p7", toProcess.SpecialityTypeId),
                    }
                }
                )
                {
                    using  var reader = cmd.ExecuteReader();
                    reader.Read();
                    if (reader.HasRows)
                    {
                        return (int)reader["id"];
                    }
                    return -1;
                }
            }
            else
            {
                using (var cmd = new NpgsqlCommand("UPDATE specialities SET fgos_code = @p1, fgos_name = @p2, qualification = @p3, main_name_prefix = @p4, qualification_postfix = @p5, " +
                " course_count = @p6, speciality_type = @p7 WHERE id = @p8", conn)
                {
                    Parameters = {
                        new ("p1", toProcess.FgosCode),
                        new ("p2", toProcess.FgosName),
                        new ("p3", toProcess.Qualification),
                        new ("p4", toProcess.MainNamePrefix),
                        new ("p5", toProcess.QualificationPostfix),
                        new ("p6", toProcess.CourseCount),
                        new ("p7", toProcess.SpecialityTypeId),
                        new ("p8", toProcess.Id),
                    }
                })
                {
                    cmd.ExecuteNonQuery();
                    return toProcess.Id;
                }
            }
        }
    }
}

public class SpecialityTypeModel
{

    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }

    public SpecialityTypeModel()
    {
        Name = "";
    }

}
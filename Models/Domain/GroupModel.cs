using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Npgsql;
using Utilities;

namespace StudentTracking.Models;

public class GroupModel
{

    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("specialityId")]
    public int SpecialityId { get; set; }
    [JsonPropertyName("courseNumber")]
    public int CourseNumber { get; set; }
    [JsonPropertyName("groupTypeId")]
    public int GroupTypeId { get; set; }
    [JsonPropertyName("educationFormId")]
    public int EducationalFormId { get; set; }
    [JsonPropertyName("groupName")]
    public string GroupName { get; set; }
    [JsonPropertyName("creationYear")]
    public int CreationYear { get; set; }

    public GroupModel()
    {
        Id = -1;
        SpecialityId = -1;
        CourseNumber = -1;
        EducationalFormId = -1;
        GroupName = "";
    }

    public static async Task<GroupModel?> GetGroupById(int id)
    {
        await using (var conn = await Utils.GetAndOpenConnectionFactory())
        {
            await using (var cmd = new NpgsqlCommand("SELECT * FROM student_groups WHERE id = @p1", conn)
            {
                Parameters = {
                    new NpgsqlParameter("p1", id)
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
                    return new GroupModel()
                    {
                        Id = id,
                        SpecialityId = (int)reader["speciality"],
                        CourseNumber = (int)reader["course_number"],
                        GroupTypeId = (int)reader["group_type"],
                        EducationalFormId = (int)reader["group_education_form"],
                        CreationYear = (int)reader["creation_year"],
                        GroupName = (string)reader["group_name"]

                    };
                }
            }
        }
    }

    public static async Task<int> CreateOrUpdateGroup(GroupModel toProcess)
    {
        await using (var conn = await Utils.GetAndOpenConnectionFactory())
        {
            await using (var cmd = new NpgsqlCommand("INSERT INTO student_groups (speciality, course_number, group_type, group_education_form, creation_year, group_name" +
            " ) VALUES (@p1,@p2,@p3,@p4,@p5,@p6) RETURNING id", conn)
            {
                Parameters = {
                        new ("p1", toProcess.SpecialityId),
                        new ("p2", toProcess.CourseNumber),
                        new ("p3", toProcess.GroupTypeId),
                        new ("p4", toProcess.EducationalFormId),
                        new ("p5", toProcess.CreationYear),
                        new ("p6", toProcess.GroupName),

                    }
            }
            )
            {
                using var reader = await cmd.ExecuteReaderAsync();
                await reader.ReadAsync();
                return (int)reader["id"];
            }
        }

        /*
        else
        {
            using (var cmd = new NpgsqlCommand("UPDATE student_groups SET speciality = @p1, course_number = @p2, group_type = @p3, group_education_form = @p4, creation_year = @p5, " +
            " group_name = @p6 WHERE id = @p7", conn)
            {
                Parameters = {
                    new ("p1", toProcess.SpecialityId),
                    new ("p2", toProcess.CourseNumber),
                    new ("p3", toProcess.GroupTypeId),
                    new ("p4", toProcess.EducationalFormId),
                    new ("p5", toProcess.CreationYear),
                    new ("p6", toProcess.GroupName),
                    new ("p7", toProcess.Id),
                }
            })
            {
                cmd.ExecuteNonQuery();
                return toProcess.Id;
            }
        }*/
    }

    public static async Task<List<GroupEssential>?> FindGroup(string? searchText)
    {
        if (searchText == null || searchText.Length < 2)
        {
            return null;
        }

        await using (var conn = await Utils.GetAndOpenConnectionFactory())
        {
            string query = "SELECT id, group_name FROM student_groups WHERE group_name LIKE @p1";
            await using (var cmd = new NpgsqlCommand(query, conn)
            {
                Parameters = {
                    new NpgsqlParameter("p1", "%"+searchText+"%")
                }
            })
            {
                using var reader = await cmd.ExecuteReaderAsync();
                if (!reader.HasRows)
                {
                    return null;
                }
                var found = new List<GroupEssential>();
                while (await reader.ReadAsync())
                {
                    found.Add(new GroupEssential()
                    {
                        Name = (string)reader["group_name"],
                        Id = (int)reader["id"]
                    });
                }
                return found;
            }
        }
    }
}

public class GroupEducationForm
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("postfix")]
    public string Postfix { get; set; }

    public GroupEducationForm()
    {
        Id = -1;
        Name = "";
        Postfix = "";
    }
}

public class GroupEducationType
{

    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("postfix")]
    public string Postfix { get; set; }
    public GroupEducationType()
    {
        Id = -1;
        Name = "";
        Postfix = "";
    }
}

public class GroupEssential
{

    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }

    public GroupEssential()
    {
        Id = -1;
        Name = "";
    }

}

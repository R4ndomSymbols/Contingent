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

    public static GroupModel? GetGroupById(int id)
    {
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand("SELECT * FROM student_groups WHERE id = @p1", conn)
            {
                Parameters = {
                    new NpgsqlParameter("p1", id)
                }
            })
            {
                var reader = cmd.ExecuteReader();
                reader.Read();
                if (!reader.HasRows)
                {
                    return null;
                }
                else
                {
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
    public static List<GroupEducationForm>? GetGroupEduForms()
    {
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand("SELECT * FROM group_education_forms", conn))
            {
                var reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    return null;
                }
                else
                {
                    var toReturn = new List<GroupEducationForm>();
                    while (reader.Read())
                    {
                        toReturn.Add(new GroupEducationForm
                        {
                            Id = (int)reader["id"],
                            Name = (string)reader["form_name"],
                            Postfix = (string)reader["group_name_postfix"],
                        });
                    }
                    return toReturn;
                }
            }
        }
    }
    public static List<GroupEducationType>? GetGroupEduTypes()
    {
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand("SELECT * FROM group_types", conn))
            {
                var reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    return null;
                }
                else
                {
                    var toReturn = new List<GroupEducationType>();
                    while (reader.Read())
                    {
                        toReturn.Add(new GroupEducationType
                        {
                            Id = (int)reader["id"],
                            Name = (string)reader["type_name"],
                            Postfix = (string)reader["group_name_postfix"],
                        });
                    }
                    return toReturn;
                }
            }
        }
    }
    public static int CreateOrUpdateGroup(GroupModel toProcess)
    {
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            if (toProcess.Id == -1)
            {
                using (var cmd = new NpgsqlCommand("INSERT INTO student_groups (speciality, course_number, group_type, group_education_form, creation_year, group_name" +
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
                    var reader = cmd.ExecuteReader();
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
            }
        }
    }

    public static List<GroupEssential>? FindGroup(string? searchText)
    {
        if (searchText == null || searchText.Length < 2){
            return null;
        }

        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            string query = "SELECT id, group_name FROM student_groups WHERE group_name LIKE @p1";
            using (var cmd = new NpgsqlCommand(query, conn)
            {
                Parameters = {
                    new NpgsqlParameter("p1", "%"+searchText+"%")
                }
            })
            {
                var reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    return null;
                }
                var found = new List<GroupEssential>();
                while (reader.Read())
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

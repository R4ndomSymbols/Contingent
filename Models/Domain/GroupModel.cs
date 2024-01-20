using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using StudentTracking.Models.Domain.Misc;
using Npgsql;
using Utilities;
using Utilities.Validation;
using StudentTracking.Models.JSON;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using StudentTracking.Models.SQL;
using StudentTracking.Controllers.DTO.Out;

namespace StudentTracking.Models;

public class GroupModel : DbValidatedObject
{
    private int _id;
    private int _eduProgramId;
    private int _courseOn;
    private string _groupName;
    private GroupEducationFormat.Formats _eduFormatType;
    private GroupSponsorshipType.Types _sponsorshipType;
    private int _creationYear;
    private string? _sequenceLetter;
    private bool _nameGenerated;
    private int _historySequenceId;

    public int Id
    {
        get => _id;
    }
    public int EducationalProgramId
    {
        get => _eduProgramId;
    }

    public async Task SetEducationalProgramId(int id, ObservableTransaction? scope)
    {
        bool exists = await SpecialityModel.IsIdExists(id, scope);
        if (PerformValidation(
            () => exists,
            new ValidationError(nameof(EducationalProgramId), "Специальность указана неверно")
        ))
        {
            _eduProgramId = id;
            _courseOn = SpecialityModel.MINIMAL_COURSE_COUNT;
        }
    }
    public int CourseOn
    {
        get => _courseOn;
    }
    public int EducationalForm
    {
        get => (int)_eduFormatType;
        set
        {
            if (PerformValidation(
                () =>
                {
                    try
                    {
                        var tmp = (GroupEducationFormat.Formats)value;
                        return true;
                    }
                    catch (InvalidCastException)
                    {
                        return false;
                    }
                }, new ValidationError(nameof(EducationalForm), "Форма обучения указана неверно")
            ))
            {
                _eduFormatType = (GroupEducationFormat.Formats)value;
            }
        }
    }
    public int SponsorshipType
    {
        get => (int)_sponsorshipType;
        set
        {
            if (PerformValidation(
                () =>
                {
                    try
                    {
                        var tmp = (GroupSponsorshipType.Types)value;
                        return true;
                    }
                    catch (InvalidCastException)
                    {
                        return false;
                    }
                }, new ValidationError(nameof(SponsorshipType), "Форма финансирования указана неверно")
            ))
            {
                _sponsorshipType = (GroupSponsorshipType.Types)value;
            }
        }
    }
    public string GroupName
    {
        get => _groupName;
        set
        {
            if (PerformValidation(
                () => !(string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value)),
            new ValidationError(nameof(GroupName), "Название группы не может быть пустым")))
            {
                _groupName = value;
            }
        }
    }
    public string CreationYear
    {
        get => _creationYear.ToString();
        set
        {
            if (PerformValidation(
                () => int.TryParse(value, out int p),
                new ValidationError(nameof(CreationYear), "Год создания не является числом")
            ))
            {

                var year = int.Parse(value);
                if (PerformValidation(
                    () =>
                    {
                        return ValidatorCollection.CheckRange(year, Utils.ORG_CREATION_YEAR, DateTime.Now.Year);

                    }, new ValidationError(nameof(CreationYear), "Год создания указан неверно")
                ))
                {
                    _creationYear = year;
                }
            }
        }
    }

    public GroupModel()
    {
        RegisterProperty(nameof(GroupName));
        RegisterProperty(nameof(SponsorshipType));
        RegisterProperty(nameof(EducationalForm));
        RegisterProperty(nameof(EducationalProgramId));
        RegisterProperty(nameof(CreationYear));
        _groupName = "";
    }

    protected GroupModel(int id) : base(RelationTypes.Bound)
    {
        _groupName = "";
    }

    public static async Task<GroupModel?> GetGroupById(int id, ObservableTransaction? scope)
    {
        NpgsqlConnection? conn = scope == null ? await Utils.GetAndOpenConnectionFactory() : null;
        string cmdText = "SELECT * FROM educational_group WHERE id = @p1";
        NpgsqlCommand cmd;
        if (scope != null)
        {
            cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }
        else
        {
            cmd = new NpgsqlCommand(cmdText, conn);
        }
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", id));
        GroupModel? result = null;
        await using (cmd)
        {
            await using var reader = cmd.ExecuteReader();
            if (!reader.HasRows)
            {
                return null;
            }
            await reader.ReadAsync();
            result = new GroupModel(id)
            {
                _courseOn = (int)reader["course_on"],
                _creationYear = (int)reader["creation_year"],
                _eduFormatType = (GroupEducationFormat.Formats)(int)reader["form_of_education"],
                _eduProgramId = (int)reader["program_id"],
                _groupName = (string)reader["group_name"],
                _sponsorshipType = (GroupSponsorshipType.Types)(int)reader["type_of_financing"],
                _sequenceLetter = reader["letter"].GetType() == typeof(DBNull) ? null : (string)reader["letter"]
            };
        }
        if (conn != null)
        {
            await conn.DisposeAsync();
        }
        return result;
    }

    public async Task SaveAsync(ObservableTransaction? scope)
    {

        if (await GetCurrentState(scope) != RelationTypes.Pending)
        {
            return;
        }
        NpgsqlConnection? conn = scope == null ? await Utils.GetAndOpenConnectionFactory() : null;
        string cmdText = "INSERT INTO public.educational_group( " +
        " program_id, course_on, group_name, type_of_financing, " +
        " form_of_education, education_program_type, creation_year, letter, name_generated, group_sequence_id) " +
        " VALUES (@p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10) RETURNING id";
        NpgsqlCommand cmd;
        if (scope != null)
        {
            cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }
        else
        {
            cmd = new NpgsqlCommand(cmdText, conn);
        }
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", _eduProgramId));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p2", _courseOn));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p3", _groupName));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p4", (int)_sponsorshipType));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p5", (int)_eduFormatType));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p6", -1));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p7", _creationYear));
        cmd.Parameters.Add(new NpgsqlParameter<string?>("p8", _sequenceLetter));
        cmd.Parameters.Add(new NpgsqlParameter<bool>("p9", _nameGenerated));
        int seqId;
        if (_courseOn == SpecialityModel.MINIMAL_COURSE_COUNT)
        {
            seqId = await GetNextSequenceId(scope);
        }
        else
        {
            seqId = _historySequenceId;
        }

        cmd.Parameters.Add(new NpgsqlParameter<int>("p10", seqId));
        await using (cmd)
        {
            await using var reader = cmd.ExecuteReader();
            await reader.ReadAsync();
            _id = (int)reader["id"];
            NotifyStateChanged();
        }
        if (conn != null)
        {
            await conn.DisposeAsync();
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

    public static async Task<bool> IsIdExists(int id, ObservableTransaction? scope){
        await using var connection = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "SELECT EXISTS(SELECT id FROM eductional_group WHERE id = @p1)";
        NpgsqlCommand cmd;
        if (scope != null){
            cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }
        else{
            cmd = new NpgsqlCommand(cmdText, connection); 
        }
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", id));
        await using (connection)
        await using (cmd){
            using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            return (bool)reader["exists"];
        }
    }


    public override async Task<IDbObjectValidated?> GetDbRepresentation(ObservableTransaction? scope)
    {
        return await GetGroupById(_id, scope);
    }
    public override bool Equals(IDbObjectValidated? other)
    {
        if (other == null)
        {
            return false;
        }
        if (other.GetType() != typeof(GroupModel))
        {
            return false;
        }
        GroupModel unboxed = (GroupModel)other;
        return _id == unboxed._id &&
               _courseOn == unboxed._courseOn &&
               _creationYear == unboxed._creationYear &&
               _eduFormatType == unboxed._eduFormatType &&
               _eduProgramId == unboxed._eduProgramId &&
               _groupName == unboxed._groupName &&
               _sponsorshipType == unboxed._sponsorshipType &&
               (_sequenceLetter == unboxed._sequenceLetter || (_sequenceLetter==null &&unboxed._sequenceLetter==null));
    }


    public static async Task<GroupModel> FromJSON(GroupModelJSON json, ObservableTransaction? scope)
    {

        var built = new GroupModel();
        built._id = json.Id;
        await built.SetEducationalProgramId(json.EduProgramId, scope);
        built.EducationalForm = json.EduFormatCode;
        built.SponsorshipType = json.SponsorshipTypeCode;
        built.CreationYear = json.CreationYear;
        if (json.AutogenerateName)
        {
            built.GroupName = "make_valid_marker";
            if (await built.TryGenerateNameAsync(scope))
            {
                built._nameGenerated = true;
            }
            else
            {
                built.GroupName = "";
                built._groupName = "";
            }

        }
        else
        {
            built.GroupName = json.GroupName;
            built._nameGenerated = false;
        }
        return built;
    }
    // доделать, заглушка
    // обновляет курс при изменении года регистрации или текущего года.
    private void UpdateCourse()
    {

    }

    public async Task<bool> TryGenerateNameAsync(ObservableTransaction? scope)
    {
        var state = await GetCurrentState(scope);
        Console.WriteLine(state);
        if (state != RelationTypes.Pending && state != RelationTypes.Bound)
        {
            return false;
        }
        var speciality = await SpecialityModel.GetById(_eduProgramId, scope);
        if (speciality == null)
        {
            return false;
        }
        char? nextSequenceLetter = await GetNextSequenceLetter(scope);
        _sequenceLetter = nextSequenceLetter == null ? null : ((char)nextSequenceLetter).ToString();
        _groupName = speciality.FgosPrefix +
                    _courseOn.ToString() +
                    (_creationYear % 100).ToString() +
                    speciality.QualificationPostfix +
                    GroupSponsorshipType.Names[_sponsorshipType].postfix +
                    GroupEducationFormat.Names[_eduFormatType].postfix +
                    (speciality.EducationalLevelIn == (int)StudentEducationalLevelRecord.EducationalLevels.SecondaryGeneralEducation
                        ? "/11"
                        : "") +
                    (nextSequenceLetter == null ? "" : ("(" + ((char)nextSequenceLetter).ToString() + ")"));
        return true;

    }
    // хранить букву в базе
    // если не получается получить последнюю, то буква не указывается
    private async Task<char?> GetNextSequenceLetter(ObservableTransaction? scope)
    {
        NpgsqlConnection? conn = scope == null ? await Utils.GetAndOpenConnectionFactory() : null;
        string cmdText = "SELECT MAX(letter) AS letter FROM educational_group WHERE letter IS NOT NULL AND " +
        " program_id = @p1 AND type_of_financing = @p2 AND form_of_education = @p3 " +
        " AND creation_year = @p4 AND name_generated";
        NpgsqlCommand cmd;
        if (scope != null)
        {
            cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }
        else
        {
            cmd = new NpgsqlCommand(cmdText, conn);
        }
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", _eduProgramId));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p2", (int)_sponsorshipType));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p3", (int)_eduFormatType));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p4", _creationYear));
        char? result = null;
        await using (cmd)
        {
            await using var reader = cmd.ExecuteReader();
            if (!reader.HasRows)
            {
                return null;
            }
            await reader.ReadAsync();
            if (reader["letter"].GetType() == typeof(DBNull)){
                return null;
            }
            result = ((string)reader["letter"])[0];
            result = (char)((int)result + 1);
        }
        if (conn != null)
        {
            await conn.DisposeAsync();
        }
        return result;
    }

    private static async Task<int> GetNextSequenceId(ObservableTransaction? scope)
    {
        NpgsqlConnection? conn = scope == null ? await Utils.GetAndOpenConnectionFactory() : null;
        string cmdText = "SELECT MAX(group_sequence_id) AS seq_id FROM educational_group";
        NpgsqlCommand cmd;
        if (scope != null)
        {
            cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }
        else
        {
            cmd = new NpgsqlCommand(cmdText, conn);
        }
        int result = 1;
        await using (cmd)
        {
            await using var reader = cmd.ExecuteReader();
            if (!reader.HasRows)
            {
                return result;
            }
            await reader.ReadAsync();
            if (reader["seq_id"].GetType() == typeof(DBNull)){
                return result;
            }
            result = (int)reader["seq_id"] + 1;
        }
        if (conn != null)
        {
            await conn.DisposeAsync();
        }
        return result;
    }

    public static async Task<bool> IsAllExists(IEnumerable<int> ids){

        var conn = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "SELECT COUNT(id) AS c FROM educational_group WHERE " + 
        "id = ANY(@p1)";
        NpgsqlCommand cmd = new NpgsqlCommand(cmdText, conn);
        var p = new NpgsqlParameter();
        p.ParameterName = "p1";
        p.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Integer;
        p.Value = ids.ToArray();
        using (conn)
        using (cmd){
            using var reader = cmd.ExecuteReader();
            return (int)reader["c"] == ids.Count();
        }
    }  

    
    public static async Task<List<GroupResponseDTO>?> FindGroups(SelectQuery<GroupResponseDTO> select){
        NpgsqlConnection conn = await Utils.GetAndOpenConnectionFactory();
        using (conn){

        }
        return null;
           
    }
}

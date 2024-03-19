using StudentTracking.Models.Domain.Misc;
using Npgsql;
using Utilities;
using Utilities.Validation;
using StudentTracking.SQL;
using StudentTracking.Controllers.DTO.In;

namespace StudentTracking.Models;

// при записи в базу создается не группа, а целый набор групп до выпускного курса
// пользователь может создавать только группы начального курса
// группы после 11 класса 

// Агрегирует специальность, подружает ее самостоятельно

public class GroupModel
{
    public static string InvalidNamePlaceholder => "Нет";
    private int _id;
    private SpecialityModel _educationProgram;
    private int _courseOn;
    private string _groupName;
    private GroupEducationFormat _formatOfEducation;
    private GroupSponsorship _groupSponsorsip;
    private int _creationYear;
    private string? _sequenceLetter;
    private bool _nameGenerated;
    private int _historySequenceId;
    // адаптированная образовательная программа
    private int _groupSpecialTeachingCondition;

    public int Id
    {
        get => _id;
    }
    public SpecialityModel EducationProgram
    {
        get => _educationProgram;
    }
    public int CourseOn
    {
        get => _courseOn;
    }
    public GroupEducationFormat FormatOfEducation
    {
        get => _formatOfEducation;
    }
    public GroupSponsorship SponsorshipType
    {
        get => _groupSponsorsip;
    }

    public int HistoricalSequenceId {
        get => _historySequenceId;
    }
    public string GroupName
    {
        get
        {
            if (_nameGenerated)
            {
                string name = _educationProgram.FgosPrefix +
                _courseOn.ToString() +
                (_creationYear % 100).ToString() +
                _educationProgram.QualificationPostfix +
                (_sequenceLetter ?? "") +
                _formatOfEducation.GroupNamePostfix +
                _groupSponsorsip.GroupNamePostfix;
                return name;
            }
            else
            {
                return _groupName;
            }
        }

    }
    public int CreationYear
    {
        get => _creationYear;
    }
    public bool IsNameGenerated {
        get => _nameGenerated;
    }

    private GroupModel()
    {

    }

    public static Mapper<GroupModel> GetMapper(Column? source, JoinSection.JoinType joinType = JoinSection.JoinType.InnerJoin){
        var specialityMapper = SpecialityModel.GetMapper(new Column("program_id","educational_group"), JoinSection.JoinType.LeftJoin);
        var usedCols =  new List<Column>(){
                new Column("group_id", "educational_group"),
                new Column("program_id", "educational_group"),
                new Column("course_on", "educational_group"),
                new Column("group_name", "educational_group"),
                new Column("type_of_financing", "educational_group"),
                new Column("form_of_education", "educational_group"),
                new Column("education_program_type", "educational_group"),
                new Column("creation_year", "educational_group"),
                new Column("letter", "educational_group"),
                new Column("name_generated", "educational_group"),
                new Column("group_sequence_id", "educational_group")
            };
        var groupMapper = new Mapper<GroupModel>(
            (reader) =>
            {   
                var id = reader["group_id"];
                if (id.GetType() == typeof(DBNull)){
                    return QueryResult<GroupModel>.NotFound();
                }
                var group = new GroupModel();
                group._id = (int)id;
                group._courseOn = (int)reader["course_on"];
                group._creationYear = (int)reader["creation_year"];
                group._formatOfEducation = GroupEducationFormat.GetByTypeCode((int)reader["form_of_education"]);
                group._groupSponsorsip = GroupSponsorship.GetByTypeCode((int)reader["type_of_financing"]);
                group._sequenceLetter = reader["letter"].GetType() == typeof(DBNull) ? null : (string)reader["letter"];
                group._nameGenerated = (bool)reader["name_generated"];
                if (!group._nameGenerated)
                {
                    group._groupName = (string)reader["group_name"];
                }
                var speciality = specialityMapper.Map(reader);
                group._educationProgram = speciality.ResultObject;
                group._historySequenceId = (int)reader["group_sequence_id"];
                group._groupSpecialTeachingCondition = (int)reader["education_program_type"];
                return QueryResult<GroupModel>.Found(group);
            },
            usedCols
           
        );
        if (source is not null){
            groupMapper.PathTo.AddHead(joinType, source, new Column("group_id", "educational_group"));
        }
        groupMapper.AssumeChild(specialityMapper);
        return groupMapper;
    } 
    

    // только первый курс
    public static async Task<Result<GroupModel?>> Build(GroupInDTO? dto)
    {
        if (dto is null)
        {
            return Result<GroupModel>.Failure(new ValidationError("dto не может быть null"));
        }
        IList<ValidationError> errors = new List<ValidationError>();
        GroupModel model = new();
        if (errors.IsValidRule(
            int.TryParse(dto.CreationYear, out int parsed) && parsed >= Utils.ORG_CREATION_YEAR && parsed <= DateTime.Now.Year + 1,
            message: "Дата создания указана неверно",
            propName: nameof(CreationYear)
        ))
        {
            model._creationYear = int.Parse(dto.CreationYear);
        }
        if (errors.IsValidRule(
            GroupEducationFormat.TryGetByTypeCode(dto.EduFormatCode),
            message: "Тип обучения указан неверно",
            propName: nameof(FormatOfEducation)
        ))
        {
            model._formatOfEducation = GroupEducationFormat.GetByTypeCode(dto.EduFormatCode);
        }
        if (errors.IsValidRule(
            GroupSponsorship.TryGetByTypeCode(dto.SponsorshipTypeCode),
            message: "Тип финансирования указан неверно",
            propName: nameof(CreationYear)
        ))
        {
            model._groupSponsorsip = GroupSponsorship.GetByTypeCode(dto.SponsorshipTypeCode);
        }

        var found = await SpecialityModel.GetById(dto.EduProgramId);

        if (errors.IsValidRule(
            found is not null,
            message: "Специальность не может быть не указана",
            propName: nameof(EducationProgram)
        ))
        {
            model._educationProgram = found;
        }

        if (errors.Any())
        {
            return Result<GroupModel>.Failure(errors);
        }

        model._courseOn = 1;
        model._historySequenceId = await GetNextSequenceId();
        model._nameGenerated = dto.AutogenerateName;

        if (model._nameGenerated)
        {
            // для генерации имени нужно знать:
            // курс группы, специальность группы, тип обучения и тип посещения,
            // так же группа может быть с 11 класса (не учитывается)
            // порядковый номер группы среди других таких же групп (совпадает специальность и курс)

            model._sequenceLetter = (await GetNextSequenceLetter(model._educationProgram, model._groupSponsorsip, model._formatOfEducation, model._creationYear))?.ToString();
        }
        else
        {
            model._sequenceLetter = null;
            if (errors.IsValidRule(
                ValidatorCollection.CheckStringPattern(dto.GroupName, ValidatorCollection.OnlyText),
                message: "Неверно указано имя группы",
                propName: nameof(GroupName)
            ))
            {
                model._groupName = dto.GroupName;
            }
        }
        if (errors.Any())
        {
            return Result<GroupModel>.Failure(errors);
        }
        else
        {
            return Result<GroupModel>.Success(model);
        }


    }

    public static async Task<GroupModel?> GetGroupById(int id, ObservableTransaction? scope = null)
    {
        var parameters = new SQLParameterCollection();
        var p1 = parameters.Add(id);
        var where = new ComplexWhereCondition(
            new WhereCondition(
                new Column("group_id", "educational_group"),
                p1,
                WhereCondition.Relations.Equal
            )
        );
        var found = await FindGroups(new QueryLimits(0, 1), additionalConditions: where, addtitionalParameters: parameters);
        if (found.Any())
        {
            return found.First();
        }
        else
        {
            return null;
        }
    }
    // метод сохраняет не только созданную группу, но и создает набор групп до выпускного курса
    // вызывается только в случае, если группа имеет первый курс, и ее имя генерируемое
    public static async Task<IReadOnlyCollection<GroupModel>> SaveAllNextGroups(GroupModel? initialGroup, ObservableTransaction? scope = null)
    {   
        if (initialGroup is null || initialGroup._courseOn != 1 || !initialGroup._nameGenerated)
        {
            throw new Exception("Такую группу невозможно сохранить данным образом");
        }
        NpgsqlConnection? conn = scope == null ? await Utils.GetAndOpenConnectionFactory() : null;
        var saved = new List<GroupModel>();
        var currentGroup = initialGroup;
        for (int i = 1; i <= currentGroup._educationProgram.CourseCount; i++)
        {
            string cmdText = "INSERT INTO public.educational_group( " +
            " program_id, course_on, group_name, type_of_financing, " +
            " form_of_education, education_program_type, creation_year, letter, name_generated, group_sequence_id) " +
            " VALUES (@p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10) RETURNING group_id";
            NpgsqlCommand cmd;
            if (scope != null)
            {
                cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
            }
            else
            {
                cmd = new NpgsqlCommand(cmdText, conn);
            }
            currentGroup._courseOn = i;
            cmd.Parameters.Add(new NpgsqlParameter<int>("p1", currentGroup._educationProgram.Id));
            cmd.Parameters.Add(new NpgsqlParameter<int>("p2", currentGroup._courseOn));
            cmd.Parameters.Add(new NpgsqlParameter<string>("p3", currentGroup.GroupName));
            cmd.Parameters.Add(new NpgsqlParameter<int>("p4", (int)currentGroup._groupSponsorsip.TypeOfSponsorship));
            cmd.Parameters.Add(new NpgsqlParameter<int>("p5", (int)currentGroup._formatOfEducation.FormatType));
            cmd.Parameters.Add(new NpgsqlParameter<int>("p6", -1));
            cmd.Parameters.Add(new NpgsqlParameter<int>("p7", currentGroup._creationYear));
            cmd.Parameters.Add(new NpgsqlParameter<string?>("p8", currentGroup._sequenceLetter));
            cmd.Parameters.Add(new NpgsqlParameter<bool>("p9", currentGroup._nameGenerated));
            cmd.Parameters.Add(new NpgsqlParameter<int>("p10", currentGroup._historySequenceId));
            await using (cmd)
            {
                await using var reader = cmd.ExecuteReader();
                await reader.ReadAsync();
                currentGroup._id = (int)reader["group_id"];
                saved.Add(currentGroup);
                currentGroup = currentGroup.Copy();
            }
        }

        if (conn != null)
        {
            await conn.DisposeAsync();
        }
        return saved;
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

    public static async Task<bool> IsIdExists(int id, ObservableTransaction? scope)
    {
        await using var connection = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "SELECT EXISTS(SELECT id FROM educational_group WHERE id = @p1)";
        NpgsqlCommand cmd;
        if (scope != null)
        {
            cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }
        else
        {
            cmd = new NpgsqlCommand(cmdText, connection);
        }
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", id));
        await using (connection)
        await using (cmd)
        {
            using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            return (bool)reader["exists"];
        }
    }

    
    // хранить букву в базе
    // если не получается получить последнюю, то буква не указывается
    private static async Task<char?> GetNextSequenceLetter(SpecialityModel spec, GroupSponsorship sp, GroupEducationFormat gef, int creationYear, ObservableTransaction? scope = null)
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
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", spec.Id));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p2", (int)sp.TypeOfSponsorship));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p3", (int)gef.FormatType));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p4", creationYear));
        char? result = null;
        await using (cmd)
        {
            await using var reader = cmd.ExecuteReader();
            if (!reader.HasRows)
            {
                return null;
            }
            await reader.ReadAsync();
            if (reader["letter"].GetType() == typeof(DBNull))
            {
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

    private static async Task<int> GetNextSequenceId(ObservableTransaction? scope = null)
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
            if (reader["seq_id"].GetType() == typeof(DBNull))
            {
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

    public static async Task<IReadOnlyCollection<GroupModel>> FindGroups(QueryLimits limits, JoinSection? additionalJoins = null, ComplexWhereCondition? additionalConditions = null, SQLParameterCollection? addtitionalParameters = null, OrderByCondition? additionalOrderBy = null)
    {
        using var conn = await Utils.GetAndOpenConnectionFactory();
        var mapper = GetMapper(null);
        var buildResult = SelectQuery<GroupModel>.Init("educational_group")
        .AddMapper(mapper)
        .AddJoins(mapper.PathTo.AppendJoin(additionalJoins))
        .AddWhereStatement(additionalConditions)
        .AddParameters(addtitionalParameters)
        .AddOrderByStatement(additionalOrderBy)
        .Finish();
        if (buildResult.IsFailure)
        {
            throw new Exception("Запрос не может быть несконструирован");
        }
        return await buildResult.ResultObject.Execute(conn, limits);
    }
    
    public GroupModel Copy(){
        var newGroup = new GroupModel
        {
            _courseOn = _courseOn,
            _creationYear = _creationYear,
            _educationProgram = _educationProgram,
            _formatOfEducation = _formatOfEducation,
            _groupName = _groupName,
            _groupSpecialTeachingCondition = _groupSpecialTeachingCondition,
            _groupSponsorsip = _groupSponsorsip,
            _historySequenceId = _historySequenceId,
            _id = _id,
            _nameGenerated = _nameGenerated,
            _sequenceLetter = _sequenceLetter
        };
        return newGroup;
    }

}

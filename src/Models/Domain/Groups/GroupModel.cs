using StudentTracking.Models.Domain.Misc;
using Npgsql;
using Utilities;
using Utilities.Validation;
using StudentTracking.SQL;
using StudentTracking.Controllers.DTO.In;
using System.Data.SqlTypes;
using Microsoft.AspNetCore.Http.Connections;
using StudentTracking.Models.Domain.Flow.History;

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
    private GroupSponsorship _groupSponsorship;
    private int _creationYear;
    private string? _sequenceLetter;
    private bool _nameGenerated;
    private int _historySequenceId;
    // поле отвечает за упрощение поисковой выдачи
    // группа активна тогда и только тогда, когда:
    // это группа первого курса
    // или это группа где предыдущая группа с таким же sequenceId имела/имеет в своем составе студентов
    // группа становится неактивной, как только из нее отчисляются все студенты
    private bool _isActive;
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
        get => _groupSponsorship;
    }

    public int HistoricalSequenceId
    {
        get => _historySequenceId;
    }
    public string GroupName
    {
        get => _groupName;
    }
    public int CreationYear
    {
        get => _creationYear;
    }
    public bool IsNameGenerated
    {
        get => _nameGenerated;
    }

    private GroupModel()
    {

    }

    public static Mapper<GroupModel> GetMapper(Column? source, JoinSection.JoinType joinType = JoinSection.JoinType.InnerJoin)
    {
        var specialityMapper = SpecialityModel.GetMapper(new Column("program_id", "educational_group"), JoinSection.JoinType.LeftJoin);
        var usedCols = new List<Column>(){
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
                new Column("group_sequence_id", "educational_group"),
                new Column("is_active", "educational_group")
            };
        var groupMapper = new Mapper<GroupModel>(
            (reader) =>
            {
                var id = reader["group_id"];
                if (id.GetType() == typeof(DBNull))
                {
                    return QueryResult<GroupModel>.NotFound();
                }
                var group = new GroupModel();
                group._id = (int)id;
                group._courseOn = (int)reader["course_on"];
                group._creationYear = (int)reader["creation_year"];
                group._formatOfEducation = GroupEducationFormat.GetByTypeCode((int)reader["form_of_education"]);
                group._groupSponsorship = GroupSponsorship.GetByTypeCode((int)reader["type_of_financing"]);
                group._sequenceLetter = reader["letter"].GetType() == typeof(DBNull) ? null : (string)reader["letter"];
                group._nameGenerated = (bool)reader["name_generated"];
                group._groupName = (string)reader["group_name"];
                var speciality = specialityMapper.Map(reader);
                group._educationProgram = speciality.ResultObject;
                group._historySequenceId = (int)reader["group_sequence_id"];
                group._groupSpecialTeachingCondition = (int)reader["education_program_type"];
                group._isActive = (bool)reader["is_active"];
                return QueryResult<GroupModel>.Found(group);
            },
            usedCols

        );
        if (source is not null)
        {
            groupMapper.PathTo.AddHead(joinType, source, new Column("group_id", "educational_group"));
        }
        groupMapper.AssumeChild(specialityMapper);
        return groupMapper;
    }


    // только первый курс
    public static Result<GroupModel> Build(GroupInDTO? dto)
    {
        if (dto is null)
        {
            return Result<GroupModel>.Failure(new ValidationError("dto не может быть null"));
        }
        IList<ValidationError?> errors = new List<ValidationError?>();
        GroupModel model = new();
        if (errors.IsValidRule(
            dto.CreationYear >= Utils.ORG_CREATION_YEAR && dto.CreationYear <= DateTime.Now.Year + 1,
            message: "Дата создания указана неверно",
            propName: nameof(CreationYear)
        ))
        {
            model._creationYear = dto.CreationYear;
        }
        if (errors.IsValidRule(
            GroupEducationFormat.TryGetByTypeCode(dto.EduFormatCode, out GroupEducationFormat? type),
            message: "Тип обучения указан неверно",
            propName: nameof(FormatOfEducation)
        ))
        {
            model._formatOfEducation = type!;
        }
        if (errors.IsValidRule(
            GroupSponsorship.TryGetByTypeCode(dto.SponsorshipTypeCode, out GroupSponsorship? sponsorship),
            message: "Тип финансирования указан неверно",
            propName: nameof(CreationYear)
        ))
        {
            model._groupSponsorship = sponsorship!;
        }

        var found = SpecialityModel.GetById(dto.EduProgramId).Result;

        if (errors.IsValidRule(
            found is not null,
            message: "Специальность не может быть не указана",
            propName: nameof(EducationProgram)
        ))
        {
            model._educationProgram = found!;
        }

        if (errors.Any())
        {
            return Result<GroupModel>.Failure(errors);
        }
        model._nameGenerated = dto.AutogenerateName;

        if (model._nameGenerated)
        {
            // для генерации имени нужно знать:
            // курс группы, специальность группы, тип обучения и тип посещения,
            // так же группа может быть с 11 класса (не учитывается)
            // порядковый номер группы среди других таких же групп (совпадает специальность и курс)
            model._courseOn = 1;
            model._isActive = true;
            model._historySequenceId = GetNextSequenceId();
            model._nameGenerated = dto.AutogenerateName;
            model._sequenceLetter = model.GetSequenceLetter();
            model._groupName = model.GenerateGroupName();

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
            if (errors.IsValidRule(
                model._educationProgram.CourseCount >= dto.CourseOn && dto.CourseOn > 0,
                message: "Курс указан неверно",
                propName: nameof(CourseOn))
            )
            {
                model._courseOn = dto.CourseOn;
                var ancestor = GetGroupById(dto.PreviousGroupId);
                if (ancestor is null)
                {
                    if (errors.IsValidRule(
                    model._courseOn == 1,
                    message: "Группа-предшественник указана неверно",
                    propName: "AncestorGroup"
                    ))
                    {
                        model._historySequenceId = GetNextSequenceId();
                        model._isActive = true;
                    }
                }
                else
                {
                    if (errors.IsValidRule(
                    model._courseOn - ancestor._courseOn == 1,
                    message: "Группа-предшественник указана неверно",
                    propName: "AncestorGroup"
                    ))
                    {
                        model._historySequenceId = ancestor._historySequenceId;
                        model._isActive = new GroupHistory(ancestor).GetStateOnDate(DateTime.Now).Any();
                    }
                }
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

    public static GroupModel? GetGroupById(int id, ObservableTransaction? scope = null)
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
        var found = FindGroups(new QueryLimits(0, 1), additionalConditions: where, addtitionalParameters: parameters).Result;
        return found.FirstOrDefault(x => true, null);
    }
    // метод сохраняет не только созданную группу, но и создает набор групп до выпускного курса
    // вызывается только в случае, если группа имеет первый курс, и ее имя генерируемое
    public Result<IReadOnlyCollection<GroupModel>> SaveGroupSequence(ObservableTransaction? scope = null)
    {
        if (_courseOn != 1 || !_nameGenerated)
        {
            return Result<IReadOnlyCollection<GroupModel>>.Failure(new ValidationError("Такую группу невозможно сохранить данным образом"));
        }
        var saved = new List<GroupModel>();
        var currentGroup = this;
        for (int i = 1; i <= currentGroup._educationProgram.CourseCount; i++)
        {
            if (currentGroup.Save(scope).IsSuccess)
            {
                saved.Add(currentGroup);
            }
            currentGroup = currentGroup.Copy();
            currentGroup._courseOn++;
        }
        return Result<IReadOnlyCollection<GroupModel>>.Success(saved);
    }

    public ResultWithoutValue Save(ObservableTransaction? scope)
    {
        if (GetGroupById(_id) is not null || _educationProgram.Id is null)
        {
            return ResultWithoutValue.Failure(new ValidationError("Невозможно сохранить данную группу"));
        }
        NpgsqlConnection? conn = scope == null ? Utils.GetAndOpenConnectionFactory().Result : null;
        string cmdText = "INSERT INTO public.educational_group( " +
            " program_id, course_on, group_name, type_of_financing, " +
            " form_of_education, education_program_type, creation_year, letter, name_generated, group_sequence_id, is_active) " +
            " VALUES (@p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11) RETURNING group_id";
        NpgsqlCommand cmd;
        if (scope != null)
        {
            cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }
        else
        {
            cmd = new NpgsqlCommand(cmdText, conn);
        }
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", (int)_educationProgram.Id!));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p2", _courseOn));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p3", _groupName));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p4", (int)_groupSponsorship.TypeOfSponsorship));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p5", (int)_formatOfEducation.FormatType));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p6", -1));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p7", _creationYear));
        cmd.Parameters.Add(new NpgsqlParameter<string?>("p8", _sequenceLetter));
        cmd.Parameters.Add(new NpgsqlParameter<bool>("p9", _nameGenerated));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p10", _historySequenceId));
        cmd.Parameters.Add(new NpgsqlParameter<bool>("p11", _isActive));
        using (cmd)
        {
            using var reader = cmd.ExecuteReader();
            reader.Read();
            _id = (int)reader["group_id"];
        }
        return ResultWithoutValue.Success();
    }

    // хранить букву в базе
    // если не получается получить последнюю, то буква не указывается
    private string GetSequenceLetter(ObservableTransaction? scope = null)
    {
        NpgsqlConnection? conn = scope == null ? Utils.GetAndOpenConnectionFactory().Result : null;
        string cmdText =
        "SELECT MAX(letter) AS letter FROM educational_group WHERE letter IS NOT NULL AND " +
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
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", (int)_educationProgram.Id!));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p2", (int)_groupSponsorship.TypeOfSponsorship));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p3", (int)_formatOfEducation.FormatType));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p4", _creationYear));
        string result = string.Empty;
        using (cmd)
        {
            using var reader = cmd.ExecuteReader();
            if (!reader.HasRows)
            {
                return result;
            }
            reader.Read();
            if (reader["letter"].GetType() == typeof(DBNull))
            {
                return result;
            }
            result = ((char)(((string)reader["letter"])[0] + 1)).ToString();
        }
        conn?.Dispose();
        return result;
    }

    private static int GetNextSequenceId(ObservableTransaction? scope = null)
    {
        NpgsqlConnection? conn = scope == null ? Utils.GetAndOpenConnectionFactory().Result : null;
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
        using (cmd)
        {
            using var reader = cmd.ExecuteReader();
            if (!reader.HasRows)
            {
                return result;
            }
            reader.Read();
            if (reader["seq_id"].GetType() == typeof(DBNull))
            {
                return result;
            }
            result = (int)reader["seq_id"] + 1;
        }
        conn?.Dispose();
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

    public static IReadOnlyCollection<GroupModel> FindGroupsByName(QueryLimits limits, string? name, bool onlyActive)
    {
        var where = onlyActive ? new ComplexWhereCondition(new WhereCondition(new Column("is_active", "educational_group"))) : null;
        if (name == string.Empty || string.IsNullOrWhiteSpace(name) || name.Length < 2)
        {
            return FindGroups(limits, additionalConditions: where).Result;
        }
        var parameters = new SQLParameterCollection();
        var p1 = parameters.Add<string>(name.ToLower() + "%");
        var secondWhere = new ComplexWhereCondition(
            new WhereCondition(
                new Column("lower", "group_name", "educational_group", null),
                p1,
                WhereCondition.Relations.Like
            )
        );
        if (where is not null)
        {
            where = new ComplexWhereCondition(where, secondWhere, ComplexWhereCondition.ConditionRelation.AND);
        }
        else
        {
            where = secondWhere;
        }
        return FindGroups(limits, additionalConditions: where, addtitionalParameters: parameters).Result;
    }

    public GroupModel Copy()
    {
        var newGroup = new GroupModel
        {
            _courseOn = _courseOn,
            _creationYear = _creationYear,
            _educationProgram = _educationProgram,
            _formatOfEducation = _formatOfEducation,
            _groupName = _groupName,
            _groupSpecialTeachingCondition = _groupSpecialTeachingCondition,
            _groupSponsorship = _groupSponsorship,
            _historySequenceId = _historySequenceId,
            _id = _id,
            _nameGenerated = _nameGenerated,
            _sequenceLetter = _sequenceLetter
        };
        return newGroup;
    }

    private string GenerateGroupName()
    {
        if (!_nameGenerated)
        {
            return string.Empty;
        }
        else
        {
            return _educationProgram.FgosPrefix +
                _courseOn.ToString() +
                (_creationYear % 100).ToString() +
                _educationProgram.QualificationPostfix +
                (_sequenceLetter ?? "") +
                _formatOfEducation.GroupNamePostfix +
                _groupSponsorship.GroupNamePostfix;
        }
    }

}

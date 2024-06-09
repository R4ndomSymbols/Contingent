using Contingent.Models.Domain.Specialties;
using Npgsql;
using Contingent.Utilities;
using Contingent.Utilities.Validation;
using Contingent.SQL;
using Contingent.Controllers.DTO.In;
using Contingent.Models.Domain.Flow.History;
using Contingent.Models.Infrastructure;
using Microsoft.AspNetCore.Routing.Tree;

namespace Contingent.Models.Domain.Groups;

// при записи в базу создается не группа, а целый набор групп до выпускного курса
// пользователь может создавать только группы начального курса
// группы после 11 класса 

// Агрегирует специальность, подружает ее самостоятельно

public class GroupModel
{
    // набор групп, которые должны быть сохранены вместе с главной
    private List<GroupModel> _threadRemainings = new();
    private bool _changed = false;
    public static string InvalidNamePlaceholder => "Нет";
    private int _id;
    private SpecialtyModel _educationProgram;
    private int _courseOn;
    private string _groupName;
    private GroupEducationFormat _formatOfEducation;
    private GroupSponsorship _groupSponsorship;
    private int _creationYear;
    private string _sequenceLetter;
    private bool _nameGenerated;
    private int _historyThreadId;
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
    public SpecialtyModel EducationProgram
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
        get => _historyThreadId;
    }
    public string GroupName
    {
        get => _groupName;
    }
    public string ThreadNames
    {
        get
        {
            if (_id == Utils.INVALID_ID)
            {
                return string.Join(", ", new[] { this }.Concat(_threadRemainings).Select(x => x.GroupName));
            }
            else
            {
                return string.Join(", ", FindGroupsByThread(new QueryLimits(0, 6), _historyThreadId).Select(x => x.GroupName));
            }
        }
    }
    public int CreationYear
    {
        get => _creationYear;
    }
    public bool IsNameGenerated
    {
        get => _nameGenerated;
    }
    public bool IsActive
    {
        get => _isActive;
        set
        {
            _changed = true;
            _isActive = value;
        }

    }

    private GroupModel()
    {
        _id = Utils.INVALID_ID;
        _educationProgram = null!;
        _formatOfEducation = null!; // заменить на пустые, но не null
        _groupName = string.Empty;
        _groupSponsorship = null!;

    }

    public static Mapper<GroupModel> GetMapper(Column? source, JoinSection.JoinType joinType = JoinSection.JoinType.InnerJoin)
    {
        var specialtyMapper = SpecialtyModel.GetMapper(new Column("program_id", "educational_group"), JoinSection.JoinType.LeftJoin);
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
                group._sequenceLetter = reader["letter"] is DBNull ? "" : (string)reader["letter"];
                group._nameGenerated = (bool)reader["name_generated"];
                group._groupName = (string)reader["group_name"];
                var specialty = specialtyMapper.Map(reader);
                group._educationProgram = specialty.ResultObject;
                group._historyThreadId = (int)reader["group_sequence_id"];
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
        groupMapper.AssumeChild(specialtyMapper);
        return groupMapper;
    }


    // только первый курс
    public static Result<GroupModel> Build(GroupInDTO? dtoIn, ObservableTransaction? scope)
    {
        if (dtoIn is null)
        {
            return Result<GroupModel>.Failure(new ValidationError("dto не может быть null"));
        }
        GroupModel built = new();
        Result<GroupModel> result;
        if (dtoIn.AutogenerateName)
        {
            result = ProcessAuto(built, dtoIn);
            if (result.IsSuccess)
            {
                var group = result.ResultObject;
                group._threadRemainings.AddRange(group.SupplementGroupSequence());
            }
        }
        else
        {
            result = ProcessManual(built, dtoIn);
        }
        return result;

        Result<GroupModel> ProcessAuto(GroupModel model, GroupInDTO dto)
        {
            IList<ValidationError> errors = new List<ValidationError>();
            if (errors.IsValidRule(
                int.TryParse(dto.CreationYear, out int result) &&
                result > 0 &&
                Period.OrganizationLifetime.IsWithin(new DateTime(result, 1, 1)),
                message: "Дата создания указана неверно",
                propName: nameof(CreationYear)
            ))
            {
                model._creationYear = result;
            }
            if (errors.IsValidRule(
                GroupEducationFormat.TryGetByTypeCode(dto.EduFormatCode, out GroupEducationFormat? type) && type!.IsDefined(),
                message: "Тип обучения указан неверно",
                propName: nameof(FormatOfEducation)
            ))
            {
                model._formatOfEducation = type!;
            }
            if (errors.IsValidRule(
                GroupSponsorship.TryGetByTypeCode(dto.SponsorshipTypeCode, out GroupSponsorship? sponsorship) && sponsorship!.IsDefined(),
                message: "Тип финансирования указан неверно",
                propName: nameof(CreationYear)
             ))
            {
                model._groupSponsorship = sponsorship!;
            }

            var found = SpecialtyModel.GetById(dto.EduProgramId).Result;

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
            // для генерации имени нужно знать:
            // курс группы, специальность группы, тип обучения и тип посещения,
            // так же группа может быть с 11 класса (не учитывается)
            // порядковый номер группы среди других таких же групп (совпадает специальность и курс)
            model._courseOn = 1;
            model._isActive = true;
            model._historyThreadId = GetNextSequenceId(scope);
            model._nameGenerated = dto.AutogenerateName;
            model._sequenceLetter = model.GetSequenceLetter(scope);
            model._groupName = model.GenerateGroupName();
            return Result<GroupModel>.Success(model);

        }

        Result<GroupModel> ProcessManual(GroupModel model, GroupInDTO dto)
        {
            IList<ValidationError> errors = new List<ValidationError>();
            if (errors.IsValidRule(
                ValidatorCollection.CheckStringPattern(dto.GroupName, ValidatorCollection.OnlyText),
                message: "Неверно указано имя группы",
                propName: nameof(GroupName)
            ))
            {
                model._groupName = dto.GroupName;
            }
            var ancestor = GetGroupById(dto.PreviousGroupId);

            // для отключения сообщений о null в ancestor
            // для случая, когда указана родительская группа
            if (errors.IsValidRule(
                ancestor is not null,
                message: "Не указан родитель",
                propName: "AncestorGroup"
            ) && ancestor is not null)
            {
                if (errors.IsValidRule(
                    ancestor._courseOn + 1 > ancestor._educationProgram.CourseCount,
                    message: "Группа не может быть предшественницей указанной",
                    propName: "AncestorGroup"
                ))
                {
                    model._courseOn = ancestor._courseOn + 1;
                }
                if (errors.IsValidRule(
                    CheckUniqueSequence(ancestor._historyThreadId, model._courseOn),
                    message: string.Format("Группа такого курса {0} уже существует в этом потоке", model._courseOn),
                    propName: "AncestorGroup"
                ))
                {
                    model._historyThreadId = ancestor._historyThreadId;
                }
                if (errors.Any())
                {
                    return Result<GroupModel>.Failure(errors);
                }
                model._creationYear = ancestor._creationYear;
                model._educationProgram = ancestor._educationProgram;
                model._formatOfEducation = ancestor._formatOfEducation;
                model._groupSponsorship = ancestor._groupSponsorship;
                model._isActive = true;
                model._nameGenerated = false;
                return Result<GroupModel>.Success(model);
            }
            else
            {
                if (errors.IsValidRule(
                    int.TryParse(dto.CreationYear, out int result) &&
                    result > 0 &&
                    Period.OrganizationLifetime.IsWithin(new DateTime(result, 1, 1)),
                    message: "Дата создания указана неверно",
                    propName: nameof(CreationYear)
                ))
                {
                    model._creationYear = result;
                }
                if (errors.IsValidRule(
                    GroupEducationFormat.TryGetByTypeCode(dto.EduFormatCode, out GroupEducationFormat? type) && type!.IsDefined(),
                    message: "Тип обучения указан неверно",
                    propName: nameof(FormatOfEducation)
                ))
                {
                    model._formatOfEducation = type!;
                }
                if (errors.IsValidRule(
                    GroupSponsorship.TryGetByTypeCode(dto.SponsorshipTypeCode, out GroupSponsorship? sponsorship) && sponsorship!.IsDefined(),
                    message: "Тип финансирования указан неверно",
                    propName: nameof(CreationYear)
                ))
                {
                    model._groupSponsorship = sponsorship!;
                }

                var found = SpecialtyModel.GetById(dto.EduProgramId).Result;

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
                // для генерации имени нужно знать:
                // курс группы, специальность группы, тип обучения и тип посещения,
                // так же группа может быть с 11 класса (не учитывается)
                // порядковый номер группы среди других таких же групп (совпадает специальность и курс)
                model._courseOn = 1;
                model._isActive = true;
                model._historyThreadId = GetNextSequenceId();
                model._nameGenerated = false;
                model._sequenceLetter = "";
                return Result<GroupModel>.Success(model);
            }
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

    public ResultWithoutValue Save(ObservableTransaction? scope = null)
    {
        if (Utils.IsValidId(_id))
        {
            return Update(scope);
        }
        var result = SaveBase(scope);
        if (result.IsFailure)
        {
            return result;
        }
        foreach (var toSave in _threadRemainings)
        {
            result = toSave.SaveBase(scope);
            if (result.IsFailure)
            {
                return result;
            }
        }
        _threadRemainings.Clear();
        return ResultWithoutValue.Success();
    }

    private ResultWithoutValue SaveBase(ObservableTransaction? scope)
    {
        using NpgsqlConnection? conn = Utils.GetAndOpenConnectionFactory().Result;
        string cmdText = "INSERT INTO public.educational_group( " +
            " program_id, course_on, group_name, type_of_financing, " +
            " form_of_education, education_program_type, creation_year, letter, name_generated, group_sequence_id, is_active) " +
            " VALUES (@p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11) RETURNING group_id";
        NpgsqlCommand cmd;
        if (scope is not null)
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
        cmd.Parameters.Add(new NpgsqlParameter<string>("p8", _sequenceLetter));
        cmd.Parameters.Add(new NpgsqlParameter<bool>("p9", _nameGenerated));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p10", _historyThreadId));
        cmd.Parameters.Add(new NpgsqlParameter<bool>("p11", _isActive));
        using (cmd)
        {
            using var reader = cmd.ExecuteReader();
            reader.Read();
            _id = (int)reader["group_id"];
        }
        return ResultWithoutValue.Success();
    }
    private ResultWithoutValue Update(ObservableTransaction? scope = null)
    {
        if (!_changed)
        {
            return ResultWithoutValue.Success();
        }
        using var connection = Utils.GetAndOpenConnectionFactory().Result;
        string cmdText = "UPDATE educational_group SET is_active = @p1 WHERE group_id = @p2";
        NpgsqlCommand cmd;
        if (scope is not null)
        {
            cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }
        else
        {
            cmd = new NpgsqlCommand(cmdText, connection);
        }
        cmd.Parameters.Add(new NpgsqlParameter<bool>("p1", _isActive));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p2", _id));
        using (cmd)
        {
            cmd.ExecuteNonQuery();
        }
        _changed = false;
        return ResultWithoutValue.Success();
    }


    // хранить букву в базе
    // если не получается получить последнюю, то буква не указывается
    private string GetSequenceLetter(ObservableTransaction? scope)
    {
        using NpgsqlConnection? conn = Utils.GetAndOpenConnectionFactory().Result;
        string cmdText =
        "SELECT MAX(letter) AS letter FROM educational_group WHERE letter IS NOT NULL AND " +
        " program_id = @p1 AND type_of_financing = @p2 AND form_of_education = @p3 " +
        " AND creation_year = @p4 AND name_generated";
        NpgsqlCommand cmd;
        if (scope is not null)
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
            result = (string)reader["letter"];
            result = result == string.Empty ? "Я" : result;
            result = ((char)(result[0] + 1)).ToString();
        }
        return result;
    }

    private static int GetNextSequenceId(ObservableTransaction? scope = null)
    {
        using NpgsqlConnection? conn = Utils.GetAndOpenConnectionFactory().Result;
        string cmdText = "SELECT MAX(group_sequence_id) AS seq_id FROM educational_group";
        NpgsqlCommand cmd;
        if (scope is not null)
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

    public static IReadOnlyCollection<GroupModel> FindGroupsByName(QueryLimits limits, string? name, bool onlyActive, bool strict = false)
    {
        var where = onlyActive ? new ComplexWhereCondition(new WhereCondition(new Column("is_active", "educational_group"))) : ComplexWhereCondition.Empty;
        if (name == string.Empty || string.IsNullOrWhiteSpace(name) || name.Length < 2)
        {
            return FindGroups(limits, additionalConditions: where).Result;
        }
        var parameters = new SQLParameterCollection();
        var correct = name.Trim().ToLower();
        where = where.Unite(
            ComplexWhereCondition.ConditionRelation.AND,
            GetFilterForGroup(strict ? correct : "%" + correct + "%", ref parameters)
        );
        return FindGroups(limits, additionalConditions: where, addtitionalParameters: parameters).Result;
    }
    public static IReadOnlyCollection<GroupModel> FindGroupsByThread(QueryLimits limits, int threadId, int? courseOn = null)
    {
        var parameters = new SQLParameterCollection();
        var where = courseOn is not null ?
            new ComplexWhereCondition(
                new WhereCondition(
                    new Column("course_on", "educational_group"),
                    parameters.Add((int)courseOn),
                    WhereCondition.Relations.Equal
                    )
                )
        : ComplexWhereCondition.Empty;

        where = where.Unite(
            ComplexWhereCondition.ConditionRelation.AND,
            new ComplexWhereCondition(
                new WhereCondition(
                    new Column("group_sequence_id", "educational_group"),
                    parameters.Add(threadId),
                    WhereCondition.Relations.Equal
                    )
                )
        );
        return FindGroups(limits, additionalConditions: where, addtitionalParameters: parameters).Result;
    }

    public static ComplexWhereCondition GetFilterForGroup(string name, ref SQLParameterCollection parameters)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrEmpty(name))
        {
            throw new Exception("Запрос не может быть сформирован с такими параметрами");
        }
        var p1 = parameters.Add<string>(name);
        return new ComplexWhereCondition(
            new WhereCondition(
                new Column("lower", "group_name", "educational_group", null),
                p1,
                WhereCondition.Relations.Like
        ));
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
            _historyThreadId = _historyThreadId,
            _id = Utils.INVALID_ID,
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

    private GroupModel[] SupplementGroupSequence()
    {
        if (!_nameGenerated || _courseOn == _educationProgram.CourseCount)
        {
            return Array.Empty<GroupModel>();
        }
        var groups = new GroupModel[_educationProgram.CourseCount - this._courseOn];
        for (int course = this._courseOn + 1; course <= _educationProgram.CourseCount; course++)
        {
            int index = course - (this._courseOn + 1);
            groups[index] = this.Copy();
            groups[index]._courseOn = course;
            groups[index]._groupName = groups[index].GenerateGroupName();
        }
        return groups;
    }

    public bool IsAllowedByEnrollmentPeriod(Period period)
    {
        var correctedDate = new DateTime(_creationYear, 1, 1).AddYears(_courseOn - 1);
        return period.IsWithin(correctedDate);
    }
    public bool IsOnTheSameThread(GroupModel? group)
    {
        if (group is null)
        {
            return false;
        }
        return _historyThreadId == group._historyThreadId;
    }
    public bool IsGraduationGroup()
    {
        return _courseOn == _educationProgram.CourseCount;
    }

    public GroupRelations GetRelationTo(GroupModel? group)
    {
        if (group is null)
        {
            return GroupRelations.None;
        }
        if (!IsOnTheSameThread(group))
        {
            if (_educationProgram.Equals(group._educationProgram)
            && _courseOn == group._courseOn
            && _creationYear == group._creationYear
            && _formatOfEducation.GroupNamePostfix == group._formatOfEducation.GroupNamePostfix
            && _groupSponsorship.GroupNamePostfix == group._groupSponsorship.GroupNamePostfix
            )
            {
                return GroupRelations.Sibling;
            }
            return GroupRelations.None;
        }
        if (_courseOn - group._courseOn == 1)
        {
            return GroupRelations.DirectChild;
        }
        if (_courseOn - group._courseOn == -1)
        {
            return GroupRelations.DirectParent;
        }
        return GroupRelations.Other;
    }

    public GroupModel? GetSuccessor()
    {
        if (_id == Utils.INVALID_ID)
        {
            return null;
        }
        if (IsGraduationGroup())
        {
            return null;
        }
        return FindGroupsByThread(new QueryLimits(0, _educationProgram.CourseCount), _historyThreadId, _courseOn + 1).FirstOrDefault();

    }

    private static bool CheckUniqueSequence(int groupSequence, int courseOn)
    {
        using var conn = Utils.GetAndOpenConnectionFactory().Result;
        string cmdText = "SELECT group_id FROM educational_group WHERE course_on = @p1 AND group_sequence_id = @p2";
        using var cmd = new NpgsqlCommand(cmdText, conn);
        cmd.Parameters.Add(new NpgsqlParameter("p1", courseOn));
        cmd.Parameters.Add(new NpgsqlParameter("p2", groupSequence));
        using var reader = cmd.ExecuteReader();
        return !reader.HasRows;
    }
}



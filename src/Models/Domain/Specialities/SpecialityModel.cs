using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Models.JSON;
using Contingent.SQL;
using Utilities;
using Utilities.Validation;
using Contingent.Models.Domain.Students;

namespace Contingent.Models.Domain.Specialities;

public class SpecialityModel
{
    public const int MINIMAL_COURSE_COUNT = 1;
    public const int MAXIMUM_COURSE_COUNT = 6;
    private int? _id;
    private string _fgosCode;
    private string _fgosName;
    private string _qualification;
    private string _groupNameFgosPrefix;
    private string? _groupNameQualificationPostfix;
    private int _courseCount;
    private LevelOfEducation _levelIn;
    private LevelOfEducation _levelOut;
    private TeachingDepth _teachingDepth;
    private TrainingProgram _trainingProgram;

    public int? Id
    {
        get => _id;
    }
    public string FgosCode
    {
        get => _fgosCode;
    }
    public string FgosName
    {
        get => _fgosName;
    }
    public string Qualification
    {
        get => _qualification;
    }
    public string FgosPrefix
    {
        get => _groupNameFgosPrefix;
    }

    public string QualificationPostfix
    {
        get => _groupNameQualificationPostfix ?? "";
    }
    public int CourseCount
    {
        get => _courseCount;
    }
    public LevelOfEducation EducationalLevelIn
    {
        get => _levelIn;
    }
    public LevelOfEducation EducationalLevelOut
    {
        get => _levelOut;
    }
    public TeachingDepth TeachingLevel
    {
        get => _teachingDepth;
    }
    public TrainingProgram ProgramType
    {
        get => _trainingProgram;
    }

    private SpecialityModel()
    {
        _id = null;
        _fgosCode = "";
        _fgosName = "";
        _qualification = "";
        _groupNameFgosPrefix = "";
        _levelIn = LevelOfEducation.None;
        _levelOut = LevelOfEducation.None;
        _teachingDepth = TeachingDepth.None;
        _trainingProgram = TrainingProgram.None;
    }

    public static Mapper<SpecialityModel> GetMapper(Column? source, JoinSection.JoinType joinType = JoinSection.JoinType.InnerJoin)
    {
        var mapper = new Mapper<SpecialityModel>(
            (reader) =>
            {
                if (reader["id_spec"].GetType() == typeof(DBNull))
                {
                    return QueryResult<SpecialityModel>.NotFound();
                }
                var mapped = new SpecialityModel();
                mapped._id = (int)reader["id_spec"];
                mapped._fgosCode = (string)reader["fgos_code"];
                mapped._fgosName = (string)reader["fgos_name"];
                mapped._qualification = (string)reader["qualification"];
                mapped._groupNameFgosPrefix = (string)reader["group_prefix"];
                mapped._groupNameQualificationPostfix = reader["group_postfix"].GetType() == typeof(DBNull) ? null : (string)reader["group_postfix"];
                mapped._courseCount = (int)reader["course_count"];
                mapped._teachingDepth = TeachingDepth.GetByTypeCode((int)reader["knowledge_depth"]);
                mapped._levelIn = LevelOfEducation.GetByLevelCode((int)reader["speciality_in_education_level"]);
                mapped._levelOut = LevelOfEducation.GetByLevelCode((int)reader["speciality_out_education_level"]);
                mapped._trainingProgram = TrainingProgram.GetByType((int)reader["training_program_type"]);
                return QueryResult<SpecialityModel>.Found(mapped);
            }, new List<Column>(){
                new Column("id", "id_spec", "educational_program"),
                new Column("group_prefix", "educational_program"),
                new Column("fgos_name", "educational_program"),
                new Column("qualification", "educational_program"),
                new Column("fgos_code", "educational_program"),
                new Column("group_postfix", "educational_program"),
                new Column("course_count", "educational_program"),
                new Column("knowledge_depth", "educational_program"),
                new Column("speciality_in_education_level", "educational_program"),
                new Column("speciality_out_education_level", "educational_program"),
                new Column("training_program_type", "educational_program")
            }
        );
        if (source is not null)
        {
            mapper.PathTo.AddHead(joinType, source, new Column("id", "educational_program"));
        }
        return mapper;
    }

    public static Result<SpecialityModel> Build(SpecialityDTO dto)
    {
        if (dto is null)
        {
            return Result<SpecialityModel>.Failure(new ValidationError("dto не может быть null"));
        }
        IList<ValidationError?> errors = new List<ValidationError?>();
        SpecialityModel model = new();
        SpecialityModel? fromDb = null;
        if (dto.Id is not null)
        {
            fromDb = SpecialityModel.GetById(dto.Id).Result;
            errors.IsValidRule(
                fromDb is not null,
                "Id специальности указан неверно",
                nameof(Id)
            );
            // значения, которые тут указаны
            // не могут меняться
            if (fromDb is not null)
            {
                model._id = fromDb._id;
                model._courseCount = fromDb._courseCount;
                model._groupNameFgosPrefix = fromDb._groupNameFgosPrefix;
                model._groupNameQualificationPostfix = fromDb._groupNameQualificationPostfix;
                model._levelIn = fromDb._levelIn;
                model._levelOut = fromDb._levelOut;
            }

        }

        if (fromDb is null)
        {
            if (
                errors.IsValidRule(
                ValidatorCollection.CheckStringPattern(dto.FgosPrefix, ValidatorCollection.OnlyLetters),
                message: "Префикс имени группы по ФГОС не указан или указан неверно",
                propName: nameof(FgosPrefix)
            ))
            {
                model._groupNameFgosPrefix = dto.FgosPrefix;
            }

            if (errors.IsValidRule(
                ValidatorCollection.CheckStringPattern(dto.QualificationPostfix, ValidatorCollection.OnlyLetters) || dto.QualificationPostfix == string.Empty,
                message: "Постфикс квалификации указан неверно",
                propName: nameof(QualificationPostfix)
            ))
            {
                model._groupNameQualificationPostfix = dto.QualificationPostfix;
            }
            if (errors.IsValidRule(
                dto.CourseCount >= MINIMAL_COURSE_COUNT && dto.CourseCount <= MAXIMUM_COURSE_COUNT,
                message: "Количество курсов указано неверно",
                propName: nameof(CourseCount)
            ))
            {
                model._courseCount = dto.CourseCount;
            }

            if (errors.IsValidRule(
                LevelOfEducation.TryGetByLevelCode(dto.EducationalLevelIn, out LevelOfEducation? typeIn) &&
                typeIn!.IsDefined(),
                message: "Уровень образования (входной) указан неверно",
                propName: nameof(EducationalLevelIn)
            ))
            {
                model._levelIn = typeIn!;
            }

            if (errors.IsValidRule(
                LevelOfEducation.TryGetByLevelCode(dto.EducationalLevelOut, out LevelOfEducation? typeOut) && typeOut!.IsDefined(),
                message: "Уровень образования (выходной) указан неверно",
                propName: nameof(EducationalLevelOut)
            ))
            {
                model._levelOut = typeOut!;
            }
        }


        if (errors.IsValidRule(
            ValidatorCollection.CheckStringPattern(dto.FgosCode, ValidatorCollection.FgosCode),
            message: "Номер ФГОС не соответствует формату или не указан",
            propName: nameof(FgosCode)
        ))
        {
            model._fgosCode = dto.FgosCode;
        }
        if (
            errors.IsValidRule(
            ValidatorCollection.CheckStringPattern(dto.FgosName, ValidatorCollection.OnlyText),
            message: "Название ФГОС не соответствует формату или не указан",
            propName: nameof(FgosName)
        ))
        {
            model._fgosName = dto.FgosName;
        }

        if (
            errors.IsValidRule(
            ValidatorCollection.CheckStringPattern(dto.Qualification, ValidatorCollection.OnlyText),
            message: "Квалификация указана или указана неверно",
            propName: nameof(Qualification)
        ))
        {
            model._qualification = dto.Qualification;
        }

        if (errors.IsValidRule(
            TeachingDepth.TryGetByTypeCode(dto.TeachingDepthCode),
            message: "Уровень программы подготовки указан неверно",
            propName: nameof(TeachingLevel)
        ))
        {
            model._teachingDepth = TeachingDepth.GetByTypeCode(dto.TeachingDepthCode);
        }
        if (errors.IsValidRule(
            TrainingProgram.TryGetByType(dto.ProgramType, out TrainingProgram? result),
            message: "Уровень подготовки указан неверно",
            propName: nameof(ProgramType)
        ))
        {
            model._trainingProgram = result!;
        }
        if (errors.Any())
        {
            return Result<SpecialityModel>.Failure(errors);
        }
        else
        {
            return Result<SpecialityModel>.Success(model);
        }
    }

    public static async Task<IReadOnlyCollection<SpecialityModel>> FindSpecialities(QueryLimits limits, JoinSection? additionalJoins = null, OrderByCondition? addtioinalOrderBy = null, ComplexWhereCondition? addtioinalWhere = null, SQLParameterCollection? additionalParameters = null)
    {
        using var conn = await Utils.GetAndOpenConnectionFactory();
        var mapper = GetMapper(null);
        var selectQuery = SelectQuery<SpecialityModel>.Init("educational_program")
        .AddMapper(mapper)
        .AddJoins(additionalJoins)
        .AddOrderByStatement(addtioinalOrderBy)
        .AddWhereStatement(addtioinalWhere)
        .AddParameters(additionalParameters)
        .Finish();
        if (selectQuery.IsFailure)
        {
            throw new Exception("Запрос не может быть составлен");
        }
        return await selectQuery.ResultObject.Execute(conn, limits);

    }

    public void Save(ObservableTransaction? scope = null)
    {
        if (_id is not null || _id == Utils.INVALID_ID)
        {
            Update(scope);
            return;
        }


        NpgsqlConnection? conn = scope == null ? Utils.GetAndOpenConnectionFactory().Result : null;
        string cmdText = "INSERT INTO public.educational_program( " +
        " fgos_code, fgos_name, qualification, course_count, " +
        " speciality_out_education_level, speciality_in_education_level, knowledge_depth, group_prefix, group_postfix, training_program_type) " +
        " VALUES (@p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9,@p10) RETURNING id";
        NpgsqlCommand cmd;
        if (scope != null)
        {
            cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }
        else
        {
            cmd = new NpgsqlCommand(cmdText, conn);
        }
        cmd.Parameters.Add(new NpgsqlParameter<string>("p1", _fgosCode));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p2", _fgosName));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p3", _qualification));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p4", _courseCount));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p5", (int)_levelOut.LevelCode));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p6", (int)_levelIn.LevelCode));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p7", (int)_teachingDepth.Level));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p8", _groupNameFgosPrefix));
        cmd.Parameters.Add(new NpgsqlParameter<string?>("p9", _groupNameQualificationPostfix));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p10", (int)_trainingProgram.Type));

        using (cmd)
        {
            using var reader = cmd.ExecuteReader();
            reader.Read();
            _id = (int)reader["id"];
        }
        conn?.Dispose();
    }

    public void Update(ObservableTransaction? scope = null)
    {
        string updateText =
        // изменить id, количество курсов, выходной и необходимый уровень образования нельзя
        "UPDATE public.educational_program " +
        "SET fgos_code = @p1, fgos_name = @p2, qualification = @p3, knowledge_depth = @p4, training_program_type = @p5 " +
        "WHERE id = @p6";
        NpgsqlCommand cmd;
        using var connection = scope is null ? Utils.GetAndOpenConnectionFactory().Result : null;
        if (scope is null)
        {
            cmd = new NpgsqlCommand(updateText, connection);
        }
        else
        {
            cmd = new NpgsqlCommand(updateText, scope.Connection, scope.Transaction);
        }
        cmd.Parameters.Add(new NpgsqlParameter<string>("p1", _fgosCode));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p2", _fgosName));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p3", _qualification));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p4", (int)_teachingDepth.Level));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p5", (int)_trainingProgram.Type));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p6", (int)_id));
        using (cmd)
        {
            cmd.ExecuteNonQuery();
            return;
        }
    }

    public static async Task<SpecialityModel?> GetById(int? id, ObservableTransaction? scope = null)
    {
        if (id is null)
        {
            return null;
        }
        var parameters = new SQLParameterCollection();
        var p1 = parameters.Add((int)id);
        var where = new ComplexWhereCondition(new WhereCondition(
            new Column("id", "educational_program"),
            p1,
            WhereCondition.Relations.Equal
        ));
        var found = await FindSpecialities(new QueryLimits(0, 1), additionalParameters: parameters, addtioinalWhere: where);
        if (found.Any())
        {
            return found.First();
        }
        else
        {
            return null;
        }
    }
    public static async Task<bool> IsIdExists(int id, ObservableTransaction? scope)
    {
        NpgsqlConnection? conn = scope == null ? await Utils.GetAndOpenConnectionFactory() : null;
        string cmdText = "SELECT (EXISTS(SELECT 1 FROM educational_program WHERE id = @p1))";
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
        bool result = false;
        await using (cmd)
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            result = (bool)reader["exists"];
        }
        if (conn != null)
        {
            await conn.DisposeAsync();
        }
        return result;
    }
    public static async Task<List<SpecialitySuggestionJSON>> GetSuggestions(string? searchText, ObservableTransaction? scope)
    {


        NpgsqlConnection? conn = scope == null ? await Utils.GetAndOpenConnectionFactory() : null;
        string cmdText = "";
        if (searchText != null)
        {
            cmdText = "SELECT id, fgos_code, fgos_name, qualification FROM public.educational_program WHERE " +
            " fgos_name || ' ' || qualification  || ' ' || fgos_code LIKE @p1";
        }
        else
        {
            cmdText = "SELECT id, fgos_code, fgos_name, qualification FROM public.educational_program";
        }

        NpgsqlCommand cmd;
        if (scope != null)
        {
            cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }
        else
        {
            cmd = new NpgsqlCommand(cmdText, conn);
        }
        if (searchText != null)
        {
            cmd.Parameters.Add(new NpgsqlParameter<string>("p1", "%" + searchText + "%"));
        }

        List<SpecialitySuggestionJSON> result = new List<SpecialitySuggestionJSON>();
        await using (cmd)
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows)
            {
                return result;
            }
            while (await reader.ReadAsync())
            {
                result.Add(
                    new SpecialitySuggestionJSON(
                        (int)reader["id"],
                        (string)reader["fgos_name"],
                        (string)reader["qualification"],
                        (string)reader["fgos_code"]
                    )
                );
            };

        }
        if (conn != null)
        {
            await conn.DisposeAsync();
        }
        return result;
    }

    public bool IsStudentAllowedByEducationLevel(StudentModel student)
    {
        return student.Education.IsHigherThan(_levelIn);
    }

    public static IEnumerable<SpecialityModel> GetAll()
    {
        return FindSpecialities(new QueryLimits(0, 2000)).Result;

    }

    public override string ToString()
    {
        return FgosCode + " " + FgosName + " (" + Qualification + ")";
    }

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != typeof(SpecialityModel))
        {
            return false;
        }
        return ((SpecialityModel)obj)._id == this._id;
    }

}



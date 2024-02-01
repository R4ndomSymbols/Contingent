using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Npgsql;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Misc;
using StudentTracking.Models.JSON;
using StudentTracking.Models.SQL;
using Utilities;
using Utilities.Validation;

namespace StudentTracking.Models;

public class SpecialityModel
{
    public const int MINIMAL_COURSE_COUNT = 1;
    public const int MAXIMUM_COURSE_COUNT = 6;
    private int _id;
    private string _fgosCode;
    private string _fgosName;
    private string _qualification;
    private string _groupNameFgosPrefix;
    private string? _groupNameQualificationPostfix;
    private int _courseCount;
    private LevelOfEducation _levelIn;
    private LevelOfEducation _levelOut;
    private TeachingDepth _teachingDepth;
    public int Id {
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

    private SpecialityModel()
    {

    }

    public static Result<SpecialityModel?> Build(SpecialityDTO dto){
        if (dto is null){
            return Result<SpecialityModel>.Failure(new ValidationError("dto не может быть null"));
        }
        IList<ValidationError?> errors = new List<ValidationError?>();
        SpecialityModel model = new();

        if (errors.IsValidRule(
            ValidatorCollection.CheckStringPattern(dto.FgosCode, ValidatorCollection.FgosCode),
            message: "Номер ФГОС не соответствует формату или не указан",
            propName: nameof(FgosCode)
        )){
            model._fgosCode = dto.FgosCode;
        }
        if (errors.IsValidRule(
            ValidatorCollection.CheckStringPattern(dto.FgosName, ValidatorCollection.OnlyText),
            message: "Номер ФГОС не соответствует формату или не указан",
            propName: nameof(FgosName)
        )){
            model._fgosName = dto.FgosName;
        }
        if (errors.IsValidRule(
            ValidatorCollection.CheckStringPattern(dto.FgosPrefix, ValidatorCollection.OnlyLetters),
            message: "Префикс имени группы по ФГОС не указан или указан неверно",
            propName: nameof(FgosPrefix)
        )){
            model._groupNameFgosPrefix = dto.FgosPrefix;
        }
        if (errors.IsValidRule(
            ValidatorCollection.CheckStringPattern(dto.Qualification, ValidatorCollection.OnlyText),
            message: "Квалификация указана или указана неверно",
            propName: nameof(Qualification)
        )){
            model._qualification = dto.Qualification;
        }

        if (dto.QualificationPostfix == null){
            model._groupNameQualificationPostfix = null;
        }
        else if (errors.IsValidRule(
            ValidatorCollection.CheckStringPattern(dto.QualificationPostfix, ValidatorCollection.OnlyLetters),
            message: "Постфикс квалификации указан неверно",
            propName: nameof(QualificationPostfix)
        )){
            model._groupNameQualificationPostfix = dto.QualificationPostfix;
        }

        if (errors.IsValidRule(
            dto.CourseCount >= MINIMAL_COURSE_COUNT && dto.CourseCount <= MAXIMUM_COURSE_COUNT,
            message: "Количество курсов указано неверно",
            propName: nameof(CourseCount)
        )){
            model._courseCount = dto.CourseCount;
        }

        if (errors.IsValidRule(
            LevelOfEducation.TryGetByLevelCode(dto.EducationalLevelIn),
            message: "Уровень образования (входной) указан неверно",
            propName: nameof(CourseCount)
        )){
            model._levelIn = LevelOfEducation.GetByLevelCode(dto.EducationalLevelIn);
        }

        if (errors.IsValidRule(
            LevelOfEducation.TryGetByLevelCode(dto.EducationalLevelOut),
            message: "Уровень образования (выходной) указан неверно",
            propName: nameof(CourseCount)
        )){
            model._levelOut = LevelOfEducation.GetByLevelCode(dto.EducationalLevelOut);
        }
        if (errors.IsValidRule(
            TeachingDepth.TryGetByTypeCode(dto.TeachingDepth),
            message: "Уровень образования (выходной) указан неверно",
            propName: nameof(CourseCount)
        )){
            model._teachingDepth = TeachingDepth.GetByTypeCode(dto.TeachingDepth);
        }
        if (errors.Any()){
            return Result<SpecialityModel>.Failure(errors);
        }
        else {
            return Result<SpecialityModel>.Success(model);
        }
    }

    public static async Task<IReadOnlyCollection<SpecialityModel>> FindSpecialities(QueryLimits limits, JoinSection? additionalJoins = null, OrderByCondition? addtioinalOrderBy = null, ComplexWhereCondition? addtioinalWhere = null, SQLParameterCollection? additionalParameters = null){
        using var conn = await Utils.GetAndOpenConnectionFactory();
        var mapper = new Mapper<SpecialityModel>(
            (reader) => {
                if (reader["id_spec"].GetType() == typeof(DBNull)){
                    return Task.Run(() => QueryResult<SpecialityModel>.NotFound());
                }
                var mapped = new SpecialityModel();
                mapped._id = (int)reader["id_spec"];
                mapped._fgosName = (string)reader["fgos_code"];
                mapped._fgosCode = (string)reader["fgos_name"];
                mapped._qualification = (string)reader["qualification"];
                mapped._groupNameFgosPrefix = (string)reader["group_prefix"];
                mapped._groupNameQualificationPostfix = reader["group_postfix"].GetType() == typeof(DBNull) ? null : (string)reader["group_postfix"];
                mapped._courseCount = (int)reader["course_count"];
                mapped._teachingDepth = TeachingDepth.GetByTypeCode((int)reader["knowledge_depth"]);
                mapped._levelIn = LevelOfEducation.GetByLevelCode((int)reader["speciality_in_education_level"]);
                mapped._levelOut = LevelOfEducation.GetByLevelCode((int)reader["speciality_out_education_level"]);
                return Task.Run(() => QueryResult<SpecialityModel>.Found(mapped));
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
                new Column("speciality_out_education_level", "educational_program")
            } 
        );
        var selectQuery = SelectQuery<SpecialityModel>.Init("educational_program")
        .AddMapper(mapper)
        .AddJoins(additionalJoins)
        .AddOrderByStatement(addtioinalOrderBy)
        .AddWhereStatement(addtioinalWhere)
        .AddParameters(additionalParameters)
        .Finish();
        if (selectQuery.IsFailure){
            throw new Exception("Запрос не может быть составлен");
        }
        return await selectQuery.ResultObject.Execute(conn, limits);

    }

    public async Task Save(ObservableTransaction? scope = null){
        
        NpgsqlConnection? conn = scope == null ? await Utils.GetAndOpenConnectionFactory() : null;
        string cmdText = "INSERT INTO public.educational_program( " +
	    " fgos_code, fgos_name, qualification, course_count, " + 
        " speciality_out_education_level, speciality_in_education_level, knowledge_depth, group_prefix, group_postfix) " +
	    " VALUES (@p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9) RETURNING id";
        NpgsqlCommand cmd;
        if (scope != null){
            cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }
        else {
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

        await using (cmd){
            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            _id = (int)reader["id"];
        }
        if (conn!=null){
            await conn.DisposeAsync();
        }   
    }

    public static async Task<SpecialityModel?> GetById(int id, ObservableTransaction? scope = null)
    {   
        var parameters = new SQLParameterCollection();
        var p1 = parameters.Add(id);
        var where = new ComplexWhereCondition(new WhereCondition(
            new Column("id", "educational_program"),
            p1,
            WhereCondition.Relations.Equal
        ));
        var found = await FindSpecialities(new QueryLimits(0,1), additionalParameters: parameters, addtioinalWhere: where);
        if (found.Any()){
            return found.First();
        }
        else {
            return null;
        } 
    }
    public static async Task<bool> IsIdExists(int id, ObservableTransaction? scope){
        NpgsqlConnection? conn = scope == null ? await Utils.GetAndOpenConnectionFactory() : null;
        string cmdText = "SELECT (EXISTS(SELECT 1 FROM educational_program WHERE id = @p1))";
        NpgsqlCommand cmd;
        if (scope != null){
            cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }
        else{
            cmd = new NpgsqlCommand(cmdText, conn);
        }
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", id));
        bool result = false;
        await using (cmd){
            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            result = (bool)reader["exists"];
        }
        if (conn!=null){
            await conn.DisposeAsync();
        }
        return result;
    }
    public static async Task<List<SpecialitySuggestionJSON>> GetSuggestions(string? searchText, ObservableTransaction? scope){
        
        
        NpgsqlConnection? conn = scope == null ? await Utils.GetAndOpenConnectionFactory() : null;
        string cmdText = "";
        if (searchText!= null){
            cmdText = "SELECT id, fgos_code, fgos_name, qualification FROM public.educational_program WHERE " +
            " fgos_name || ' ' || qualification  || ' ' || fgos_code LIKE @p1";
        }
        else {
            cmdText = "SELECT id, fgos_code, fgos_name, qualification FROM public.educational_program";
        }

        NpgsqlCommand cmd;
        if (scope != null){
            cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }
        else{
            cmd = new NpgsqlCommand(cmdText, conn);
        }
        if (searchText!=null){
             cmd.Parameters.Add(new NpgsqlParameter<string>("p1", "%" + searchText + "%"));
        }

        List<SpecialitySuggestionJSON> result = new List<SpecialitySuggestionJSON>();
        await using (cmd){
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows){
                return result;
            }
            while (await reader.ReadAsync()){
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
        if (conn!=null){
            await conn.DisposeAsync();
        }
        return result;
    }
    public SpecialitySuggestionJSON ToSuggestion(){
        return new SpecialitySuggestionJSON(_id, _fgosName, _qualification, _fgosCode);
    } 
    public override bool Equals(object? other)
    {
        if (other == null){
            return false;
        }
        if (other.GetType() != this.GetType()){
            return false;
        }
        var unboxed = (SpecialityModel)other;
        return 
            _id == unboxed._id &&
            _courseCount == unboxed._courseCount &&
            _teachingDepth == unboxed._teachingDepth &&
            _fgosCode == unboxed._fgosCode &&
            _fgosName == unboxed._fgosName &&
            _levelIn == unboxed._levelIn &&
            _levelOut == unboxed._levelOut &&
            _qualification == unboxed._qualification &&
            _groupNameQualificationPostfix == unboxed._groupNameQualificationPostfix;
    }
}



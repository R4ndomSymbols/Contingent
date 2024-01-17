using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Npgsql;
using StudentTracking.Models.Domain.Misc;
using StudentTracking.Models.JSON;
using Utilities;
using Utilities.Validation;

namespace StudentTracking.Models;

public class SpecialityModel : DbValidatedObject
{
    public const int MINIMAL_COURSE_COUNT = 1;
    public const int MAXIMUM_COURSE_COUNT = 6;
    private int _id;
    private string _fgosCode;
    private string _fgosName;
    private string _qualification;
    private string _fgosPrefix;
    private string? _qualificationPostfix;
    private int _courseCount;
    private StudentEducationalLevelRecord.EducationalLevels _levelIn;
    private StudentEducationalLevelRecord.EducationalLevels _levelOut;
    private TeachingDepth.Levels _educationDepth;
    public int Id {
        get => _id;
    }
    public string FgosCode
    {
        get => _fgosCode;
        set
        {
            if (PerformValidation(
                () => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.FgosCode),
                new ValidationError(nameof(FgosCode), "Номер ФГОС не соответствует формату или не указан")))
            {
                _fgosCode = value;
            }
        }
    }
    public string FgosName
    {
        get => _fgosName;
        set
        {
            if (PerformValidation(() => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.OnlyText)
            , new ValidationError(nameof(FgosName), "Название ФГОС имеет неверный формат или не указано"))
            )
            {
                _fgosName = value;
            }
        }
    }
    public string Qualification
    {
        get => _qualification;
        set
        {
            if (PerformValidation(() => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.OnlyText),
            new ValidationError(nameof(Qualification), "Квалификация указана или указана неверно")))
            {
                _qualification = value;
            }
        }
    }
    public string FgosPrefix
    {
        get => _fgosPrefix;
        set
        {
            if (PerformValidation(() => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.OnlyLetters),
            new ValidationError(nameof(FgosPrefix), "Префикс специальности не указан или указан неверно")))
            {
                _fgosPrefix = value;
            }
        }

    }

    public string QualificationPostfix
    {
        get => _qualificationPostfix ?? "";
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _qualificationPostfix = null;
                return;
            }
            if (PerformValidation(() => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.OnlyLetters),
            new ValidationError(nameof(QualificationPostfix), "Постфикс квалификации имеет неверный формат")))
            {
                _qualificationPostfix = value;
            }
        }
    }
    [JsonIgnore]
    public int IntCourseCount
    {
        get => _courseCount;
    }
    public int CourseCount
    {
        get => _courseCount;
        set
        {
            if (PerformValidation(
                () => ValidatorCollection.CheckRange(value, 1, 6),
                new ValidationError(nameof(CourseCount), "Указанное число превышает допустимые пределы")
            ))
            {
                _courseCount = value;
            }
        }
    }
    public int EducationalLevelIn
    {
        get => (int)_levelIn;
        set
        {
            if (PerformValidation(
                () =>
                {
                    try
                    {
                        var dummy = (StudentEducationalLevelRecord.EducationalLevels)value;
                        return true;
                    }
                    catch (InvalidCastException)
                    {
                        return false;
                    }
                }, new ValidationError(nameof(EducationalLevelIn), "Переданное значение не является допустимым")
            ))
            {
                _levelIn = (StudentEducationalLevelRecord.EducationalLevels)value;
            }
        }
    }
    public int EducationalLevelOut
    {
        get => (int)_levelOut;
        set
        {
            if (PerformValidation(
                () =>
                {
                    try
                    {
                        var dummy = (StudentEducationalLevelRecord.EducationalLevels)value;
                        return true;
                    }
                    catch (InvalidCastException)
                    {
                        return false;
                    }
                }, new ValidationError(nameof(EducationalLevelOut), "Переданное значение не является допустимым")
            ))
            {
                _levelOut = (StudentEducationalLevelRecord.EducationalLevels)value;
            }
        }
    }
    public int TeachingLevel
    {
        get => (int)_educationDepth;
        set
        {
            if (PerformValidation(
                () =>
                {
                    try
                    {
                        var dummy = (TeachingDepth.Levels)value;
                        return true;
                    }
                    catch (InvalidCastException)
                    {
                        return false;
                    }
                }, new ValidationError(nameof(TeachingLevel), "Переданное значение уровня изучения не является допустимым")
            ))
            {
                _educationDepth = (TeachingDepth.Levels)value;
            }
        }
    }

    public SpecialityModel() : base()
    {
        RegisterProperty(nameof(FgosCode));
        RegisterProperty(nameof(FgosName));
        RegisterProperty(nameof(Qualification));
        RegisterProperty(nameof(QualificationPostfix));
        RegisterProperty(nameof(FgosPrefix));
        RegisterProperty(nameof(EducationalLevelIn));
        RegisterProperty(nameof(EducationalLevelOut));
        RegisterProperty(nameof(TeachingLevel));
        RegisterProperty(nameof(CourseCount));

        _fgosName = "";
        _fgosCode = "";
        _courseCount = 0;
        _educationDepth = TeachingDepth.Levels.NotMentioned;
        _fgosPrefix = "";
        _levelIn = StudentEducationalLevelRecord.EducationalLevels.NotMentioned;
        _levelOut = StudentEducationalLevelRecord.EducationalLevels.NotMentioned;
        _qualification = "";
        _qualificationPostfix = "";
    }

    protected SpecialityModel(int id, string code, string name, string qual, string fPref) : base(RelationTypes.Bound)
    {
        _id = id;
        _fgosCode = code;
        _fgosName = name;
        _qualification = qual;
        _fgosPrefix = fPref;
    }
    public async Task Save(ObservableTransaction? scope){
        
        if (await GetCurrentState(scope) != RelationTypes.Pending){
            Console.WriteLine(string.Join("\n", GetErrors()));
            return;
        }
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
        cmd.Parameters.Add(new NpgsqlParameter<int>("p5", (int)_levelOut));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p6", (int)_levelIn));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p7", (int)_educationDepth));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p8", _fgosPrefix));
        cmd.Parameters.Add(new NpgsqlParameter<string?>("p9", _qualificationPostfix));

        await using (cmd){
            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            _id = (int)reader["id"];
            NotifyStateChanged();
        }
        if (conn!=null){
            await conn.DisposeAsync();
        }   
    }

    public static async Task<SpecialityModel?> GetById(int id, ObservableTransaction? scope)
    {
        SpecialityModel? result = null;
        NpgsqlConnection? conn = scope == null ? await Utils.GetAndOpenConnectionFactory() : null;
        string cmdText = "SELECT * FROM educational_program WHERE id = @p1";
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

        await using (cmd)
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows)
            {
                return result;
            }
            else
            {
                await reader.ReadAsync();
                result = new SpecialityModel(id,
                    (string)reader["fgos_code"],
                    (string)reader["fgos_name"],
                    (string)reader["qualification"],
                    reader["group_prefix"].GetType() == typeof(DBNull) ? "" : (string)reader["group_prefix"])
                {
                    _qualificationPostfix = reader["group_postfix"].GetType() == typeof(DBNull) ? null : (string)reader["group_postfix"],
                    _courseCount = (int)reader["course_count"],
                    _educationDepth = (TeachingDepth.Levels)(int)reader["knowledge_depth"],
                    _levelIn = (StudentEducationalLevelRecord.EducationalLevels)(int)reader["speciality_in_education_level"],
                    _levelOut = (StudentEducationalLevelRecord.EducationalLevels)(int)reader["speciality_out_education_level"],
                };
            }
        }
        if (conn != null)
        {
            await conn.DisposeAsync();
        }
        return result;
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

    public override async Task<IDbObjectValidated?> GetDbRepresentation(ObservableTransaction? scope)
    {
        return await GetById(this._id, scope);
    }
    public override bool Equals(IDbObjectValidated? other)
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
            _educationDepth == unboxed._educationDepth &&
            _fgosCode == unboxed._fgosCode &&
            _fgosName == unboxed._fgosName &&
            _levelIn == unboxed._levelIn &&
            _levelOut == unboxed._levelOut &&
            _qualification == unboxed._qualification &&
            _qualificationPostfix == unboxed._qualificationPostfix;
    }
}



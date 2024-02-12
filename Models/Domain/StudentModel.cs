using Npgsql;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Controllers.DTO.Out;
using StudentTracking.Models.Domain.Address;
using StudentTracking.Models.Domain.Flow;
using StudentTracking.Models.Domain.Misc;
using StudentTracking.Models.SQL;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Utilities;
using Utilities.Validation;

namespace StudentTracking.Models.Domain;

public class StudentModel
{

    private int _id;
    private int? _actualAddress;
    private string _snils;
    private string _inn;
    private DateTime _dateOfBirth;
    private decimal _admissionScore;
    private int? _russianCitizenshipId;
    private string _gradeBookNumber;
    private Genders.GenderCodes _gender;
    private TargetEduAgreement _targetAgreementType;
    private PaidEduAgreement _paidAgreementType;
    private int? _giaMark;
    private int? _giaDemoExamMark;
    public int Id
    {
        get => _id;
    }
    public string Snils
    {
        get => _snils;
    }
    public string Inn
    {
        get => _inn;
    }
    public DateTime DateOfBirth
    {
        get => _dateOfBirth;
    }
    public decimal AdmissionScore  {
        get => Math.Round(_admissionScore, 2);
    }
    public string GradeBookNumber  {
        get => _gradeBookNumber;
    }
    public Genders.GenderCodes Gender {
        get => _gender;
    }

    public TargetEduAgreement TargetAgreementType {
        get => _targetAgreementType;
    }
    public int? GiaMark {
        get => _giaMark;
    }
    public int? GiaDemoExamMark {
        get => _giaDemoExamMark;
    }
    public PaidEduAgreement PaidAgreementType {
        get => _paidAgreementType;
    }

    public int? RussianCitizenshipId  {
        get => _russianCitizenshipId;
        set => _russianCitizenshipId = value;
    }
    public int? ActualAddressId
    {
        get => _actualAddress;
        set => _actualAddress = value; 
    }

    private StudentModel()
    {

    }
    public static Result<StudentModel?> Build(StudentDTO? dto){
        if (dto is null){
            return Result<StudentModel>.Failure(new ValidationError(nameof(dto), "Пустая модель студента"));
        }
        var model = new StudentModel();
        var errors = new List<ValidationError?>();
        if (dto.Id != null){
            model._id = (int)dto.Id;    
        }
        // инн и снилс должны быть уникальны в базе
        if (errors.IsValidRule(
            dto.Snils.CheckStringPatternD(ValidatorCollection.Snils),
            message: "Неверный формат СНИЛС",
            propName: nameof(Snils)))
        {
            model._snils = dto.Snils;
        }
        if (errors.IsValidRule(
            dto.Inn.CheckStringPatternD(ValidatorCollection.OnlyDigits) ||
            dto.Inn.Length == 12,
            message: "Неверный формат ИНН",
            propName: nameof(Inn)
        )){
            model._inn = dto.Inn;
        }
        if (errors.IsValidRule(
            Utils.TryParseDate(dto.DateOfBirth),
            message: "Недопустимая дата рождения",
            propName: nameof(DateOfBirth)
        )){
            model._dateOfBirth = Utils.ParseDate(dto.DateOfBirth);
        }
        if (errors.IsValidRule(
            dto.AdmissionScore.CheckStringPatternD(ValidatorCollection.DecimalFormat) ||
            dto.AdmissionScore.CheckStringPatternD(ValidatorCollection.OnlyDigits),
            message: "Неверный формат вступительного балла",
            propName: nameof(AdmissionScore)
        )){
            var admScore = Math.Round(decimal.Parse(dto.AdmissionScore),2);
            if (errors.IsValidRule(
                admScore >= 3 && admScore <= 5,
                message: "Неверное значение вступительного балла",
                propName: nameof(AdmissionScore)
            )){
                model._admissionScore = admScore;
            }
        }
        if (errors.IsValidRule(
            dto.GradeBookNumber != null &&
            dto.GradeBookNumber.CheckStringPatternD(ValidatorCollection.OnlyDigits) &&
            dto.GradeBookNumber.Length >= 1 && dto.GradeBookNumber.Length <= 6,
            message: "Неверный номер зачетки",
            propName: nameof(GradeBookNumber)
        )){
            model._gradeBookNumber = dto.GradeBookNumber;
        }
        if (errors.IsValidRule(
            Enum.TryParse(typeof(Genders.GenderCodes), dto.Gender.ToString(), out object? t),
            message: "Неверный пол",
            propName: nameof(Gender)
        )){
            model._gender = (Genders.GenderCodes)dto.Gender;
        }
        if (errors.IsValidRule(
            TargetEduAgreement.TryGetByTypeCode(dto.TargetAgreementType),
            message: "Неверно указан тип договора о целевом обучении",
            propName: nameof(TargetAgreementType)
        )){
            model._targetAgreementType = TargetEduAgreement.GetByTypeCode(dto.TargetAgreementType);
        }
        if(errors.IsValidRule(
            dto.GiaMark == null || 
            (int.TryParse(dto.GiaMark, out int mark) && mark >= 3 && mark <= 5),
            message: "Неверно указана оценка ГИА",
            propName: nameof(GiaMark) 
        )){
            model._giaMark = dto.GiaMark is null ? null : int.Parse(dto.GiaMark);
        }
        if(errors.IsValidRule(
            dto.GiaDemoExamMark == null || 
            (int.TryParse(dto.GiaDemoExamMark, out int dmark) && dmark >= 3 && dmark <= 5),
            message: "Неверно указана оценка демонстрационного экзамена",
            propName: nameof(GiaDemoExamMark) 
        )){
            model._giaDemoExamMark = dto.GiaDemoExamMark is null ? null : int.Parse(dto.GiaDemoExamMark);
        }
        if (errors.IsValidRule(
            PaidEduAgreement.TryGetByTypeCode(dto.PaidAgreementType),
            message: "Неверно указан тип договора о платном обучении",
            propName: nameof(PaidAgreementType)
        )){
            model._paidAgreementType = PaidEduAgreement.GetByTypeCode(dto.PaidAgreementType);
        }
        if (errors.Any()){
            return Result<StudentModel>.Failure(errors);
        }
        else {
            return Result<StudentModel>.Success(model);
        }
    }

    // по умолчанию возвращает только уникальных студентов
    public static async Task<IReadOnlyCollection<StudentModel>> FindUniqueStudents(QueryLimits limits, JoinSection? additionalJoins = null, ComplexWhereCondition? additionalConditions = null, SQLParameterCollection? addtitionalParameters = null){
        using var conn = await Utils.GetAndOpenConnectionFactory();
        var mapper = new Mapper<StudentModel>(
            (reader) => {
                var id = reader["idstd"];
                if (id.GetType() == typeof(DBNull)){
                    return Task.Run(() => QueryResult<StudentModel>.NotFound());
                }
                var result = new StudentModel() {
                    _id = (int)id,
                    _snils = (string)reader["snils"],
                    _inn = (string)reader["inn"],
                    _actualAddress = (int)reader["actual_address"],
                    _dateOfBirth = (DateTime)reader["date_of_birth"],
                    _gender = (Genders.GenderCodes)reader["gender"],
                    _gradeBookNumber = (string)reader["grade_book_number"],
                    _targetAgreementType = TargetEduAgreement.GetByTypeCode((int)reader["target_education_agreement"]),
                    _paidAgreementType = PaidEduAgreement.GetByTypeCode((int)reader["paid_education_agreement"]),
                    _admissionScore = (decimal)reader["admission_score"],
                    _russianCitizenshipId = reader["rus_citizenship_id"].GetType() == typeof(DBNull) ? null : (int)reader["rus_citizenship_id"],
                    _giaDemoExamMark = reader["gia_demo_exam_mark"].GetType() == typeof(DBNull) ? null : (int)reader["gia_demo_exam_mark"],
                    _giaMark = reader["gia_mark"].GetType() == typeof(DBNull) ? null : (int)reader["gia_mark"],
                };
                return Task.Run(() => QueryResult<StudentModel>.Found(result));
            },
            new List<Column>(){
                new Column("id", "idstd", "students"),
                new Column("snils", "students"),
                new Column("gender", "students"),
                new Column("inn", "students"),
                new Column("actual_address", "students"),
                new Column("date_of_birth", "students"),
                new Column("grade_book_number", "students"),
                new Column("target_education_agreement", "students"),
                new Column("paid_education_agreement", "students"),
                new Column("admission_score", "students"),
                new Column("rus_citizenship_id", "students"),
                new Column("gia_demo_exam_mark", "students"),
                new Column("gia_mark", "students"),
            }
        );
        var buildResult = SelectQuery<StudentModel>.Init("students", new Column("id", "students"))
        .AddMapper(mapper)
        .AddJoins(additionalJoins)
        .AddWhereStatement(additionalConditions)
        .AddParameters(addtitionalParameters)
        .Finish();
        if (buildResult.IsFailure){
            throw new Exception("Создание запроса по студентам не должно проваливаться");
        }
        return await buildResult.ResultObject.Execute(conn, limits);
    }



    public static async Task<StudentModel?> GetStudentById(int id)
    {
        using var conn = await Utils.GetAndOpenConnectionFactory();
        
        var sParams = new SQLParameterCollection();
        var p1 = sParams.Add<int>(id);
        var where = new ComplexWhereCondition(
            new WhereCondition(
                new Column("id", "students"),
                p1,
                WhereCondition.Relations.Equal
            )
        );
        var found = await FindUniqueStudents(new QueryLimits(0,1), additionalConditions: where, addtitionalParameters: sParams);
        if (found.Any()){
            return found.First();
        }
        else {
            return null;
        }
    }

    public static async Task LinkStudentAndCitizenship(Type citizenshipType, int studentId, int citizenshipId){
        
        await using var connection = await Utils.GetAndOpenConnectionFactory(); 
        
        if (!await IsIdExists(studentId, null) || !await RussianCitizenship.IsIdExists(citizenshipId, null)){
            return;
        }

        if (citizenshipType == typeof(RussianCitizenship)){
            string cmdText = "UPDATE students SET rus_citizenship_id = @p1 WHERE id = @p2";
            NpgsqlCommand cmd = new NpgsqlCommand(cmdText, connection);
            cmd.Parameters.Add(new NpgsqlParameter<int>("p1", citizenshipId));
            cmd.Parameters.Add(new NpgsqlParameter<int>("p2", studentId));
            using (cmd){
                var nonQuery = await cmd.ExecuteNonQueryAsync();  
            }
        }
    }

    public async Task Save(ObservableTransaction? scope) 
    {
        await using NpgsqlConnection connection = await Utils.GetAndOpenConnectionFactory();

        var cmdText = "INSERT INTO students( " +
            "snils, inn, actual_address, date_of_birth, rus_citizenship_id, " +
            "gender, grade_book_number, target_education_agreement, gia_mark, gia_demo_exam_mark, paid_education_agreement, admission_score) " +
            "VALUES (@p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12) RETURNING id";
        
        NpgsqlCommand cmd;
        if (scope!= null){
            cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }
        else{
            cmd = new NpgsqlCommand(cmdText, connection);
        }
        

        cmd.Parameters.Add(new NpgsqlParameter<string>("p1", _snils));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p2", _inn));
        cmd.Parameters.Add(new NpgsqlParameter("p3", _actualAddress == null ? DBNull.Value : (int)_actualAddress));
        cmd.Parameters.Add(new NpgsqlParameter<DateTime>("p4", _dateOfBirth));
        cmd.Parameters.Add(new NpgsqlParameter("p5", _russianCitizenshipId == null? DBNull.Value : (int)_russianCitizenshipId));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p6", (int)_gender));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p7", _gradeBookNumber));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p8", (int)_targetAgreementType.AgreementType));
        cmd.Parameters.Add(new NpgsqlParameter("p9", _giaMark == null ? DBNull.Value : (int)_giaMark));
        cmd.Parameters.Add(new NpgsqlParameter("p10", _giaDemoExamMark == null ? DBNull.Value : (int)_giaDemoExamMark));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p11", (int)_paidAgreementType.AgreementType));
        cmd.Parameters.Add(new NpgsqlParameter<decimal>("p12", _admissionScore));

        await using (cmd){
            using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();;
            _id = (int)reader["id"];
        }
    }
    public static async Task<bool> IsIdExists(int id, ObservableTransaction? scope){

        await using var connection = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "SELECT EXISTS(SELECT id FROM students WHERE id = @p1)";
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
    public static async Task<bool> IsAllExists(IEnumerable<int> ids){

        using var conn = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "SELECT COUNT(id) AS c FROM students WHERE " + 
        "id = ANY(@p1)";
        NpgsqlCommand cmd = new NpgsqlCommand(cmdText, conn);
        var p = new NpgsqlParameter();
        p.ParameterName = "p1";
        p.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Integer;
        p.Value = ids.ToArray();
        using (cmd){
            using var reader = cmd.ExecuteReader();
            return (int)reader["c"] == ids.Count();
        }
    }  


    public override bool Equals(object? other)
    {
        if (other == null){
            return false;
        }
        if (other.GetType() != this.GetType()){
            return false;
        }
        var p = (StudentModel)other;
        return 
            _id == p._id &&
            _actualAddress == p._actualAddress &&
            _dateOfBirth == p._dateOfBirth &&
            ((_giaMark == null && p._giaMark == null) || _giaMark == p._giaMark) &&
            ((_giaDemoExamMark == null && p._giaDemoExamMark == null) || _giaDemoExamMark == p._giaDemoExamMark) &&
            _gender == p._gender &&
            _paidAgreementType == p._paidAgreementType &&
            _targetAgreementType == p._targetAgreementType &&
            _snils == p._snils &&
            _inn == p._inn &&
            _gradeBookNumber == p._gradeBookNumber;
    }

    public async Task<string> GetName(){
        
        if (_russianCitizenshipId != null){
            var rCitizenship = await RussianCitizenship.GetById((int)_russianCitizenshipId, null);
            if (rCitizenship != null){
                return rCitizenship.GetName();
            } 
        }
        return "Не указано";
    }

    public async Task<GroupModel?> GetCurrentGroup(){
        return await StudentHistory.GetCurrentStudentGroup(_id);
    }

    public async Task Update(ObservableTransaction? scope = null)
    {
        using var conn = await Utils.GetAndOpenConnectionFactory(); 
        var cmdText = "UPDATE public.students " +
	    " SET snils=@p1, inn=@p2, actual_address=@p3, date_of_birth=@p4, rus_citizenship_id=@p5, " +
        " gender=@p6, grade_book_number=@p7, target_education_agreement=@p8, gia_mark=@p9, " +
        " gia_demo_exam_mark=@p10, paid_education_agreement=@p11, admission_score=@p12 " +
	    " WHERE id = @p13";
        NpgsqlCommand cmd;
        if (scope is null){
            cmd = new NpgsqlCommand(cmdText, conn);
        }
        else{
            cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }
        cmd.Parameters.Add(new NpgsqlParameter<string>("p1", _snils));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p2", _inn));
        cmd.Parameters.Add(new NpgsqlParameter("p3", _actualAddress is null ? DBNull.Value : (int)_actualAddress));
        cmd.Parameters.Add(new NpgsqlParameter<DateTime>("p4", _dateOfBirth));
        cmd.Parameters.Add(new NpgsqlParameter("p5", _russianCitizenshipId is null ? DBNull.Value : (int)_russianCitizenshipId));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p6", (int)_gender));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p7", _gradeBookNumber));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p8", (int)_targetAgreementType.AgreementType));
        cmd.Parameters.Add(new NpgsqlParameter("p9", _giaMark is null ? DBNull.Value : (int)_giaMark));
        cmd.Parameters.Add(new NpgsqlParameter("p10", _giaDemoExamMark is null ? DBNull.Value : (int)_giaDemoExamMark));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p11", (int)_paidAgreementType.AgreementType));
        cmd.Parameters.Add(new NpgsqlParameter<decimal>("p12", _admissionScore));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p13", _id));
        using (cmd){
            await cmd.ExecuteNonQueryAsync();
        }
    }
}



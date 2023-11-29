using Npgsql;
using StudentTracking.Models.Domain.Address;
using StudentTracking.Models.Domain.Misc;
using System.Data;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Utilities;
using Utilities.Validation;

namespace StudentTracking.Models.Domain;

public class StudentModel : DbValidatedObject
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
    private TargetEduAgreement.Types _targetAgreementType;
    private PaidEduAgreement.Types _paidAgreementType;
    private int? _giaMark;
    private int? _giaDemoExamMark;
    public int Id
    {
        get => _id;
    }
    public string Snils
    {
        get => _snils;
        set
        {
            if (PerformValidation(
            
            () => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.Snils)
            , new ValidationError(nameof(Snils), "Неверный формат СНИЛС")))
            {
                _snils = value;
            }
        }
    }
    public int? ActualAddressId
    {
        get => _actualAddress;
        set
        {
            if (PerformValidation(
                () => AddressModel.IsIdExists(value, null).Result, new ValidationError(nameof(ActualAddressId), "Неверно заданный адрес"))
            ){
                _actualAddress = value;
            }
        }
    }

    public string Inn
    {
        get => _inn;
        set
        {
            if (PerformValidation(
            () =>
            ValidatorCollection.CheckStringLength(value, 12, 12) ||
            ValidatorCollection.CheckStringPattern(value, ValidatorCollection.OnlyDigits)
            , new ValidationError(nameof(Inn), "Неверный формат ИНН")))
            {
                _inn = value;
            }
        }
    }
    public string DateOfBirth
    {
        get => Utils.FormatDateTime(_dateOfBirth);
        set
        {
            if (PerformValidation(
            () => Utils.TryParseDate(value), new ValidationError(nameof(DateOfBirth), "Неверный формат даты либо дата некорректна")))
            {
                _dateOfBirth = Utils.ParseDate(value);
            }
        }
    }
    public string AdmissionScore  {
        get => Math.Round(_admissionScore, 2).ToString();
        set
        {
            if (PerformValidation(
            () => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.DecimalFormat)
            || ValidatorCollection.CheckStringPattern(value, ValidatorCollection.OnlyDigits),
            new ValidationError(nameof(AdmissionScore), "Неверный формат среднего балла")))
            {
                decimal got = Math.Round(decimal.Parse(value),2);
                if (PerformValidation(() => ValidatorCollection.CheckRange(got, 3m, 5m),
                new ValidationError(nameof(AdmissionScore), "Неверное значение среднего балла"))){
                    _admissionScore = got;
                }
                
            }
        }
    }
    public int? RussianCitizenshipId  {
        get => _russianCitizenshipId;
        set
        {   
            if (PerformValidation(
                () => {
                    if (value == null){
                        return true;
                    }
                    else{
                        return RussianCitizenship.IsIdExists((int)value).Result;
                    }
                }, new DbIntegrityValidationError(nameof(RussianCitizenshipId), "Указанное гражданство не зарегистрировано")
            )){
                _russianCitizenshipId = value;
            }      
        }
    }
    public string GradeBookNumber  {
        get => _gradeBookNumber;
        set
        {
            if (PerformValidation(
            () => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.OnlyDigits) &&
                    ValidatorCollection.CheckStringLength(value, 1, 6),
            new ValidationError(nameof(GradeBookNumber), "Неверный формат номера в поименной книге")))
            {
                _gradeBookNumber = value;
            }
        }
    }
    public int Gender {
        get => (int)_gender;
        set
        {
            if (PerformValidation(
            () => Enum.TryParse(typeof(Genders.GenderCodes), value.ToString(), out object? t),
            new ValidationError(nameof(Gender), "Неверный пол")))
            {
                _gender = (Genders.GenderCodes)value;
            }
        }
    }

    public int TargetAgreementType {
        get => (int)_targetAgreementType;
        set
        {
            if (PerformValidation(
            () => Enum.TryParse(typeof(TargetEduAgreement.Types), value.ToString(), out object? t),
            new ValidationError(nameof(TargetAgreementType), "Неверный тип договора о целевом обучении")))
            {
                _targetAgreementType = (TargetEduAgreement.Types)value;
            }
        }
    }
    public string GiaMark {
        get => _giaMark == null ? "" : ((int)_giaMark).ToString();
        set
        {
            if (PerformValidation(
                () => int.TryParse(value, out int mark),
                new ValidationError(nameof(GiaMark), "Неверный формат оценки ГИА")))
            {
                if (PerformValidation (
                    () => ValidatorCollection.CheckRange(int.Parse(value),3,5),
                    new ValidationError(nameof(GiaMark), "Неверное значение оценки ГИА"))
                ){
                    _giaMark = int.Parse(value);
                }
            } else {
                if (PerformValidation (
                    () => value == string.Empty,
                    new ValidationError(nameof(GiaMark), "Недопустимое значение оценки"))
                ){
                    _giaMark = null;
                }
            }
                
            
        }
    }
    public string GiaDemoExamMark {
        get => _giaDemoExamMark == null ? "" : ((int)_giaDemoExamMark).ToString();
        set
        {
            if (PerformValidation(
                () => int.TryParse(value, out int mark),
                new ValidationError(nameof(GiaDemoExamMark), "Неверный формат оценки ГИА")))
            {
                if (PerformValidation (
                    () => ValidatorCollection.CheckRange(int.Parse(value),3,5),
                    new ValidationError(nameof(GiaDemoExamMark), "Неверное значение оценки ГИА"))
                ){
                    _giaDemoExamMark = int.Parse(value);
                }
            } else {
                if (PerformValidation (
                    () => value == string.Empty,
                    new ValidationError(nameof(GiaDemoExamMark), "Недопустимое значение оценки"))
                ){
                    _giaDemoExamMark = null;
                }
            }
        }
    }
    public int PaidAgreementType {
        get => (int)_paidAgreementType;
        set
        {
            if (PerformValidation(
            () => Enum.TryParse(typeof(PaidEduAgreement.Types), value.ToString(), out object? t),
            new ValidationError(nameof(PaidAgreementType), "Неверный тип договора о целевом обучении")))
            {
                _paidAgreementType = (PaidEduAgreement.Types)value;
            }
        }
    }

    public StudentModel() : base()
    {
        RegisterProperty(nameof(GradeBookNumber));
        RegisterProperty(nameof(DateOfBirth));
        RegisterProperty(nameof(Gender));
        RegisterProperty(nameof(Snils));
        RegisterProperty(nameof(Inn));
        RegisterProperty(nameof(PaidAgreementType));
        RegisterProperty(nameof(TargetAgreementType));
        RegisterProperty(nameof(AdmissionScore));
        RegisterProperty(nameof(GiaMark));
        RegisterProperty(nameof(GiaDemoExamMark));
        RegisterProperty(nameof(ActualAddressId));
        RegisterProperty(nameof(RussianCitizenshipId));

        _id = -1;
        _actualAddress = Utils.INVALID_ID;
        _dateOfBirth = DateTime.Today;
        _giaMark = null;
        _giaDemoExamMark = null;
        _gender = Genders.GenderCodes.Undefinned;
        _paidAgreementType = PaidEduAgreement.Types.NotMentioned;
        _targetAgreementType = TargetEduAgreement.Types.NotMentioned;
        _snils = "";
        _inn = "";
        _gradeBookNumber = "";
    }

    public static async Task<StudentModel?> GetStudentById(int id)
    {
        await using (var conn = await Utils.GetAndOpenConnectionFactory())
        {
            NpgsqlCommand getCommand = new NpgsqlCommand("SELECT * FROM students WHERE id = @p1", conn);
            getCommand.Parameters.Add(new NpgsqlParameter<int>("p1", id));
            await using (getCommand)
            {
                using var pgreader = await getCommand.ExecuteReaderAsync();
                if (!pgreader.HasRows)
                {
                    return null;
                }

                var toReturn = new List<StudentModel>();
                await pgreader.ReadAsync();
                
                var built = new StudentModel
                {
                    _snils = (string)pgreader["snils"],
                    _inn = (string)pgreader["inn"],
                    _actualAddress = (int)pgreader["actual_address"],
                    _dateOfBirth = (DateTime)pgreader["date_of_birth"],
                    _gender = (Genders.GenderCodes)pgreader["gender"],
                    _gradeBookNumber = (string)pgreader["grade_book_number"],
                    _targetAgreementType = (TargetEduAgreement.Types)pgreader["target_education_agreement"],
                    _paidAgreementType = (PaidEduAgreement.Types)pgreader["paid_education_agreement"]
                };

                object rus_citizenship = pgreader["rus_citizenship_id"];
                if (rus_citizenship.GetType() == typeof(System.DBNull))
                {
                    built._russianCitizenshipId = null;
                }
                else
                {
                    built._russianCitizenshipId = (int)rus_citizenship;
                }
                object gia_mark_result = pgreader["gia_mark"];
                if (gia_mark_result.GetType() == typeof(DBNull))
                {
                    built._giaMark = null;
                }
                else
                {
                    built._giaMark = (int)gia_mark_result;
                }
                object gia_mark_dem_result = pgreader["gia_demo_exam_mark"];
                if (gia_mark_dem_result.GetType() == typeof(DBNull))
                {
                    built._giaDemoExamMark = null;
                }
                else
                {
                    built._giaDemoExamMark = (int)gia_mark_dem_result;
                }
                return built;
            }
        }
    }

    public static async Task LinkStudentAndCitizenship(Type citizenshipType, int studentId, int citizenshipId){
        
        await using var connection = await Utils.GetAndOpenConnectionFactory(); 
        
        if (!await IsIdExists(studentId, null) || !await RussianCitizenship.IsIdExists(citizenshipId)){
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

    public async Task Save() 
    {
        await using NpgsqlConnection connection = await Utils.GetAndOpenConnectionFactory();

        if (await GetCurrentState(null) != RelationTypes.Pending){
            return;
        }

        var cmdText = "INSERT INTO students( " +
            "snils, inn, actual_address, date_of_birth, rus_citizenship_id, " +
            "gender, grade_book_number, target_education_agreement, gia_mark, gia_demo_exam_mark, paid_education_agreement, admission_score) " +
            "VALUES (@p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12) RETURNING id";
        var insertCommand = new NpgsqlCommand(cmdText, connection);

        insertCommand.Parameters.Add(new NpgsqlParameter<string>("p1", _snils));
        insertCommand.Parameters.Add(new NpgsqlParameter<string>("p2", _inn));
        insertCommand.Parameters.Add(new NpgsqlParameter("p3", _actualAddress == null ? DBNull.Value : (int)_actualAddress));
        insertCommand.Parameters.Add(new NpgsqlParameter<DateTime>("p4", _dateOfBirth));
        insertCommand.Parameters.Add(new NpgsqlParameter("p5", _russianCitizenshipId == null? DBNull.Value : (int)_russianCitizenshipId));
        insertCommand.Parameters.Add(new NpgsqlParameter<int>("p6", (int)_gender));
        insertCommand.Parameters.Add(new NpgsqlParameter<string>("p7", _gradeBookNumber));
        insertCommand.Parameters.Add(new NpgsqlParameter<int>("p8", (int)_targetAgreementType));
        insertCommand.Parameters.Add(new NpgsqlParameter("p9", _giaMark == null ? DBNull.Value : (int)_giaMark));
        insertCommand.Parameters.Add(new NpgsqlParameter("p10", _giaDemoExamMark == null ? DBNull.Value : (int)_giaDemoExamMark));
        insertCommand.Parameters.Add(new NpgsqlParameter<int>("p11", (int)_paidAgreementType));
        insertCommand.Parameters.Add(new NpgsqlParameter<decimal>("p12", _admissionScore));

        await using (insertCommand){
            using var reader = await insertCommand.ExecuteReaderAsync();
            await reader.ReadAsync();
            NotifyStateChanged();
            _id = (int)reader["id"];
        }
    }

    public override async Task<IDbObjectValidated?> GetDbRepresentation(ObservableTransaction? stateWithin)
    {
        var rawCheck = await GetStudentById(_id);
        if (rawCheck == null){

            var conn = await Utils.GetAndOpenConnectionFactory();
            string cmdText = "SELECT id FROM students WHERE inn = @p1";
            NpgsqlCommand cmd = new NpgsqlCommand (cmdText, conn);
            cmd.Parameters.Add(new NpgsqlParameter<string>("p1", _inn));

            using (conn)
            using (cmd){
                await using var reader = cmd.ExecuteReader();
                await reader.ReadAsync();
                if (!reader.HasRows){
                    return null;
                }
                _id = (int)reader["id"];
                return this;
            }
        }
        return rawCheck;
    }
    public static async Task<bool> IsIdExists(int id, ObservableTransaction? scope){

        await using var connection = await Utils.GetAndOpenConnectionFactory();{
            string cmdText = "SELECT EXISTS(SELECT id FROM students WHERE id = @p1)";
            NpgsqlCommand cmd = new NpgsqlCommand(cmdText, connection);
            cmd.Parameters.Add(new NpgsqlParameter<int>("p1", id));
            await using (connection)
            await using (cmd){
                using var reader = await cmd.ExecuteReaderAsync();
                await reader.ReadAsync();
                return (bool)reader["exists"];
            }
        }
    } 

    public override bool Equals(IDbObjectValidated? other)
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
    /*
    private void Update(){
        using (var conn = Utils.GetConnectionFactory()){
        using var updateCommand = new NpgsqlCommand(
                    "UPDATE students " +
                    "SET snils=@p1, inn=@p2, actual_address=@p3, date_of_birth=@p4, rus_citizenship_id=@p5, gender=@p6, grade_book_number=@p7, " +
                    "target_education_agreement=@p8, gia_mark=@p9, gia_demo_exam_mark=@p10, paid_education_agreement=@p11, admission_score=@p12 " +
                    "WHERE id = @p13", conn)
                {
                    Parameters = {
                             new ("p1", _snils),
                            new ("p2", _inn),
                            new ("p3", _actualAddress),
                            new ("p4", _dateOfBirth),
                            new ("p5", _russianCitizenshipId == null ? DBNull.Value : (int)RussianCitizenshipId),
                            new ("p6", (int)_gender),
                            new ("p7", _gradeBookNumber),
                            new ("p8", (int)_targetAgreementType),
                            new ("p9", _giaMark == null ? DBNull.Value : (int)_giaMark),
                            new ("p10", _giaDemoExamMark == null ? DBNull.Value : (int)_giaDemoExamMark),
                            new ("p11", (int)_paidAgreementType),
                            new ("p12", _admissionScore),
                            new ("p13", _id),
                        }
                };
                updateCommand.ExecuteNonQuery(); 
        }
    } */
}



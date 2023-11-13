using Npgsql;
using StudentTracking.Models.Domain.Address;
using StudentTracking.Models.Domain.Misc;
using System.Data;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Utilities;

namespace StudentTracking.Models.Domain;

public class StudentModel : ValidatedObject<StudentModel>
{

    private int _id;
    private AddressModel? _actualAddress;
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

    [JsonRequired]
    public int Id
    {
        get => _id;
        set
        {
            _id = value;
        }
    }
    public string Snils
    {
        get => _snils;
        set
        {
            if (PerformValidation(
            
            () => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.Snils)
            , new ValidationError<StudentModel>(nameof(Snils), "Неверный формат СНИЛС")))
            {
                _snils = value;
            }
        }
    }
    public int ActualAddressId
    {
        get => _actualAddress == null ? Utils.INVALID_ID : _actualAddress.Id;
        set
        {
            var address = AddressModel.GetAddressById(value);
            if (PerformValidation(
                () => address != null, new ValidationError<StudentModel>(nameof(ActualAddressId), "Неверно заданный адрес"))
            ){
                _actualAddress = address;
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
            {
                return ValidatorCollection.CheckStringLength(value, 12, 12);
            }, new ValidationError<StudentModel>(nameof(Inn), "Неверный формат ИНН")))
            {
                _snils = value;
            }
        }
    }
    public string DateOfBirth
    {
        get => Utils.FormatDateTime(_dateOfBirth);
        set
        {
            if (PerformValidation(
            () => Utils.TryParseDate(value), new ValidationError<StudentModel>(nameof(DateOfBirth), "Неверный формат даты либо дата некорректна")))
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
            ,
            new ValidationError<StudentModel>(nameof(AdmissionScore), "Неверный формат среднего балла")))
            {
                decimal got = Math.Round(decimal.Parse(value),2);
                if (PerformValidation(() => ValidatorCollection.CheckRange(got, 3m, 5m),
                new ValidationError<StudentModel>(nameof(AdmissionScore), "Неверное значение среднего балла"))){
                    _admissionScore = got;
                }
                
            }
        }
    }
    public int RussianCitizenshipId  {
        get => _russianCitizenshipId == null ? Utils.INVALID_ID : (int)_russianCitizenshipId;
        set
        {
            _russianCitizenshipId = value;
        }
    }
    public string GradeBookNumber  {
        get => _gradeBookNumber;
        set
        {
            if (PerformValidation(
            () => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.OnlyDigits) &&
                    ValidatorCollection.CheckStringLength(value, 1, 6),
            new ValidationError<StudentModel>(nameof(GradeBookNumber), "Неверный формат номера в поименной книге")))
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
            new ValidationError<StudentModel>(nameof(GradeBookNumber), "Неверный пол")))
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
            new ValidationError<StudentModel>(nameof(TargetAgreementType), "Неверный тип договора о целевом обучении")))
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
            () => int.TryParse(value, out int mark) || ValidatorCollection.CheckStringLength(value, 0 , 0),
            new ValidationError<StudentModel>(nameof(GiaMark), "Неверный формат оценки ГИА")))
            {
                if (value.Length == 0){
                    _giaMark = null;
                }
                else{
                    if (PerformValidation (
                        () => ValidatorCollection.CheckRange(int.Parse(value),3,5),
                        new ValidationError<StudentModel>(nameof(GiaMark), "Неверное значение оценки ГИА"))
                    ){
                        _giaMark = int.Parse(value);
                    }
                }
                
            }
        }
    }
    public string GiaDemoExamMark {
        get => _giaDemoExamMark == null ? "" : ((int)_giaDemoExamMark).ToString();
        set
        {
            if (PerformValidation(
            () => int.TryParse(value, out int mark) || ValidatorCollection.CheckStringLength(value, 0 , 0),
            new ValidationError<StudentModel>(nameof(GiaDemoExamMark), "Неверный формат оценки ГИА (демэкзамен)")))
            {
                if (value.Length == 0){
                    _giaDemoExamMark = null;
                }
                else{
                    if (PerformValidation (
                        () => ValidatorCollection.CheckRange(int.Parse(value),3,5),
                        new ValidationError<StudentModel>(nameof(GiaDemoExamMark), "Неверное значение оценки ГИА (демэкзамен)"))
                    ){
                        _giaDemoExamMark = int.Parse(value);
                    }
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
            new ValidationError<StudentModel>(nameof(PaidAgreementType), "Неверный тип договора о целевом обучении")))
            {
                _paidAgreementType = (PaidEduAgreement.Types)value;
            }
        }
    }
    [JsonIgnore]
    public AddressModel? AddressObject {
        get => _actualAddress;
    }

    public StudentModel()
    {
        _id = -1;
        _actualAddress = null;
        _dateOfBirth = DateTime.Today;
        _giaMark = null;
        _giaDemoExamMark = null;
        _gender = Genders.GenderCodes.Undefinned;
        _paidAgreementType = PaidEduAgreement.Types.NotMentioned;
        _targetAgreementType = TargetEduAgreement.Types.NotMentioned;
        _snils = "";
        _inn = "";
        _gradeBookNumber = "";
        _validationErrors = new List<ValidationError<StudentModel>>();
    }
    public static StudentModel? GetStudentById(int id)
    {
        var got = GetStudents(id);
        if (got == null)
        {
            return null;
        }
        else
        {
            return got.First();
        }
    }
    public static List<StudentModel>? GetAllStudents()
    {
        return GetStudents();
    }

    private static List<StudentModel>? GetStudents(int? id = null)
    {
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            NpgsqlCommand getCommand;
            if (id == null)
            {
                getCommand = new NpgsqlCommand("SELECT * FROM students", conn);
            }
            else
            {
                getCommand = new NpgsqlCommand("SELECT * FROM students WHERE id = @p1", conn)
                {
                    Parameters ={
                        new NpgsqlParameter("p1", id),
                    }
                };
            }
            using (getCommand)
            {
                var pgreader = getCommand.ExecuteReader();
                if (!pgreader.HasRows)
                {
                    return null;
                }

                var toReturn = new List<StudentModel>();
                while (pgreader.Read())
                {
                    var built = new StudentModel
                    {
                        _snils = (string)pgreader["snils"],
                        _inn = (string)pgreader["inn"],
                        _actualAddress = AddressModel.GetAddressById((int)pgreader["actual_address"]),
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

                    toReturn.Add(built);
                }
                return toReturn;
            }
        }
    }
    public static int SaveStudent(StudentModel student)
    {
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            if (student.Id == -1)
            {
                using var insertCommand = new NpgsqlCommand(
                    "INSERT INTO students( " +
                    "snils, inn, actual_address, date_of_birth, rus_citizenship_id, " +
                    "gender, grade_book_number, target_education_agreement, gia_mark, gia_demo_exam_mark, paid_education_agreement, admission_score) " +
                    "VALUES (@p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12) RETURNING id", conn)
                {
                    Parameters = {
                            new ("p1", student._snils),
                            new ("p2", student._inn),
                            new ("p3", student._actualAddress),
                            new ("p4", student._dateOfBirth),
                            new ("p5", student._russianCitizenshipId == null ? DBNull.Value : (int)student.RussianCitizenshipId),
                            new ("p6", (int)student._gender),
                            new ("p7", student._gradeBookNumber),
                            new ("p8", (int)student._targetAgreementType),
                            new ("p9", student._giaMark == null ? DBNull.Value : (int)student._giaMark),
                            new ("p10", student._giaDemoExamMark == null ? DBNull.Value : (int)student._giaDemoExamMark),
                            new ("p11", (int)student._paidAgreementType),
                            new ("p12", student._admissionScore),
                        }
                };
                var reader = insertCommand.ExecuteReader();
                reader.Read();
                return (int)reader["id"];

            }
            else
            {
                using var updateCommand = new NpgsqlCommand(
                    "UPDATE students " +
                    "SET snils=@p1, inn=@p2, actual_address=@p3, date_of_birth=@p4, rus_citizenship_id=@p5, gender=@p6, grade_book_number=@p7, " +
                    "target_education_agreement=@p8, gia_mark=@p9, gia_demo_exam_mark=@p10, paid_education_agreement=@p11, admission_score=@p12 " +
                    "WHERE id = @p13", conn)
                {
                    Parameters = {
                             new ("p1", student._snils),
                            new ("p2", student._inn),
                            new ("p3", student._actualAddress),
                            new ("p4", student._dateOfBirth),
                            new ("p5", student._russianCitizenshipId == null ? DBNull.Value : (int)student.RussianCitizenshipId),
                            new ("p6", (int)student._gender),
                            new ("p7", student._gradeBookNumber),
                            new ("p8", (int)student._targetAgreementType),
                            new ("p9", student._giaMark == null ? DBNull.Value : (int)student._giaMark),
                            new ("p10", student._giaDemoExamMark == null ? DBNull.Value : (int)student._giaDemoExamMark),
                            new ("p11", (int)student._paidAgreementType),
                            new ("p12", student._admissionScore),
                            new ("p13", student._id),
                        }
                };
                updateCommand.ExecuteNonQuery();
                return student.Id;
            }

        }
    }


}



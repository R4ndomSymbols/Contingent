using Npgsql;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Address;
using StudentTracking.Models.Domain.Flow;
using StudentTracking.Models.Domain.Misc;
using StudentTracking.SQL;
using Utilities;
using Utilities.Validation;

namespace StudentTracking.Models.Domain;

public class StudentModel
{
    private int? _id;
    private string _snils;
    private DateTime _dateOfBirth;
    private decimal _admissionScore;
    private string _gradeBookNumber;
    private Genders.GenderCodes _gender;
    private TargetEduAgreement _targetAgreementType;
    private PaidEduAgreement _paidAgreementType;
    private int? _giaMark;
    private int? _giaDemoExamMark;
    public bool IsFemale => _gender == Genders.GenderCodes.Female;
    public int? Id
    {
        get => _id;
    }
    public string Snils
    {
        get => _snils;
    }
    public DateTime DateOfBirth
    {
        get => _dateOfBirth;
    }
    public decimal AdmissionScore
    {
        get => Math.Round(_admissionScore, 2);
    }
    public string GradeBookNumber
    {
        get => _gradeBookNumber;
    }
    public Genders.GenderCodes Gender
    {
        get => _gender;
    }
    public TargetEduAgreement TargetAgreementType
    {
        get => _targetAgreementType;
    }
    public int? GiaMark
    {
        get => _giaMark;
    }
    public int? GiaDemoExamMark
    {
        get => _giaDemoExamMark;
    }
    public PaidEduAgreement PaidAgreement
    {
        get => _paidAgreementType;
    }

    // зависимости
    private int? _actualAddressId;
    private AddressModel? _actualAddress;
    private int? _russianCitizenshipId;
    private StudentHistory? _history;
    private RussianCitizenship? _russianCitizenship;

    public int? ActualAddressId
    {
        get => _actualAddress is null ? _actualAddressId : _actualAddress.Id;
    }
    public int? RussianCitizenshipId
    {
        get => _russianCitizenship is null ? _russianCitizenshipId : _russianCitizenship.Id;
    }

    public RussianCitizenship? RussianCitizenship
    {
        get => _russianCitizenship;
        set
        {
            if (value is null)
            {
                throw new Exception("Нельзя присвоить пустое гражданство");
            }
            _russianCitizenshipId = value.Id;
            _russianCitizenship = value;
        }
    }
    public AddressModel? ActualAddress
    {
        get
        {
            if (_actualAddressId != Utils.INVALID_ID && _actualAddressId is not null && _actualAddress is null)
            {
                _actualAddress = AddressModel.GetAddressById(_actualAddressId).Result;
                _actualAddressId = _actualAddress.Id;
            }
            return _actualAddress;
        }
        set
        {
            _actualAddress = value;
            _actualAddressId = value?.Id;

        }
    }
    public StudentHistory History
    {
        get
        {
            if (_history is null)
            {
                _history = StudentHistory.Create(this);
            }
            return _history;
        }
    }

    private StudentModel()
    {
        _id = null;
        _actualAddressId = null;
        _russianCitizenshipId = null;
    }

    public static Mapper<StudentModel> GetMapper((bool actual, bool legal) includeAddresses, Column? source, JoinSection.JoinType join = JoinSection.JoinType.InnerJoin)
    {
        var russianCitizenshipMapper = RussianCitizenship.GetMapper(new Column("rus_citizenship_id", "students"), includeAddresses.legal, JoinSection.JoinType.LeftJoin);
        var usedCols = new List<Column>(){
                new Column("id", "idstd", "students"),
                new Column("snils", "students"),
                new Column("gender", "students"),
                new Column("actual_address", "students"),
                new Column("date_of_birth", "students"),
                new Column("grade_book_number", "students"),
                new Column("target_education_agreement", "students"),
                new Column("paid_education_agreement", "students"),
                new Column("admission_score", "students"),
                new Column("rus_citizenship_id", "students"),
                new Column("gia_demo_exam_mark", "students"),
                new Column("gia_mark", "students"),
        };
        var studentMapper = new Mapper<StudentModel>(
            (reader) =>
            {
                var id = reader["idstd"];
                if (id.GetType() == typeof(DBNull))
                {
                    return QueryResult<StudentModel>.NotFound();
                }
                var result = new StudentModel()
                {
                    _id = (int)id,
                    _snils = (string)reader["snils"],
                    _dateOfBirth = (DateTime)reader["date_of_birth"],
                    _gender = (Genders.GenderCodes)reader["gender"],
                    _gradeBookNumber = (string)reader["grade_book_number"],
                    _targetAgreementType = TargetEduAgreement.GetByTypeCode((int)reader["target_education_agreement"]),
                    _paidAgreementType = PaidEduAgreement.GetByTypeCode((int)reader["paid_education_agreement"]),
                    _admissionScore = (decimal)reader["admission_score"],
                    _giaDemoExamMark = reader["gia_demo_exam_mark"].GetType() == typeof(DBNull) ? null : (int)reader["gia_demo_exam_mark"],
                    _giaMark = reader["gia_mark"].GetType() == typeof(DBNull) ? null : (int)reader["gia_mark"],
                };
                result._actualAddressId = reader["actual_address"].GetType() == typeof(DBNull) ? null : (int)reader["actual_address"];
                result._actualAddress = includeAddresses.actual && result._actualAddressId is not null ? AddressModel.GetAddressById((int)reader["actual_address"]).Result : null;
                var rusCitizenship = russianCitizenshipMapper.Map(reader);
                if (rusCitizenship.IsFound)
                {
                    result._russianCitizenship = rusCitizenship.ResultObject;
                }
                return QueryResult<StudentModel>.Found(result);
            },
            usedCols
        );
        if (source is not null)
        {
            studentMapper.PathTo.AddHead(join, source, new Column("id", "students"));
        }
        studentMapper.AssumeChild(russianCitizenshipMapper);
        return studentMapper;
    }
    public static Result<StudentModel> Build(StudentInDTO? dto, ObservableTransaction? scope = null)
    {
        if (dto is null)
        {
            return Result<StudentModel>.Failure(new ValidationError(nameof(dto), "Пустая модель студента"));
        }
        var model = new StudentModel();
        var errors = new List<ValidationError?>();
        StudentModel? modelInDatabase = null;
        if (dto.Id != null)
        {
            model._id = (int)dto.Id;
            modelInDatabase = GetStudentById(model._id).Result;
            errors.IsValidRule(
                modelInDatabase is not null,
                "Несуществующий id модели",
                nameof(Id)
            );
        }
        if (string.IsNullOrEmpty(dto.PhysicalAddress.Address))
        {
            model.ActualAddress = null;
        }
        else
        {
            var addressResult = AddressModel.Create(dto.PhysicalAddress, scope);
            if (addressResult.IsSuccess)
            {
                model.ActualAddress = addressResult.ResultObject;
            }
            else
            {
                errors.AddRange(addressResult.Errors);
            }
        }


        var citizenshipResult = RussianCitizenship.Create(dto.RusCitizenship, scope);
        if (citizenshipResult.IsSuccess)
        {
            model.RussianCitizenship = citizenshipResult.ResultObject;
        }
        else
        {
            errors.AddRange(citizenshipResult.Errors);
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
            Utils.TryParseDate(dto.DateOfBirth),
            message: "Недопустимая дата рождения",
            propName: nameof(DateOfBirth)
        ))
        {
            model._dateOfBirth = Utils.ParseDate(dto.DateOfBirth);
        }
        if (errors.IsValidRule(
            dto.AdmissionScore.CheckStringPatternD(ValidatorCollection.DecimalFormat) ||
            dto.AdmissionScore.CheckStringPatternD(ValidatorCollection.OnlyDigits),
            message: "Неверный формат вступительного балла",
            propName: nameof(AdmissionScore)
        ))
        {
            var admScore = Math.Round(decimal.Parse(dto.AdmissionScore), 2);
            if (errors.IsValidRule(
                admScore >= 3 && admScore <= 5,
                message: "Неверное значение вступительного балла",
                propName: nameof(AdmissionScore)
            ))
            {
                model._admissionScore = admScore;
            }
        }
        if (errors.IsValidRule(
            dto.GradeBookNumber != null &&
            dto.GradeBookNumber.CheckStringPatternD(ValidatorCollection.OnlyDigits) &&
            dto.GradeBookNumber.Length >= 1 && dto.GradeBookNumber.Length <= 6,
            message: "Неверный номер зачетки",
            propName: nameof(GradeBookNumber)
        ))
        {
            model._gradeBookNumber = dto.GradeBookNumber;
        }
        if (errors.IsValidRule(
            Enum.TryParse(typeof(Genders.GenderCodes), dto.Gender.ToString(), out object? t),
            message: "Неверный пол",
            propName: nameof(Gender)
        ))
        {
            model._gender = (Genders.GenderCodes)dto.Gender;
        }
        if (errors.IsValidRule(
            TargetEduAgreement.TryGetByTypeCode(dto.TargetAgreementType),
            message: "Неверно указан тип договора о целевом обучении",
            propName: nameof(TargetAgreementType)
        ))
        {
            model._targetAgreementType = TargetEduAgreement.GetByTypeCode(dto.TargetAgreementType);
        }
        if (errors.IsValidRule(
            dto.GiaMark == null ||
            (int.TryParse(dto.GiaMark, out int mark) && mark >= 3 && mark <= 5),
            message: "Неверно указана оценка ГИА",
            propName: nameof(GiaMark)
        ))
        {
            model._giaMark = dto.GiaMark is null ? null : int.Parse(dto.GiaMark);
        }
        if (errors.IsValidRule(
            dto.GiaDemoExamMark == null ||
            (int.TryParse(dto.GiaDemoExamMark, out int dmark) && dmark >= 3 && dmark <= 5),
            message: "Неверно указана оценка демонстрационного экзамена",
            propName: nameof(GiaDemoExamMark)
        ))
        {
            model._giaDemoExamMark = dto.GiaDemoExamMark is null ? null : int.Parse(dto.GiaDemoExamMark);
        }
        if (modelInDatabase is not null)
        {
            model._paidAgreementType = modelInDatabase._paidAgreementType;
        }
        else if (errors.IsValidRule(
            PaidEduAgreement.TryGetByTypeCode(dto.PaidAgreementType),
            message: "Неверно указан тип договора о платном обучении",
            propName: nameof(PaidAgreement)
        ))
        {
            model._paidAgreementType = PaidEduAgreement.GetByTypeCode(dto.PaidAgreementType);
        }
        if (errors.Any())
        {
            return Result<StudentModel>.Failure(errors);
        }
        else
        {
            return Result<StudentModel>.Success(model);
        }
    }

    // по умолчанию возвращает только уникальных студентов
    public static async Task<IReadOnlyCollection<StudentModel>> FindUniqueStudents(QueryLimits limits, JoinSection? additionalJoins = null, ComplexWhereCondition? additionalConditions = null, SQLParameterCollection? additionalParameters = null)
    {
        using var conn = await Utils.GetAndOpenConnectionFactory();
        var mapper = GetMapper((false, false), null);
        var buildResult = SelectQuery<StudentModel>.Init("students", new Column("id", "students"))
        .AddMapper(mapper)
        .AddJoins(mapper.PathTo.AppendJoin(additionalJoins))
        .AddWhereStatement(additionalConditions)
        .AddParameters(additionalParameters)
        .Finish();
        if (buildResult.IsFailure)
        {
            throw new Exception("Создание запроса по студентам не должно проваливаться");
        }
        return await buildResult.ResultObject.Execute(conn, limits);
    }


    public static async Task<StudentModel?> GetStudentById(int? id)
    {
        if (id is null || id == Utils.INVALID_ID)
        {
            return null;
        }
        using var conn = await Utils.GetAndOpenConnectionFactory();

        var sParams = new SQLParameterCollection();
        var p1 = sParams.Add((int)id);
        var where = new ComplexWhereCondition(
            new WhereCondition(
                new Column("id", "students"),
                p1,
                WhereCondition.Relations.Equal
            )
        );
        var found = await FindUniqueStudents(new QueryLimits(0, 1), additionalConditions: where, additionalParameters: sParams);
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

        await using var connection = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "SELECT EXISTS(SELECT id FROM students WHERE id = @p1)";
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

    public string GetName()
    {

        if (_russianCitizenship != null)
        {
            return _russianCitizenship.GetName();
        }
        return "Не указано";
    }

    public int GetAgeOnDate(DateTime date)
    {
        int diff = date.Year - _dateOfBirth.Year;
        if (diff <= 0)
        {
            return 0;
        }
        if (date.Month >= _dateOfBirth.Month && date.Day >= _dateOfBirth.Day)
        {
            return diff;
        }
        return --diff;

    }

    public async Task<ResultWithoutValue> Save(ObservableTransaction? scope = null)
    {

        if (_actualAddress is not null)
        {
            var result = await _actualAddress.Save(scope);
            if (result.IsFailure)
            {
                return result;
            }
        }
        if (_russianCitizenship is not null)
        {
            var result = await _russianCitizenship.Save(scope);
            if (result.IsFailure)
            {
                return result;
            }
        }

        if (_id is not null)
        {
            await Update(scope);
            return ResultWithoutValue.Success();
        }

        await using NpgsqlConnection connection = await Utils.GetAndOpenConnectionFactory();

        var cmdText = "INSERT INTO students( " +
            "snils, actual_address, date_of_birth, rus_citizenship_id, " +
            "gender, grade_book_number, target_education_agreement, gia_mark, gia_demo_exam_mark, paid_education_agreement, admission_score) " +
            "VALUES (@p1, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12) RETURNING id";

        NpgsqlCommand cmd;
        if (scope != null)
        {
            cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }
        else
        {
            cmd = new NpgsqlCommand(cmdText, connection);
        }


        cmd.Parameters.Add(new NpgsqlParameter<string>("p1", _snils));
        cmd.Parameters.Add(new NpgsqlParameter("p3", ActualAddress is null ? DBNull.Value : ActualAddress.Id));
        cmd.Parameters.Add(new NpgsqlParameter<DateTime>("p4", _dateOfBirth));
        cmd.Parameters.Add(new NpgsqlParameter("p5", RussianCitizenshipId == null ? DBNull.Value : RussianCitizenshipId.Value));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p6", (int)_gender));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p7", _gradeBookNumber));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p8", (int)_targetAgreementType.AgreementType));
        cmd.Parameters.Add(new NpgsqlParameter("p9", _giaMark == null ? DBNull.Value : (int)_giaMark));
        cmd.Parameters.Add(new NpgsqlParameter("p10", _giaDemoExamMark == null ? DBNull.Value : (int)_giaDemoExamMark));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p11", (int)_paidAgreementType.AgreementType));
        cmd.Parameters.Add(new NpgsqlParameter<decimal>("p12", _admissionScore));

        await using (cmd)
        {
            using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            _id = (int)reader["id"];
        }
        return ResultWithoutValue.Success();
    }

    public async Task Update(ObservableTransaction? scope = null)
    {
        using var conn = await Utils.GetAndOpenConnectionFactory();
        // полное обновление, по идее, невозможно, поэтому используется сокращенная форма
        // для обеспечения непротиворечивости истории студента
        var cmdText = "UPDATE public.students " +
        " SET snils=@p1, actual_address=@p3, date_of_birth=@p4, rus_citizenship_id=@p5, " +
        " gender=@p6, grade_book_number=@p7, target_education_agreement=@p8, gia_mark=@p9, " +
        " gia_demo_exam_mark=@p10, admission_score=@p11" + // ", paid_education_agreement=@p12 " +
        " WHERE id = @p13";
        NpgsqlCommand cmd;
        if (scope is null)
        {
            cmd = new NpgsqlCommand(cmdText, conn);
        }
        else
        {
            cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }
        cmd.Parameters.Add(new NpgsqlParameter<string>("p1", _snils));
        cmd.Parameters.Add(new NpgsqlParameter("p3", ActualAddress is null ? DBNull.Value : ActualAddress.Id));
        cmd.Parameters.Add(new NpgsqlParameter<DateTime>("p4", _dateOfBirth));
        cmd.Parameters.Add(new NpgsqlParameter("p5", (int)_russianCitizenship.Id));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p6", (int)_gender));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p7", _gradeBookNumber));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p8", (int)_targetAgreementType.AgreementType));
        cmd.Parameters.Add(new NpgsqlParameter("p9", _giaMark is null ? DBNull.Value : (int)_giaMark));
        cmd.Parameters.Add(new NpgsqlParameter("p10", _giaDemoExamMark is null ? DBNull.Value : (int)_giaDemoExamMark));
        cmd.Parameters.Add(new NpgsqlParameter<decimal>("p11", _admissionScore));
        // cmd.Parameters.Add(new NpgsqlParameter<int>("p12", (int)_paidAgreementType.AgreementType));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p13", (int)_id));
        using (cmd)
        {
            await cmd.ExecuteNonQueryAsync();
        }
    }
}



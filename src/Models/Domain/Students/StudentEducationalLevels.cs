using Npgsql;
using Contingent.Models.Domain.Specialities;
using Contingent.Controllers.DTO.In;
using Utilities;
using System.Collections;


namespace Contingent.Models.Domain.Students;

public class StudentEducationalLevelRecord
{

    private LevelOfEducation _level;
    public string RussianName => _level.RussianName;
    public LevelOfEducation Level => _level;
    public StudentModel Owner { get; private set; }
    private StudentEducationalLevelRecord(LevelOfEducation lvl, StudentModel owner)
    {
        _level = lvl;
        Owner = owner;
    }

    public static Result<StudentEducationalLevelRecord> Create(StudentEducationRecordInDTO dto, StudentModel model)
    {

        if (dto == null)
        {
            return Result<StudentEducationalLevelRecord>.Failure(new ValidationError("general", "DTO не может быть null"));
        }
        if (model is null)
        {
            return Result<StudentEducationalLevelRecord>.Failure(new ValidationError(nameof(Owner), "Студент указан неверно"));
        }
        if (!LevelOfEducation.TryGetByLevelCode(dto.Level, out LevelOfEducation? foundType))
        {
            return Result<StudentEducationalLevelRecord>.Failure(new ValidationError(nameof(Level), "Неверный тип записи об обучении"));
        }
        var created = new StudentEducationalLevelRecord(foundType!, model);

        return Result<StudentEducationalLevelRecord>.Success(created);
    }

    public void SaveRecord(ObservableTransaction? scope = null)
    {
        var byStudent = GetByOwner(Owner);
        // один и тот же тег не может быть записан на студента дважды
        if (byStudent.Any(x => x == this))
        {
            return;
        }
        var cmdText = "INSERT INTO education_tag_history( " +
                " student_id, level_code) VALUES (@p1, @p2)";
        NpgsqlCommand cmd;
        using var conn = Utils.GetAndOpenConnectionFactory().Result;
        if (scope is null)
        {
            cmd = new NpgsqlCommand(cmdText, conn);
        }
        else
        {
            cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", (int)Owner.Id!));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p2", (int)_level.LevelCode));
        cmd.ExecuteNonQuery();
        conn?.Dispose();
    }
    public static IReadOnlyCollection<StudentEducationalLevelRecord> GetByOwner(StudentModel? owner)
    {
        if (owner is null || owner.Id is null)
        {
            return new List<StudentEducationalLevelRecord>();
        }
        using var conn = Utils.GetAndOpenConnectionFactory().Result;
        using var command = new NpgsqlCommand("SELECT * FROM education_tag_history WHERE student_id = @p1", conn);
        command.Parameters.Add(new NpgsqlParameter<int>("p1", (int)owner.Id));
        using var reader = command.ExecuteReader();
        var found = new List<StudentEducationalLevelRecord>();
        if (!reader.HasRows)
        {
            return found;
        }
        while (reader.Read())
        {
            found.Add(new StudentEducationalLevelRecord(
                LevelOfEducation.GetByLevelCode((int)reader["level_code"]), owner));
        }
        return found;
    }

    public static bool operator ==(StudentEducationalLevelRecord left, StudentEducationalLevelRecord right)
    {
        if (left is null || right is null)
        {
            return false;
        }
        else
        {
            return left.Owner == right.Owner && left._level == right._level;
        }
    }
    public static bool operator !=(StudentEducationalLevelRecord left, StudentEducationalLevelRecord right)
    {
        return !(left == right);
    }
}

public class StudentEducation : IEnumerable<StudentEducationalLevelRecord>
{
    private List<StudentEducationalLevelRecord> _levels;
    private StudentModel _owner;
    public StudentEducation(StudentModel owner)
    {
        _levels = new List<StudentEducationalLevelRecord>(StudentEducationalLevelRecord.GetByOwner(owner));
        _owner = owner;
    }
    public void Add(StudentEducationalLevelRecord record)
    {
        if (record is not null && record.Owner.Equals(_owner) && !_levels.Any(l => l == record))
        {
            _levels.Add(record);
        }
    }

    public IEnumerator<StudentEducationalLevelRecord> GetEnumerator()
    {
        return _levels.GetEnumerator();
    }

    public void Save()
    {
        foreach (var level in _levels)
        {
            level.SaveRecord();
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _levels.GetEnumerator();
    }

    public bool IsHigherOrEqualThan(LevelOfEducation level)
    {
        return _levels.Any(x => x.Level >= level);
    }
}


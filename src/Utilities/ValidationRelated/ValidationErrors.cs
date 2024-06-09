using System.Text;
using System.Text.Json.Serialization;
using Contingent.Controllers.DTO.Out;
using Contingent.Models.Domain.Students;

namespace Contingent.Utilities;
[Serializable]
public class ValidationError
{

    private string _message;
    private string _propertyName;

    private string? _path;
    private string? _line;

    [JsonPropertyName("err")]
    public string ErrorMessage { get => _message; }
    [JsonPropertyName("field")]
    public string PropertyName { get => _propertyName; }

    [JsonIgnore]
    public string? FilePath { get => _path; }
    [JsonIgnore]
    public string? Line { get => _line; }

    public ValidationError(string propName, string exceptionMessage)
    {
        _message = exceptionMessage;
        _propertyName = propName;
        _line = null;
        _path = null;
    }
    public ValidationError(string exceptionMessage)
    {
        _message = exceptionMessage;
        _propertyName = ErrorCollectionDTO.EmptyInputIdentifier;
        _line = null;
        _path = null;
    }
    public ValidationError(string propName, string exceptionMessage, string path, string line)
    {
        _message = exceptionMessage;
        _propertyName = propName;
        _line = line;
        _path = path;

    }
    public override string ToString()
    {
        return "Ошибка: " + _message + "\t" + "Свойство: " + _propertyName;
    }

    public string ToUserString()
    {
        return "Ошибка: " + _message;
    }

    public static bool operator ==(ValidationError? left, ValidationError? right)
    {
        if (left is null)
        {
            return false;
        }
        if (right is null)
        {
            return false;
        }
        return left._propertyName == right._propertyName;
    }
    public static bool operator !=(ValidationError? left, ValidationError? right)
    {
        return !(left == right);
    }
    public override bool Equals(object? obj)
    {
        if (obj == null)
        {
            return false;
        }
        if (obj.GetType() != typeof(ValidationError))
        {
            return false;
        }
        return this == (ValidationError)obj;
    }
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public virtual void Log()
    {
        Console.WriteLine(
            "Error detected: \n" +
            "Field: " + _propertyName + "\n" +
            "Path: " + (_path ?? "Not mentioned") + "\n" +
            "Line: " + (_line ?? "Not mentioned") + "\n" +
            "Message: " + _message
        );
    }

    public static ValidationError GetNullReceivedError(string err) => new ValidationError(ErrorCollectionDTO.EmptyInputIdentifier, err);

}

public class DbIntegrityValidationError : ValidationError
{
    public DbIntegrityValidationError(string propName, string exceptionMessage) : base(propName, exceptionMessage)
    {

    }
}

public class OrderValidationError : ValidationError
{
    public OrderValidationError(
        string exceptionMessage,
        StudentModel student)
        : base("OrderGenericError", string.Format("Студент {0} не может быть проведен по причине {1}", student.GetName(), exceptionMessage))
    {

    }
}

public class ImportValidationError : ValidationError
{
    public const string ImportErrorName = "IMPORT_ERROR";
    public ImportValidationError(string exceptionMessage) : base(ImportErrorName, exceptionMessage)
    {

    }
}








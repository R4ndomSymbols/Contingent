using System.Globalization;

namespace Contingent.Controllers.DTO.Out;

[Serializable]
public class ErrorCollectionDTO
{
    // идентификатор критической ошибки
    public const string CriticalErrorIdentifier = "CRITICAL_ERROR";
    // идентификатор неконкретной ошибки
    public const string GeneralErrorIdentifier = "GENERAL_ERROR";
    // индетификатор пустого ввода
    public const string EmptyInputIdentifier = "NULL_RECEIVED_ERROR";
    public IReadOnlyCollection<ErrorDTO> Errors { get; private init; }

    public ErrorCollectionDTO(IEnumerable<ValidationError?>? errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }
        var transform = new List<ErrorDTO>();
        foreach (var e in errors)
        {
            transform.Add(new ErrorDTO(e));
        }
        Errors = transform;
    }

    public ErrorCollectionDTO(ValidationError error)
    {
        var transform = new ErrorDTO(error);
        Errors = new List<ErrorDTO>() { transform };
    }

    public static ErrorCollectionDTO GetCriticalError(string error)
    {
        return new ErrorCollectionDTO(
            new ValidationError(
                CriticalErrorIdentifier, error
            )
        );
    }
    public static ErrorCollectionDTO GetGeneralError(string error)
    {
        return new ErrorCollectionDTO(
            new ValidationError(
                GeneralErrorIdentifier, error
            )
        );
    }



}

public static class ErrorCollectionDTOExtensions
{
    public static ErrorCollectionDTO AsErrorCollection(this ValidationError error)
    {
        return new ErrorCollectionDTO(error);
    }

    public static ErrorCollectionDTO AsErrorCollection(this IEnumerable<ValidationError?> errors)
    {
        return new ErrorCollectionDTO(errors);
    }

}


namespace StudentTracking.Controllers.DTO.Out;

[Serializable]
public class ErrorsDTO {

    public IReadOnlyCollection<ErrorDTO> Errors {get; private init; }

    public ErrorsDTO(IReadOnlyCollection<ValidationError?>? errors){
        if (errors is null){
            throw new ArgumentNullException(nameof(errors) + " не может иметь значение null");
        }
        var transform = new List<ErrorDTO>();
        foreach (var e in errors){
            transform.Add(new ErrorDTO(e));
        }
        Errors = transform;
    }

    public ErrorsDTO(ValidationError error){
        var transform = new ErrorDTO(error);
        Errors = new List<ErrorDTO>(){transform};
    }

}

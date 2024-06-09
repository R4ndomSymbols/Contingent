using Contingent.Utilities;

namespace Contingent.Controllers.DTO.Out;

[Serializable]
public class ErrorDTO
{

    public string FrontendFieldName { get; private init; }
    public string MessageForUser { get; private init; }

    public ErrorDTO(ValidationError? model)
    {
        if (model is null)
        {
            throw new ArgumentException("Ошибка не может иметь значение null");
        }
        FrontendFieldName = model.PropertyName;
        MessageForUser = model.ErrorMessage;
    }

}

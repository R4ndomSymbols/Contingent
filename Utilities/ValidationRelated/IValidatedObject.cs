namespace Utilities.Validation;


public interface IValidatedObject {

    public IReadOnlyCollection<ValidationError> GetErrors();

    public bool CheckErrorsExist();

    public bool PerformValidation(Func<bool> validationDelegate, ValidationError error);

}

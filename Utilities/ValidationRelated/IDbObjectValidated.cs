namespace Utilities.Validation;

public interface IDbObjectValidated : IValidatedObject, IEquatable<IDbObjectValidated>{

    public bool CheckIntegrityErrorsExist();

    public IReadOnlyCollection<ValidationError> GetIntegriryErrors();

    public IDbObjectValidated? GetDbRepresentation();

}


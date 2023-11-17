namespace Utilities.Validation;

public interface IDbObjectValidated : IValidatedObject, IEquatable<IDbObjectValidated>{

    public bool CheckIntegrityErrorsExist();

    public IDbObjectValidated? GetDbRepresentation();

}


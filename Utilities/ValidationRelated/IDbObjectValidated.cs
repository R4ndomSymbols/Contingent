namespace Utilities.Validation;

public interface IDbObjectValidated : IValidatedObject, IEquatable<IDbObjectValidated>{

    public bool CheckIntegrityErrorsExist();

    public IReadOnlyCollection<ValidationError> GetIntegriryErrors();

    public IReadOnlyCollection<ValidationError> FilterErrorsByName(string name);

    public Task<IDbObjectValidated?> GetDbRepresentation(ObservableTransaction? within);
}


using Contingent.Utilities;

namespace Contingent.Models.Domain.Address;

public interface IAddressPart
{
    public int Id { get; }
    public AddressRecord ToAddressRecord();
    public IEnumerable<IAddressPart> GetDescendants(ObservableTransaction? scope);
    public string ToString();
    public bool Equals(object? obj);
}

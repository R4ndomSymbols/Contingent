namespace StudentTracking.Models.Domain.Address;

public interface IAddressPart {
    public int Id {get; }
    public AddressRecord ToAddressRecord();
    public IEnumerable<IAddressPart> GetDescendants();
    public string ToString();
    public bool Equals(object? obj); 
}

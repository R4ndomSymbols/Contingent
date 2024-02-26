namespace StudentTracking.Models.Domain.Address;

public interface IAddressPart {
    public AddressRecord ToAddressRecord();
    public IEnumerable<IAddressPart> GetDescendants();
    public string ToString(); 
}

namespace Contingent.Models.Domain.Address;

// id = 0 - id не указан
//
public class AddressRecord
{

    // уникальный идентификатор кусочка адреса
    public int AddressPartId;
    // родитель, null в случае корневых субъектов
    public int? ParentId;
    // уровень адреса, по которому будет выводиться его тип
    public int AddressLevelCode;
    // тип топонима, который представляет собой его название
    public int ToponymType;
    // название структуры
    public string AddressName;

    public AddressRecord()
    {
        AddressName = string.Empty;
    }


}



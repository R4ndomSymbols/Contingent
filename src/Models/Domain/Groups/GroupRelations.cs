namespace Contingent.Models.Domain.Groups;

public enum GroupRelations
{
    None = 0,
    DirectParent = 1,
    DirectChild = 2,
    // группа, у которой совпадает специальность, курс, форма обучения и финансирования
    Sibling = 3,
    Other = 4
}
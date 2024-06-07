using Contingent.Models.Domain.Flow;

namespace Contingent.Controllers.DTO.In;

[Serializable]
// класс нужен для ограничения запроса
// он устанавливает ограничения на уровне базы данных
public class StudentSearchQuerySourceDTO
{

    public int? OrderId { get; set; }
    // либо все студенты должны быть в приказе, либо нет
    public string? OrderMode { get; set; }

    public StudentSearchQuerySourceDTO()
    {
        OrderMode = OrderRelationMode.OnlyIncluded.ToString();
    }
}

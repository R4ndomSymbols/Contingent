using System.Diagnostics;
using Microsoft.VisualBasic;
using Contingent.Models.Domain.Address;
using Contingent.Models.Domain.Flow;
using Contingent.SQL;
using Contingent.Statistics.Tables.Headers;
using Contingent.Models.Infrastructure;
using Contingent.Models.Domain.Students;
using Contingent.Models.Domain.Orders.OrderData;

namespace Contingent.Statistics.Tables;


public class AddressTable : ITable
{
    private StatisticTable<StudentModel> _model;
    public string DisplayedName => _model.TableName;
    public string HtmlContent => _model.ToHtmlTable();
    public Period StatisticPeriod { get; set; }

    public AddressTable(Period statsPeriod)
    {
        if (!statsPeriod.IsOneMoment())
        {
            throw new Exception("Адресация возможна только на дату");
        }
        StatisticPeriod = statsPeriod;
        // горизонтальная шапка таблицы
        var verticalRoot = new ColumnHeaderCell<StudentModel>();
        var addressHeader1 = new ColumnHeaderCell<StudentModel>(
            "Субъект федерации (прописка)",
            verticalRoot

        );
        var addressHeader2 = new ColumnHeaderCell<StudentModel>(
            "Район субъекта (прописка)",
            verticalRoot

        );
        for (int i = 1; i <= 4; i++)
        {
            var courseBlock = TemplateHeaders.GetBaseCourseHeader<StudentModel>(
                i,
                (StudentModel s) => s,
                (StudentModel s) => s.GetHistory(null, statsPeriod.End).GetGroupOnDate(StatisticPeriod.End),
                verticalRoot
            );
        }
        // вертикальная шапка таблицы
        var horizontalRoot = new RowHeaderCell<StudentModel>();
        var allRegions = AddressModel.FindByAddressLevel(FederalSubject.ADDRESS_LEVEL, null).Select(x => FederalSubject.Create(x));
        foreach (var reg in allRegions)
        {
            var regHeader = new RowHeaderCell<StudentModel>(
                reg.ToString(),
                horizontalRoot,
                new Filter<StudentModel>(
                    (source) => source.Where(
                        model =>
                        {
                            var got = model?.RussianCitizenship?.LegalAddress;
                            if (got is null)
                            {
                                return false;
                            }
                            return got.Contains(reg);
                        }
                    )
                )
            );
            // добавление к дерево происходит неявно
            TemplateHeaders.GetAddressRowHeader(
                (StudentModel s) => s?.RussianCitizenship?.LegalAddress,
                reg.GetDescendants(null),
                regHeader
            );
        }
        var horizontalHeader = new TableColumnHeader<StudentModel>(
            verticalRoot, false
        );
        var verticalHeader = new TableRowHeader<StudentModel>(
            horizontalRoot,
            horizontalHeader,
            false
        );
        var source = StudentHistory.GetStudentByOrderState(StatisticPeriod.End,
        OrderTypeInfo.EnrollmentTypes,
        OrderTypeInfo.DeductionTypes,
        null);
        _model = new StatisticTable<StudentModel>(horizontalHeader, verticalHeader, source, "Распределение студентов по прописке");
    }
}
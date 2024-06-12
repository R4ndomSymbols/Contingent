using System.Diagnostics;
using Microsoft.VisualBasic;
using Contingent.Models.Domain.Address;
using Contingent.Models.Domain.Flow;
using Contingent.SQL;
using Contingent.Statistics.Tables.Headers;

namespace Contingent.Statistics.Tables;


public class AddressTable : ITable
{
    private StatisticTable<StudentFlowRecord> _model;
    public string DisplayedName => _model.TableName;
    public string Html => _model.ToHtmlTable();

    public AddressTable()
    {
        // горизонтальная шапка таблицы
        var verticalRoot = new ColumnHeaderCell<StudentFlowRecord>();
        var addressHeader1 = new ColumnHeaderCell<StudentFlowRecord>(
            "Субъект федерации (прописка)",
            verticalRoot

        );
        var addressHeader2 = new ColumnHeaderCell<StudentFlowRecord>(
            "Район субъекта (прописка)",
            verticalRoot

        );
        for (int i = 1; i <= 4; i++)
        {
            var courseBlock = TemplateHeaders.GetBaseCourseHeader<StudentFlowRecord>(
                i,
                (StudentFlowRecord s) => s.Student,
                (StudentFlowRecord s) => s.GroupTo,
                verticalRoot
            );
        }
        // вертикальная шапка таблицы
        var horizontalRoot = new RowHeaderCell<StudentFlowRecord>();
        var allRegions = AddressModel.FindByAddressLevel(FederalSubject.ADDRESS_LEVEL, null).Select(x => FederalSubject.Create(x));
        foreach (var reg in allRegions)
        {
            var regHeader = new RowHeaderCell<StudentFlowRecord>(
                reg.ToString(),
                horizontalRoot,
                new Filter<StudentFlowRecord>(
                    (source) => source.Where(
                        model =>
                        {
                            var got = model?.Student?.RussianCitizenship?.LegalAddress;
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
                (StudentFlowRecord s) => s.Student?.RussianCitizenship?.LegalAddress,
                reg.GetDescendants(null),
                regHeader
            );
        }
        var horizontalHeader = new TableColumnHeader<StudentFlowRecord>(
            verticalRoot, false
        );
        var verticalHeader = new TableRowHeader<StudentFlowRecord>(
            horizontalRoot,
            horizontalHeader,
            false
        );
        var source = StudentHistory.GetLastRecordsForManyStudents(new QueryLimits(0, 2000), (false, true));
        _model = new StatisticTable<StudentFlowRecord>(horizontalHeader, verticalHeader, source, "Распределение студентов по прописке");
    }


}
using Contingent.Controllers.DTO.In;
using Contingent.SQL;
using Contingent.Utilities;
using Contingent.Models.Domain.Students;
using Contingent.Models.Domain.Groups;
using Contingent.Models.Domain.Citizenship;

namespace Contingent.Models.Domain.Orders;
public static class OrderDataExtractions
{
    public static Result<StudentModel> GetStudent(StudentStatementDTO? dto)
    {
        if (dto is null)
        {
            return Result<StudentModel>.Failure(new ValidationError("Источник данных (dto) должен быть указан"));
        }
        var student = StudentModel.GetStudentById(dto.StudentId);
        if (student is null)
        {
            var paramCollection = new SQLParameterCollection();
            var nameSplit = dto.Name.Split(' ');
            var whereClauseForCitizenship = RussianCitizenship.GetFilterClause(new RussianCitizenshipInDTO()
            {
                Surname = nameSplit.ElementAtOrDefault(0)!,
                Name = nameSplit.ElementAtOrDefault(1)!,
                Patronymic = nameSplit.ElementAtOrDefault(2)
            },
            ref paramCollection);
            var foundBy = StudentModel.FindStudents(
                new QueryLimits(0, 2),
                null,
                whereClauseForCitizenship.Unite(
                    ComplexWhereCondition.ConditionRelation.AND,
                    new ComplexWhereCondition(
                        new WhereCondition(
                            new Column("grade_book_number", "students"),
                            paramCollection.Add(dto.StudentGradeBookNumber),
                            WhereCondition.Relations.Equal
                        )
                    )
                ), paramCollection).Result;
            if (foundBy.Count == 0)
            {
                return Result<StudentModel>.Failure(new ValidationError("Студент не найден"));
            }
            if (foundBy.Count != 1)
            {
                return Result<StudentModel>.Failure(new ValidationError("Студент не может быть определен"));
            }
            return Result<StudentModel>.Success(foundBy.First());
        }
        return Result<StudentModel>.Success(student);
    }
    public static Result<GroupModel> GetGroup(GroupStatementDTO dto)
    {
        if (dto is null)
        {
            return Result<GroupModel>.Failure(new ValidationError("Источник данных (dto) должен быть указан"));
        }
        if (Utils.IsValidId(dto.GroupId))
        {
            var group = GroupModel.GetGroupById(dto.GroupId);
            if (group is null)
            {
                return Result<GroupModel>.Failure(new ValidationError("Группа не найдена"));
            }
            return Result<GroupModel>.Success(group);
        }
        var groupsFound = GroupModel.FindGroupsByName(
            new QueryLimits(0, 2),
            dto.GroupName,
            false,
            true
        );
        if (groupsFound.Count == 0)
        {
            return Result<GroupModel>.Failure(new ValidationError("Группа не найдена"));
        }
        if (groupsFound.Count != 1)
        {
            return Result<GroupModel>.Failure(new ValidationError("Группа не может быть определена однозначно"));
        }
        return Result<GroupModel>.Success(groupsFound.First());
    }
}


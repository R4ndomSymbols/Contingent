using Contingent.Controllers.DTO.In;
using Contingent.SQL;
using Utilities;
using Contingent.Models.Domain.Students;
using Contingent.Models.Domain.Groups;
using Contingent.Models.Domain.Citizenship;

namespace Contingent.Models.Domain.Orders;
public abstract class StudentStatement
{
    protected Result<StudentModel> GetStudent(StudentStatementDTO? dto)
    {
        if (dto is null)
        {
            return Result<StudentModel>.Failure(new ValidationError("Источник данных (dto) должен быть указан"));
        }
        var student = StudentModel.GetStudentById(dto.StudentId).Result;
        if (student is null)
        {
            var nameSplit = dto.Name.Split(' ');
            var whereClauseForCitizenship = RussianCitizenship.GetFilterClause(new RussianCitizenshipInDTO()
            {
                Surname = nameSplit.ElementAtOrDefault(0)!,
                Name = nameSplit.ElementAtOrDefault(1)!,
                Patronymic = nameSplit.ElementAtOrDefault(2)
            },
            out SQLParameterCollection paramCollection);
            var foundBy = StudentModel.FindUniqueStudents(
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
    protected Result<GroupModel> GetGroup(GroupStatementDTO dto)
    {
        if (dto is null)
        {
            return Result<GroupModel>.Failure(new ValidationError("Источник данных (dto) должен быть указан"));
        }
        var group = GroupModel.GetGroupById(dto.GroupId);
        if (group is null)
        {
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
        return Result<GroupModel>.Success(group);
    }
}


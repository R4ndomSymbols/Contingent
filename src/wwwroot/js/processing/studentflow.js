import { Utilities } from "../site.js";
let utils = new Utilities();
const includePostfix = "_inc";
const excludePostfix = "_ex";
const identityIncludedPostfix = "_inc_id";
const identityExcludedPostfix = "_ex_id";
const studentPinnedGroupInputPostfix = "_n_group"
const startDateInputPostfix = "_st_date";
const endDateInputPostfix = "_end_date"
let currentDisplayingPolicy = undefined;
let currentOrderId = undefined;
let pinnedTable = undefined;
let unpinnedTable = undefined;

// здесь хранятся все студенты
let studentsPool = [];
// здесь находятся прикрепленные студенты
let studentsPinned = [];
// студенты, числящиеся в приказе
// removed - свойство, отвечающее за удаление
let studentsInOrder = [];

init();

function init() {
    currentOrderId = Number($("#current_order").attr("order_id"));
    currentDisplayingPolicy = String($("#current_order").attr("group_behavior"))
    if (currentDisplayingPolicy === "PeriodInput") {
        $("#input_title").text("Период отпуска");
    }
    setStudentsInOrder();
    unpinnedTable = $("#students_not_in_order");
    pinnedTable = $("#students_in_order");
    $("#close_order").on("click", function () {
        closeOrder();
    });
}


function setUpStudents(students) {
    // в пуле не должно быть уже прикрепленных студентов
    studentsPool = students.filter(x => !studentsPinned.some(z => z.studentId === x.studentId));
    $.each(studentsPool, function (index, student) {
        const closureSaveStudent = student;
        closureSaveStudent.pin = getPinFunc(closureSaveStudent)
        closureSaveStudent.unpin = getUnPinFunc(closureSaveStudent)
        closureSaveStudent.unpin();
    });

}

function getStudentSearchText() {
    let text = $("#fullname_filter").val();
    return text;
}
function getGroupSearchText() {
    let text = $("#group_filter").val();
    return text;
}
function getPinFunc(student) {
    return function () {
        const closure = student;
        // удаление студента из массива неприкрепленных
        studentsPool = studentsPool.filter(x => x.studentId !== closure.studentId);
        studentsPinned.push(closure)
        const studentId = String(closure.studentId);
        const fullName = closure.studentFullName
        const removeButtonId = studentId + excludePostfix;
        const rowId = studentId + identityIncludedPostfix;
        const startDateInput = studentId + startDateInputPostfix;
        const endDateInput = studentId + endDateInputPostfix;
        const groupInputId = studentId + studentPinnedGroupInputPostfix;
        // удаляем студента из другой таблицы
        $("#" + closure.studentId + identityExcludedPostfix).remove();
        // функция добавления в таблицу
        let header = `<tr id="${rowId}"> <td>${fullName}</td>`;
        let buttonPart = `<td><button id="${removeButtonId}">-</button></td></tr>`
        let content = undefined;
        let callback = undefined;
        if (currentDisplayingPolicy === "MustChange") {
            content =
                `<td>
                <input type="text" id="${groupInputId}"/>
            </td>`;
            callback = () => findGroupsAndSetAutoComplete(closure);
        }
        else if (currentDisplayingPolicy === "WipeOut") {
            content = "<td>Нет</td>";
            callback = () => { }
        }
        else if (currentDisplayingPolicy == "PeriodInput") {
            content =
                `<td>
                    <input type="text" placeholder="Начало" id = "${startDateInput}"/>
                    <input type="text" placeholder="Конец" id = "${endDateInput}"/>
                </td>`;
            callback = () => {
                addDateListener(closure, "startDate", startDateInput);
                addDateListener(closure, "endDate", endDateInput);
            };
        }
        pinnedTable.append(header + content + buttonPart);
        console.log(callback);
        callback();
        $("#" + removeButtonId).on("click", function () { student.unpin() });
    }
}

function addDateListener(student, dateFieldName, dateInputId) {
    $("#" + dateInputId).on("input", function () {
        student[dateFieldName] = $(this).val();
    });
}

function getUnPinFunc(student) {
    return function () {
        // удаление из массива прикрепленных
        studentsPinned = studentsPinned.filter(x => x.studentId !== student.studentId);
        studentsPool.push(student);
        // удаление из таблицы прикрепленных
        $("#" + student.studentId + identityIncludedPostfix).remove();
        // добавление в таблицу неприкрепленных
        unpinnedTable.append(
            `
            <tr id="${student.studentId + identityExcludedPostfix}"> 
                <td>
                    ${student.studentFullName}
                </td>
                <td>
                    ${student.groupName}
                </td>
                <td>
                    <button id="${student.studentId + includePostfix}">
                    +
                    </button>
                </td>

            </tr>
            `
        );
        $("#" + student.studentId + includePostfix).on("click", function () { student.pin() });
    }
}
// поиск и добавление студентов, которые уже находятся в приказе
function setStudentsInOrder() {
    $.ajax({
        type: "POST",
        url: "/students/search/find",
        data: JSON.stringify(
            {
                Name: "",
                GroupName: "",
                Source: {
                    OrderId: currentOrderId,
                    OrderMode: "OnlyIncluded"
                },
                PageSkipCount: 0,
                PageSize: 30
            }
        ),
        contentType: "application/json",
        beforeSend: utils.setAuthHeader,
        success: function (response) {
            $.each(response, function (index, student) {
                const studentClosure = student;
                studentsInOrder.push(studentClosure);
                let stdId = String(student["studentId"])
                pinnedTable.append(
                    `
                    <tr> 
                        <td>
                            ${student["studentFullName"]}
                        </td>
                        <td>
                            ${student["groupName"]}
                        </td>
                        <td id ="${stdId + identityExcludedPostfix}">
                            <button id ="${stdId + excludePostfix}">
                                Исключить
                            </button>
                        </td>
                    </tr>
                    `
                );
                $("#" + stdId + excludePostfix).on("click", function () {
                    let studentAssociated = studentClosure;
                    this.remove();
                    $("#" + stdId + identityExcludedPostfix).append(
                        "<p>Будет исключен<p>"
                    )
                    studentAssociated.removed = true;
                });
            });
        }
    });
}
// поиск студентов
$("#find_students").on("click", function () {
    // убираем всех студентов, которые оказались вне приказа
    unpinnedTable.empty();
    studentsPool = [];
    $.ajax({
        type: "POST",
        url: "/students/search/find",
        data: JSON.stringify(
            {
                Name: getStudentSearchText(),
                GroupName: getGroupSearchText(),
                Source: {
                    OrderId: currentOrderId,
                    OrderMode: "OnlyExcluded"
                },
                PageSize: 30,
                PageSkipCount: 0
            }
        ),
        contentType: "application/json",
        beforeSend: utils.setAuthHeader,
        success: function (studentsJSON) {
            setUpStudents(studentsJSON)
        }
    });
});

function findGroupsAndSetAutoComplete(student) {
    let inputName = String(student.studentId) + studentPinnedGroupInputPostfix
    let request = (ctrlName) => {
        let groupInput = $("#" + ctrlName);
        let searchText = groupInput.val();
        if (String(searchText).length < 3 || searchText === undefined) {
            return
        }
        let groupsAvailable = [];
        $.ajax({
            type: "POST",
            url: "/groups/search/find",
            data: JSON.stringify({
                GroupName: searchText
            }),
            dataType: "json",
            contentType: "application/json",
            beforeSend: utils.setAuthHeader,
            success: function (response) {
                $.each(response, function (i, group) {
                    groupsAvailable.push(
                        {
                            label: group["groupName"],
                            value: group["groupId"]
                        }
                    )
                });
                groupInput.autocomplete({
                    source: groupsAvailable,
                    select: function (event, ui) {
                        groupInput.val(ui.item.label)
                        student.groupToId = ui.item.value;
                        event.preventDefault();
                    }
                });
                groupInput.autocomplete("search");
            }
        });
    }
    $("#" + inputName).on("input", function () {
        utils.registerScheduledQuery(function () { request(inputName) });
    });
}

$("#save_changes").on("click", function () {
    let obj = getOrderJsonData();
    if (obj.length == 0) {
        return;
    }
    $.ajax({
        type: "POST",
        url: "/studentflow/save/" + String(currentOrderId),
        data: JSON.stringify(obj),
        contentType: "application/json",
        beforeSend: utils.setAuthHeader,
        success: function (response) {
            alert("Сохранение новых студентов прошло успешно")
        },
        error: function (response, a, b) {
            utils.readAndSetErrors(response)
        }
    });

    $.each(studentsInOrder, function (indexInArray, valueOfElement) {
        if (valueOfElement.removed) {
            $.ajax({
                type: "DELETE",
                url: "/studentflow/revert/" + String(currentOrderId) + "/" + String(valueOfElement.studentId),
                beforeSend: utils.setAuthHeader,
                success: function (response) { },
                error: function (xhr, a, b) {
                    utils.readAndSetErrors(xhr)
                }
            });
        }
    });



});

function getOrderJsonData() {
    // определение ввода в зависимости от политики отображения
    let inputType = currentDisplayingPolicy;
    let result = undefined;
    let mainModel = studentsPinned.map(
        (x) => {
            return {
                id: x.studentId,
                newGroup: x.groupToId,
                name: x.studentFullName,
                stDate: x.startDate,
                enDate: x.endDate
            }
        });

    switch (inputType) {
        case "MustChange":
            result =
            {
                Moves: mainModel.map(
                    (x) => {
                        return {
                            Student: {
                                StudentId: x.id
                            },
                            Group: {
                                GroupId: x.newGroup
                            }
                        }
                    }
                )
            }
            break;
        case "NoChange":
        case "WipeOut":
            result = {
                Students: mainModel.map(
                    x => {
                        return {
                            Student: {
                                StudentId: x.id
                            }
                        }
                    }
                )
            }
            break;
        case "PeriodInput":
            result = {
                Statements: mainModel.map(
                    x => {
                        return {
                            Student: {
                                StudentId: x.id
                            },
                            StartDate: x.stDate,
                            EndDate: x.enDate
                        }
                    }
                )
            }
            break;
        case "Undefined":
            alert("Неподдерживаемый приказ")

    }
    return result
}

function closeOrder() {
    $.ajax({
        type: "GET",
        url: "/orders/close/" + String(currentOrderId),
        success: function (response) {
            alert("Приказ успешно закрыт")
            utils.disableField("close_order")
        }
    });
}


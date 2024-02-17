var orderSelectionSource = [];
var orderTypesForDisplayingNames = [];
var selectedOrder = undefined;
var includePostfix = "_inc";
var excludePostfix = "_ex";
var identityIncludedPostfix = "_inc_id";
var identityExcludedPostfix = "_ex_id";
var currentGroupFlag = false;
var studentPinnedGroupInputPostfix = "_n_group"
var groupPolicy
// здесь хранятся все студенты
// pinned - свойство, отвечающее за прикрепленность
var students = [];

const invalidGroupId = "invalid_g"

$(document).ready(function () {
    $.ajax({
        type: "GET",
        url: "/studentflow/inorder/" + String($("#current_order").prop("value")),
        dataType: "json",
        success: function (response) {
            $.each(response, function (index, elem) {
                if (elem["groupId"] === null) {
                    elem["groupId"] = undefined
                }
                $("#students_in_order").append(
                    `
                    <tr> 
                        <td id = ${elem["studentId"]}>
                            ${elem["studentFullName"]}
                        </td>
                        <td id = "${elem["groupId"]}">
                            ${elem["groupName"]}
                        </td>
                        <td>
                            Нет
                        </td>
                    </tr>
                    `
                );
            });
        }
    });
    $("#close_order").on("click", function () {
        closeOrder();
    });

    groupPolicy = String($("#current_order").attr("group_behaviour"))
});

$("#find_students").on("click", function () {
    document.getElementById("students_not_in_order").innerHTML = "";
    students = students.filter((x) => x.pinned);
    $.ajax({
        type: "GET",
        url: "/studentflow/notinorder/" + String($("#current_order").prop("value")),
        success: function (studentsJSON) {
            $.each(studentsJSON, function (i, val) {
                if (!isStudentPinned(val["studentId"])) {
                    addStudentToPool(val);
                }
            });
        }
    });
});

// id - айди студента
// groupNow - айди текущей группы студента
// groupAfter - айди группы после регистрации в приказе

function isStudentPinned(id = -1) {
    var studentfound = students.find((x) => x.studentId == id);
    if (studentfound == undefined) {
        return false;
    }
    else {
        return studentfound.pinned
    }


}

function addStudentToPool(student) {
    students.push(student);
    student.pinned = false;
    $("#students_not_in_order").append(
        `
        <tr id = "${student["studentId"] + identityExcludedPostfix}"> 
            <td>
                ${student["studentFullName"]}
            </td>
            <td>
                ${student["groupName"]}
            </td>
            <td>
                <button id = "${student["studentId"] + includePostfix}">
                +
                </button>
            </td>
        </tr>
        `
    );
    $("#" + student["studentId"] + includePostfix).on("click", function () { includeStudent(student) });
}



function includeStudent(student) {
    student.pinned = true
    if (groupPolicy === "MustChange") {
        $("#students_in_order").append(
            `
                    <tr id="${student["studentId"] + identityIncludedPostfix}"> 
                        <td>
                            ${student["studentFullName"]}
                        </td>
                        <td>
                            <input id = "${student["studentId"] + studentPinnedGroupInputPostfix}"/>
                        </td>
                        <td>
                            <button id="${student["studentId"] + excludePostfix}">
                            -
                            </button>
                        </td>
                    </tr>
                    `);
        findGroupsAndSetAutoComplete(student);
    }
    else if (groupPolicy === "Vipe") {
        $("#students_in_order").append(
            `
                    <tr id="${student["studentId"] + identityIncludedPostfix}"> 
                        <td>
                            ${student["studentFullName"]}
                        </td>
                        <td>
                            Нет
                        </td>
                        <td>
                            <button id="${student["studentId"] + excludePostfix}">
                            -
                            </button>
                        </td>
                    </tr>
                    `);
    }
    document.getElementById(student["studentId"] + identityExcludedPostfix).remove()
    $("#" + student["studentId"] + excludePostfix).on("click", function () { excludeStudent(student); });
}

function excludeStudent(student) {
    student.pinned = false;
    $("#students_not_in_order").append(
        `
            <tr id="${student["studentId"] + identityExcludedPostfix}"> 
                <td>
                    ${student["studentFullName"]}
                </td>
                <td>
                    ${student["groupName"]}
                </td>
                <td>
                    <button id="${student["studentId"] + includePostfix}">
                    +
                    </button>
                </td>

            </tr>
            `
    );
    document.getElementById(student["studentId"] + identityIncludedPostfix).remove();
    $("#" + student["studentId"] + includePostfix).on("click", function () { includeStudent(student) });
}

function findGroupsAndSetAutoComplete(student) {
    var inputName = String(student.studentId) + studentPinnedGroupInputPostfix
    $("#" + inputName).on("keyup", function () {
        var searchText = $("#" + inputName).val()
        if (String(searchText).length < 3) {
            return
        }
        var groupsAvailable = [];
        $.ajax({
            type: "GET",
            url: "/groups/find/" + searchText,
            dataType: "json",
            success: function (response) {
                $.each(response, function (i, val) {
                    groupsAvailable.push(
                        {
                            label: val["groupName"],
                            value: val["groupId"]
                        }
                    )
                });
                $("#" + inputName).autocomplete({
                    source: groupsAvailable,
                    select: function (event, ui) {
                        $("#" + inputName).val(ui.item.label)
                        $("#" + inputName).attr("group_id", ui.item.value)
                        student.nextGroup = ui.item.value;
                        event.preventDefault();
                    }
                });
            }
        });
    })
}

$("#save_changes").on("click", function () {
    var obj = getOrderJsonData();
    $.ajax({
        type: "POST",
        url: "/studentflow/save/" + String($("#current_order").prop("value")),
        data: JSON.stringify(obj),
        contentType: "application/json",
        success: function (response) {
            alert("Сохранение прошло успешно")
        },
        error: function(response){
            alert("Сохранение провалилось (ошибка в данных или закрытый приказ)");
        }
    });
});

function getOrderJsonData() {
    type = String($("#current_order").attr("order_type"))
    var result;
    var mainModel = students.filter((xo) => xo.pinned).map(
        (x) => {
            return {
                id: x.studentId,
                newGroup: x.nextGroup
            }
        });

    switch (type) {
        case "FreeNextCourseTransfer":
        case "FreeEnrollment":
        case "FreeEnrollmentWithTransfer":
        case "FreeReenrollment":
        case "FreeTransferBetweenSpecialities":
            result = 
            {
                Moves: mainModel.map(
                    (x) => {
                        return {
                            StudentId: x.id,
                            GroupToId: x.newGroup
                        }
                    }
                )
            }
        break;
        case "FreeDeductionWithGraduation":
        case "FreeDeductionWithAcademicDebt":
        case "FreeDeductionWithOwnDesire":    
            result = {
                Students: mainModel.map(
                    x => x.id
                )
            }
        case "EmptyOrder":
            alert("Проведение по пустому приказу...")

    }
    return result
}

function closeOrder(){
    $.ajax({
        type: "GET",
        url: "/orders/close/" + $("#current_order").attr("value"),
        success: function (response) {
            alert("Приказ успешно закрыт")
        }
    });



}



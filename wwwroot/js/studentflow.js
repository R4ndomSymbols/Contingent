var orderSelectionSource = [];
var orderTypesForDisplayingNames = [];
var selectedOrder = undefined;
var includePostfix = "_inc";
var excludePostfix = "_ex";
var identityIncludedPostfix = "_inc_id";
var identityExcludedPostfix = "_ex_id";
var currentGroupFlag = false;
var studentPinnedGroupInputPostfix = "_n_group"
// здесь хранятся все студенты
// pinned - свойство, отвечающее за прикрепленность
var students = [];

const invalidGroupId = "invalid_g"
var orderSearchNow = null;

$("#current_order").on("keyup", function () {
    if (orderSearchNow != null) {
        orderSearchNow.abort();
    }
    orderSearchNow = $.ajax({
        type: "GET",
        url: "/orders/filter/" + String($("#current_order").val()),
        dataType: "json",
        success: function (response) {
            orderSelectionSource = [];
            orderAutoCompleteSource = [];
            $.each(response, function (index, orderSug) {
                orderSelectionSource.push(
                    {
                        name: orderSug["displayedName"],
                        id: orderSug["orderId"],
                        groupBehaviour: orderSug["groupBehaviour"]
                    }
                )
                orderAutoCompleteSource.push(
                    {
                        value: orderSug["orderId"],
                        label: orderSug["displayedName"]
                    }
                );
            });
            $("#current_order").autocomplete({
                delay: 100,
                source: orderAutoCompleteSource,
                change: function (event, ui) {
                    if (ui.item === null) {
                        return;
                    }
                    getStudentsByOrderId(ui.item.value);
                },
                select: function (event, ui) {
                    if (ui.item === null) {
                        return;
                    }
                    $("#current_order").val(ui.item.label)
                    $("#current_order").prop("order_id", ui.item.value);
                    $("#current_order").prop("group_behaviour", orderSelectionSource.find(x => x.id === ui.item.value).groupBehaviour);
                    event.preventDefault();
                }
            });
        }
    });
});

function getStudentsByOrderId(orderId) {
    $.ajax({
        type: "GET",
        url: "/studentflow/inorder/" + String($("#current_order").prop("order_id")),
        dataType: "json",
        success: function (response) {
            document.getElementById("students_in_order").innerHTML = "";
            document.getElementById("students_not_in_order").innerHTML = "";
            $.each(response, function (index, elem) {
                if (elem["group"]["groupId"] === null) {
                    elem["group"]["groupId"] = invalidGroupId
                }
                $("#students_in_order").append(
                    `
                    <tr> 
                        <td id = ${elem["studentId"]}>
                            ${elem["studentFullName"]}
                        </td>
                        <td id = "${elem["group"]["groupId"]}">
                            ${elem["group"]["groupName"]}
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
}

$("#find_students").on("click", function () {
    document.getElementById("students_not_in_order").innerHTML = "";
    students = students.filter(x => x.pinned != undefined || x.pinned !== true);
    $.ajax({
        type: "GET",
        url: "/studentflow/notinorder/" + String($("#current_order").prop("order_id")),
        success: function (studentsJSON) {
            $.each(studentsJSON, function (i, val) { 
                if (!isStudentPinned(Number(val["studentId"]))) {
                    addStudentToPool(val);
                }
            });
        }
    });
});

// id - айди студента
// groupNow - айди текущей группы студента
// groupAfter - айди группы после регистрации в приказе
function addOrUpdateStudentPinned(studentRecordObject) {
    var found = studentPinned.find(x => x.id == studentRecordObject.id);
    if (found == undefined) {
        studentPinned.push(studentRecordObject);
    }
    else {
        if ("groupNow" in studentRecordObject) {
            found.groupNow = studentRecordObject.groupNow;
        }
        if ("groupAfter" in studentRecordObject) {
            found.groupAfter = studentRecordObject.groupAfter;
        }
    }
}
function removeStudentById(id) {
    var toDelete = studentPinned.find(x => x.id == id);
    if (toDelete != undefined) {
        studentPinned.splice(studentPinned.indexOf(toDelete), 1);
    }
}
function isStudentPinned(id = -1) {
    var studentfound = students.find(x => x.id == id);
    if (studentfound == undefined) {
        return false;
    }
    else {
        return studentfound.pinned === true;
    }


}

function addStudentToPool(student) {
    student.pinned = false;
    $("#students_not_in_order").append(
        `
        <tr id = "${student["studentId"] + identityExcludedPostfix}"> 
            <td>
                ${student["studentFullName"]}
            </td>
            <td>
                ${student["group"]["groupName"]}
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
    if (student != null) {
        var groupPolicy = $("#current_order").prop("group_behaviour")
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
            findGroupsAndSetAutoComplete(student["studentId"]);
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
        else if (groupPolicy === "NoChange") {
            $("#students_in_order").append(
                `
                    <tr id="${student["studentId"] + identityIncludedPostfix}"> 
                        <td>
                            ${student["studentFullName"]}
                        </td>
                        <td>
                            ${student["group"]["groupName"]}
                        </td>
                        <td>
                            <button id="${student["studentId"] + excludePostfix}">
                            -
                            </button>
                        </td>
                    </tr>
                    `);
        }
        document.getElementById(student["studentId"] + identityExcludedPostfix).innerHTML = "";
        $("#" + student["studentId"] + excludePostfix).on("click", function () { excludeStudent(student); });

    }
}

function excludeStudent(student) {

    var includedStudentHTML = document.getElementById(student["studentId"] + identityIncludedPostfix);
    if (student != null) {
        $("#students_not_in_order").append(
            `
            <tr id="${student["studentId"] + identityExcludedPostfix}"> 
                <td>
                    ${student["studentFullName"]}
                </td>
                <td>
                    ${student["group"]["groupName"]}
                </td>
                <td>
                    <button id="${student["studentId"] + includePostfix}">
                    +
                    </button>
                </td>

            </tr>
            `
        );
        $("#" + student["studentId"] + includePostfix).on("click", function () { includeStudent(student) });
        includedStudentHTML.innerHTML = "";

    }
}

function findGroupsAndSetAutoComplete(studentId) {
    var inputName = String(studentId) + studentPinnedGroupInputPostfix
    $("#" + inputName).on("keyup", function () {
        var searchText = $("#" + inputName).val()
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
                        event.preventDefault();
                    }
                });
            }
        });
    })
}

$("#save_changes").on("click", function () {
    $.ajax({
        type: "POST",
        url: "/flow/save",
        data: JSON.stringify(studentPinned.map(
            (x) => ({
                studentId: x.id,
                groupFromId: x.groupNow,
                groupToId: x.groupAfter,
                orderId: selectedOrder.id
            }))),
        dataType: "json",
        success: function (response) {

        }
    });
});



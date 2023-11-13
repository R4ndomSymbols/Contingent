var orderSelectionSource = [];
var orderTypesForDisplayingNames = [];
var selectedOrder = undefined;
var includePostfix = "_inc";
var excludePostfix = "_ex";
var identityIncludedPostfix = "_inc_id";
var identityExcludedPostfix = "_ex_id";
var currentGroupFlag = false;
var studentPinnedGroupInputPostfix = "_n_group"
// здесь хранятся все студенты, добавленные в приказ на view
var studentPinned = [];
//
$(document).ready(function () {
    $.ajax({
        type: "GET",
        url: "/orders/types",
        dataType: "json",
        success: function (response) {
            for (i in response) {
                if (Number(response[i].id) > 0) {
                    orderTypesForDisplayingNames.push({
                        id: response[i].id,
                        typeName: response[i].name,
                        groupFlag: response[i].groupFlag,
                    });
                }
            }
        }
    });

});

$("#current_order").on("keyup", function () {
    var searchText = String($("#current_order").val());
    if (searchText.length > 2) {
        $.ajax({
            type: "GET",
            url: "/orders/filter/" + String($("#current_order").val()),
            dataType: "json",
            success: function (response) {
                orderSelectionSource = [];
                for (i in response) {
                    if (response[i].id > 0) {
                        orderSelectionSource.push({
                            id: response[i].id,
                            orderName: response[i].orderName,
                            orderTypeId: response[i].orderType,
                        });
                    }
                }
                for (i in orderSelectionSource) {
                    orderSelectionSource[i].orderName =
                        orderSelectionSource[i].orderName + " | " + orderTypesForDisplayingNames.find((x) => x.id == orderSelectionSource[i].orderTypeId).typeName;
                }
                $("#current_order").autocomplete({
                    source: orderSelectionSource.map(x => x.orderName),
                });
            }
        });
    }
});
$("#current_order").on("change", function () {
    var orderSelected = String($("#current_order").val());
    selectedOrder = orderSelectionSource.find(x => x.orderName == orderSelected);
    if (selectedOrder != undefined) {
        $.ajax({
            type: "GET",
            url: "/flow/pinned/" + String(selectedOrder.id),
            dataType: "json",
            success: function (response) {
                document.getElementById("students_in_order").innerHTML = "";
                document.getElementById("students_not_in_order").innerHTML = "";
                for (i in response) {
                    $("#students_in_order").append(
                        `
                        <tr> 
                            <td>
                                ${response[i].name}
                            </td>
                            <td>
                                ${response[i].groupName}
                            </td>
                            <td>
                                Нет
                            </td>
                        </tr>
                        `
                    );
                }
                $("#fullname_filter").attr("disabled", false);
                $("#group_filter").attr("disabled", false);
                $("#find_students").attr("disabled", false);
            }
        });
    }
    else {
        $("#fullname_filter").attr("disabled", true);
        $("#group_filter").attr("disabled", true);
        $("#find_students").attr("disabled", true);
    }


});

$("#find_students").on("click", function () {
    document.getElementById("students_not_in_order").innerHTML = "";

    $.ajax({
        type: "POST",
        url: "flow/filter",
        data: JSON.stringify(
            {
                orderTypeId: selectedOrder.orderTypeId,
                orderId: selectedOrder.id,
                searchName: String($("#group_filter").val()),
                searchGroupName: String($("#fullname_filter").val()),
            }),
        dataType: "json",
        success: function (students) {
            currentGroupFlag = orderTypesForDisplayingNames.find(x => x.id == selectedOrder.orderTypeId).groupFlag;
            for (i in students) {
                var student = students[i];
                if (!checkStudentPin(Number(student.id))) {
                    addStudentToPool(student.id, student.groupId, student.name, student.groupName);
                }
            }
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
function checkStudentPin(id = -1) {
    return studentPinned.find(x => x.id == id) != undefined;
}

function addStudentToPool(id = "", groupId, name, group) {
    $("#students_not_in_order").append(
        `
        <tr id = "${id + identityExcludedPostfix}"> 
            <td>
                ${name}
            </td>
            <td>
                ${group}
            </td>
            <td>
                <button id = "${id + includePostfix}">
                +
                </button>
            </td>
        </tr>
        `
    );
    $("#" + id + includePostfix).on("click", function () { includeStudent(id, groupId, name, group) });
}



function includeStudent(id = "", groupId, name, group) {
    var student = document.getElementById(id + identityExcludedPostfix);
    if (student != null) {
        function first() {
            if (currentGroupFlag) {
                $("#students_in_order").append(
                    `
                    <tr id="${id + identityIncludedPostfix}"> 
                        <td>
                            ${name}
                        </td>
                        <td>
                            <input id = "${id + studentPinnedGroupInputPostfix}"/>
                        </td>
                        <td>
                            <button id="${id + excludePostfix}">
                            -
                            </button>
                        </td>
                    </tr>
                    `);
                findGroupsAndSetAutoComplete(id);
            }
            else {
                $("#students_in_order").append(
                    `
                    <tr id="${id + identityIncludedPostfix}"> 
                        <td>
                            ${name}
                        </td>
                        <td>
                            ${group}
                        </td>
                        <td>
                            <button id="${id + excludePostfix}">
                            -
                            </button>
                        </td>
                    </tr>
                    `);
            }
            second();
        }
        function second() {
            $("#" + id + excludePostfix).on("click", function () { excludeStudent(id, groupId, name, group); });
            addOrUpdateStudentPinned({ id: id, groupNow: groupId });
            third();
        }
        function third() {
            student.remove();
        }
        first();
    }
}

function excludeStudent(id = "", groupId, name, group) {

    var student = document.getElementById(id + identityIncludedPostfix);
    if (student != null) {
        function first() {
            $("#students_not_in_order").append(
                `
                <tr id="${id + identityExcludedPostfix}"> 
                    <td>
                        ${name}
                    </td>
                    <td>
                        ${group}
                    </td>
                    <td>
                        <button id="${id + includePostfix}">
                        +
                        </button>
                    </td>

                </tr>
                `
            );
            second()
        }
        function second() {
            $("#" + id + includePostfix).on("click", function () { includeStudent(id, groupId, name, group) });
            removeStudentById(Number(id));
            third();
        }
        function third() {
            student.remove();
        }
        first();

    }
}

function findGroupsAndSetAutoComplete(studentId) {
    var inputName = String(studentId) + studentPinnedGroupInputPostfix
    $("#" + inputName).on("keyup", function () {

        var searchText = $("#" + inputName).val()
        if (String(searchText).length >= 2) {

            var groupsAvailable = [];
            $.ajax({
                type: "GET",
                url: "groups/find/" + searchText,
                dataType: "json",
                success: function (response) {
                    for (i in response) {
                        groupsAvailable.push(
                            {
                                name: response[i].name,
                                id: response[i].id
                            }
                        )
                    }
                    //$("#" + studentId + studentPinnedGroupInputPostfix).autocomplete("destroy");
                    $("#" + inputName).autocomplete({
                        source: groupsAvailable.map(x => x.name),
                        change: function (event, ui) {
                            if (ui != null) {
                                addOrUpdateStudentPinned(
                                    {
                                        id: studentId,
                                        groupAfter: groupsAvailable.find(x => x.name == ui.item.label).id,
                                    }
                                );
                            }
                        }
                    });
                }
            });
        }
    });
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



import { Utilities } from "../site.js";
const exPostfix = "_ex";
const rowPostfix = "_row";
let utils = new Utilities();
let moves = undefined;
let info = undefined;
let table = undefined;
let isClosedForDeletion = undefined;
let displayType = undefined;
init();


function init() {
    info = $("#order_info");
    table = $("#students_in_order");
    isClosedForDeletion = info.attr("is_closed_for_deletion") === "true";
    displayType = info.attr("display_type");
    getHistory();
    if (isClosedForDeletion) {
        utils.disableField("delete_history")
    }
    $("#delete_history").on("click", function () {
        if (confirm("Вы точно хотите удалить историю приказа (удалит ВСЮ последующую за данным приказом историю ВСЕХ студентов в приказе)?")) {
            $.ajax({
                type: "DELETE",
                url: "/studentflow/revert/" + String($("#order_info").attr("order_id")),
                beforeSend: utils.setAuthHeader,
                success: function (response) {
                    getHistory();
                    alert("История удалена")
                },
                error: function (xhr, b, c) {
                    utils.readAndSetErrors(xhr)
                }
            });
        }
    });
}

function getHistory() {
    moves = [];
    table.empty();
    $.ajax({
        type: "GET",
        url: "/orders/history/" + String(info.attr("order_id")),
        contentType: "application/json",
        dataType: "json",
        beforeSend: utils.setAuthHeader,
        success: function (response) {
            var tableHtml = "";
            moves = response;
            $.each(response, function (index, student) {
                const excludeStudentUrl = "/studentflow/revert/"
                    + String(info.attr("order_id"))
                    + "/" + String(student.studentId);
                const buttonId = String(student.studentId) + exPostfix;
                const rowId = String(student.studentId) + rowPostfix;

                if (displayType === "MustChange") {
                    table.append(
                        `
                        <tr id = "${rowId}">
                            <td>${student.gradeBookNumber}</td>
                            <td>${student.studentFullName}</td>
                            <td>${student.groupNameFrom}</td>
                            <td>${student.groupNameTo}</td>
                            <td>
                                <button id = "${buttonId}" class="standard-button-sec">
                                    Исключить
                                </button>
                            </td>
                        </tr>
                    `);
                }
                else if (displayType === "WipeOut") {
                    table.append(
                        `
                        <tr id = "${rowId}">
                            <td>${student.gradeBookNumber}</td>
                            <td>${student.studentFullName}</td>
                            <td>${student.groupNameFrom}</td>
                            <td>
                                <button id = "${buttonId}" class="standard-button-sec">
                                    Исключить
                                </button>
                            </td>
                        </tr>
                    `);
                }
                else if (displayType === "PeriodInput") {
                    table.append(
                        `
                        <tr id = "${rowId}">
                            <td>${student.gradeBookNumber}</td>
                            <td>${student.studentFullName}</td>
                            <td>${student.groupNameFrom}</td>
                            <td>${student.startDate}</td>
                            <td>${student.endDate}</td>
                            <td>
                                <button id = "${buttonId}" class="standard-button-sec">
                                    Исключить
                                </button>
                            </td>
                        </tr>
                    `);
                }






                if (isClosedForDeletion) {
                    utils.disableField(buttonId)
                }
                $("#" + buttonId).on("click", function () {
                    if (confirm("Вы точно хотите исключить студента из приказа?")) {
                        $.ajax({
                            type: "DELETE",
                            url: excludeStudentUrl,
                            beforeSend: utils.setAuthHeader,
                            success: function (response) {
                                alert("Студент успешно исключен из приказа")
                                $("#" + rowId).remove();
                            },
                            error: function (xhr, a, b) {
                                utils.readAndSetErrors(xhr)
                            }
                        });
                    }
                });
            });
        }
    });
}

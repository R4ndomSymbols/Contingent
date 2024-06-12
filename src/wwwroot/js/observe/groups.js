import { Utilities } from "../site.js";
let utils = new Utilities();
let dateInput;
let searchButton;
let historyTable;

init();

function init() {
    dateInput = $("#date_input")
    searchButton = $("#search_students_button")
    historyTable = $("#group_history")
    searchButton.on("click", function () {
        $.ajax({
            type: "POST",
            url: "/groups/history",
            data: getDataForSearchHistory(),
            contentType: "application/json",
            beforeSend: utils.setAuthHeader,
            success: function (response) {
                resetHistory();
                $.each(response, function (indexInArray, valueOfElement) {
                    appendHistoryRecord(valueOfElement)
                });

            },
            error: function (jqXHR, status, error) {
                utils.readAndSetErrors(jqXHR)
                resetHistory();
            }
        });
    });
}

function resetHistory() {
    historyTable.empty();
}
function appendHistoryRecord(record) {
    historyTable.append(
        `
            <tr>
                <td>
                <a href="${record.studentLink}">
                ${record.studentGradeBookNumber}
                </a>
                </td>
                <td>${record.studentName}</td>
                <td>
                <a href="${record.orderLink}">
                ${record.orderTypeName}
                </a>
                </td>
                <td>${record.orderEffectiveDate}</td>
            </tr>
        `
    );
}

function getDataForSearchHistory() {
    return JSON.stringify(
        {
            Id: Number($("#info").attr("group_id")),
            OnDate: dateInput.val()
        }
    )
}



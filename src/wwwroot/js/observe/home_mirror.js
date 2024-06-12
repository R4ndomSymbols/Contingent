import { Utilities } from "../site.js";
let historyTable = null;
let utils = new Utilities();

init();

function init() {
    getHistory();
}


function getHistory() {
    historyTable = $("#last_records");
    $.ajax({
        type: "GET",
        url: "/home/last",
        contentType: "application/json",
        beforeSend: utils.setAuthHeader,
        success: function (response) {
            $.each(response, function (indexInArray, valueOfElement) {
                appendHistoryRecord(valueOfElement)
            });
        }
    });
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
                    ${record.orderOrgId}  ${record.orderTypeName}
                    </a>
                </td>
            </tr>
        `
    );
}

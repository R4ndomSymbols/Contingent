import { Utilities } from "../site.js";
let utils = new Utilities();

init();

function init() {
    $.ajax({
        type: "GET",
        url: "/studentflow/history/" + String($("#about_student").attr("student_id")),
        contentType: "application/json",
        beforeSend: utils.setAuthHeader,
        success: function (response) {
            var tableContent = "";
            $.each(response, function (index, move) {
                tableContent += `
                 <tr>
                    <td>${move.orderRussianTypeName}</td>
                    <td>${move.orderOrgId}</td>
                    <td>${move.orderSpecifiedDate}</td>
                    <td>${move.groupNameFrom}</td>
                    <td>${move.groupNameTo}</td>
                    <td>${move.startDate}</td>
                    <td>${move.endDate}</td>
                 </tr>
                 `
            });
            document.getElementById("history").innerHTML = tableContent;
        }
    });
}



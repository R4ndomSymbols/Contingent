$(document).ready(function () {
    $.ajax({
        type: "GET",
        url: "/studentflow/studentsByOrder/" + String($("#order_info").attr("order_id")),
        dataType: "JSON",
        success: function (response) {
            var tableHtml = "";
            $.each(response, function (index, student) { 
                tableHtml += 
                `
                    <tr>
                        <td>${student.gradeBookNumber}</td>
                        <td>${student.studentFullName}</td>
                        <td>${student.groupNameFrom}</td>
                        <td>${student.groupNameTo}</td>
                    </tr>
                `
            });
            document.getElementById("students_in_order").innerHTML = tableHtml;
        }
    });
});
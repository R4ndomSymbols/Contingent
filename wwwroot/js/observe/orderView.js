$(document).ready(function () {
    getHistory();
});

$("#delete_history").on("click", function () {
    if (confirm("Вы точно хотите удалить историю приказа (удалит ВСЮ последующую историю ВСЕХ студентов в приказе)?")){
        $.ajax({
            type: "DELETE",
            url: "/studentflow/revert/" + String($("#order_info").attr("order_id")),
            success: function (response) {
                getHistory();
                alert("История удалена")
            },
            error: function(a,b,c){
                alert("Произошла ошибка при удалении")
            }
        });
    }
});

function revertAllHistory() 
{  

}

function getHistory(){
    $.ajax({
        type: "GET",
        url: "/orders/history/" + String($("#order_info").attr("order_id")),
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
}

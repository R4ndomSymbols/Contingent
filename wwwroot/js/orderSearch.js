
$(document).ready(function () {
    $("#find_orders").on("click", function () {
        $.ajax({
            type: "POST",
            url: "/orders/search/find",
            data: JSON.stringify(
                {
                    SearchText: "void"
                }
            ),
            dataType: "JSON",
            success: function (response) {
                table = document.getElementById("search_results");
                table.innerHTML = "";
                $.each(response, function (index, value) {
                    table.innerHTML +=
                        ` <tr>
                            <th scope="row">${value.orderOrgId}</th>
                            <td>${value.orderFullName}</td>
                            <td>${value.orderSpecifiedDate}</td>
                            <td>${value.orderEffectiveDate}</td>
                            <td>
                                <div class="row">
                                    <a href="${value.orderModifyLink}">Изменить</a>
                                </div>
                            </td>
                        </tr>
                        `
                });
            }
        });
    });
});

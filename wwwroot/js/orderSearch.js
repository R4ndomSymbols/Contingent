
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
                    card =
                        ` <tr>
                            <th scope="row">${value.orderOrgId}</th>
                            <td>${value.orderFullName}</td>
                            <td>${value.orderSpecifiedDate}</td>
                            <td>${value.orderEffectiveDate}</td>
                            <td>
                        `;
                    card+= `
                    <div class="row">
                            <a href="${value.orderViewLink}">Детали</a>
                    </div>
                    `
                    if (value.orderModifyLink != null){
                        card += 
                        `
                        <div class="row">
                                <a href="${value.orderModifyLink}">Изменить</a>
                        </div>
                        `
                    }
                    if (value.orderFlowLink != null){
                        card += 
                        `
                        <div class="row">
                                <a href="${value.orderFlowLink}">Движение</a>
                        </div>
                        `
                    }
                    if (value.orderCloseLink != null){
                        card += 
                        `
                        <div class="row">
                                <a href="${value.orderCloseLink}">Закрыть</a>
                        </div>
                        `
                    }
                    table.innerHTML += card
                });
            }
        });
    });
});

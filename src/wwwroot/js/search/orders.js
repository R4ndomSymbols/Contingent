import { Utilities } from "../site.js";
let utils = new Utilities();
let table = undefined;

init();

function init() {
    table = $("#search_results");
    $("#find_orders").on("click", function () {
        $.ajax({
            type: "POST",
            url: "/orders/search/find",
            data: getSearchRequestData(),
            contentType: "application/json",
            beforeSend: utils.setAuthHeader,
            success: function (response) {
                table.empty()
                $.each(response, function (index, value) {
                    let card =
                        ` <tr>
                            <th scope="row">${value.orderOrgId}</th>
                            <td>${value.orderFullName}</td>
                            <td>${value.orderSpecifiedDate}</td>
                            <td>${value.orderEffectiveDate}</td>
                            <td>
                        `;
                    card += `
                    <div class="d-flex flex-row">
                            <a class="mx-2" href="${value.orderViewLink}">Детали</a>
                    `
                    if (value.orderModifyLink != null) {
                        card +=
                            `
                        <a class="mx-2" href="${value.orderModifyLink}">Изменить</a>
                        `
                    }
                    if (value.orderFlowLink != null) {
                        card +=
                            `
                            <a class="mx-2" href="${value.orderFlowLink}">Движение</a>
                        `
                    }
                    if (value.orderCloseLink != null) {
                        card +=
                            `
                            <a class="mx-2" href="${value.orderCloseLink}">Закрыть</a>
                        `
                    }
                    card += "</div>"
                    table.append(card)
                });
            }
        });
    });
}

function getSearchRequestData() {
    let type = Number($("#order_type").val())
    let year = Number($("#order_date").val())
    return JSON.stringify(
        {
            SearchText: $("#order_name").val(),
            Year: year === -1 ? null : year,
            Type: type === -1 ? null : type,
            PageSize: Number($(`input[name = "record_count"]:checked`).val())
        }
    )
}
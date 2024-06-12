import { Utilities } from "../site.js";
let utils = new Utilities();
let id = null;

init();

function init() {
    id = $("#OrderOrgId").attr("orgid");
    if (id === "" || id === undefined || id == null || id === "-1") {
        $(".identity_dependency").focusout(
            function () {
                utils.registerScheduledQuery(
                    () => updateIdentity()
                );
            }
        );
    }
    else {
        utils.disableField("EffectiveDate");
        utils.disableField("identity_dependency", utils.SELECTOR_CLASS)
    }
}


function updateIdentity() {

    $.ajax({
        type: "POST",
        url: "/orders/generateIdentity",
        data: JSON.stringify(
            {
                SpecifiedDate: $("#SpecifiedDate").val(),
                EffectiveDate: $("#EffectiveDate").val(),
                OrderType: Number($("#OrderType").val()),
                OrderDisplayedName: "test",
                OrderDescription: $("#OrderDescription").val() == "" ? null : $("#OrderDescription").val()
            }
        ),
        contentType: "application/json",
        beforeSend: utils.setAuthHeader,
        success: function (response) {
            $("#order_name").text("Приказ " + String(response["orderOrgId"]));
        },
        error: function (xhr, textStatus, errThrown) {
            utils.readAndSetErrors(xhr);
        }
    });

}

$("#save").click(function () {
    $.ajax({
        type: "POST",
        url: "/orders/save",
        data: JSON.stringify(
            {
                Id: id,
                SpecifiedDate: $("#SpecifiedDate").val(),
                EffectiveDate: $("#EffectiveDate").val(),
                OrderType: Number($("#OrderType").val()),
                OrderDisplayedName: $("#OrderDisplayedName").val(),
                OrderDescription: $("#OrderDescription").val()

            }),
        contentType: "application/json",
        beforeSend: utils.setAuthHeader,
        success: function (response) {
            id = response["orderId"];
            utils.notifySuccess();
        },
        error: function (xhr, textStatus, errThrown) {
            utils.readAndSetErrors(xhr);
        }
    });
});




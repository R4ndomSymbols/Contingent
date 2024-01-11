$(document).ready(function () {
    $(".identity_dependency").blur(
        function(){
            updateIdentity();
        }
    );
});

function updateIdentity(){

    $.ajax({
        type: "POST",
        url: "/orders/generateIdentity",
        data: JSON.stringify(
            {
                SpecifiedDate: $("#SpecifiedDate").val(),
                EffectiveDate: $("#EffectiveDate").val(),
                OrderType: Number($("#OrderType").val()),
            }
        ),
        dataType: "JSON",
        success: function (response) {
            document.getElementById("OrderStringId").innerText = response["orderStringId"];
        }
    });
}

$("#save").click(function () {
    $.ajax({
        type: "POST",
        url: "/orders/save",
        data: JSON.stringify(
        {
            SpecifiedDate: $("#SpecifiedDate").val(),
            EffectiveDate: $("#EffectiveDate").val(),
            OrderType: Number($("#OrderType").val()),
            OrderDisplayedName: $("#OrderDisplayedName").val(),
            OrderDescription: $("#OrderDescription").val()

        }),
        dataType: "JSON",
        success: function (response) {
            var orderId = response["orderId"];

            if (orderId != undefined) {
                $("#OrderId").val(orderId);
            }
            else {
                setErrors(response);
            }
        }
    });
});




import { Utilities } from "../site.js";
let utils = new Utilities();
init();

function init() {
    $("#import").on("click", function () {
        sendFile();
    });
}

function sendFile() {
    readBinaryFile(
        function (file) {
            $.ajax({
                type: "POST",
                url: "/import/upload/" + String($("#import_type").val()),
                data: file,
                contentType: "application/octet-stream",
                processData: false,
                beforeSend: utils.setAuthHeader,
                success: function (response) {
                    utils.notifySuccess();
                },
                error: function (xhr, a, b) {
                    utils.readAndSetErrors(xhr)
                }
            });
        }
    );

}

function readBinaryFile(callback) {
    let reader = new FileReader();
    reader.onload = function () {
        let buffer = this.result;
        let array = new Uint8Array(buffer);
        console.log(array);
        callback(array)
    };
    reader.readAsArrayBuffer(document.getElementById("input_file").files[0]);
}



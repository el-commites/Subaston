/* REPORTE */
document.addEventListener("DOMContentLoaded", function () {

    document.querySelectorAll("[id^='modalReporte-'] form").forEach(function (form) {

        form.addEventListener("submit", function (e) {
            e.preventDefault();

            Swal.fire({
                title: "¿Estás seguro?",
                text: "Si mandas un reporte sin razón válida, podrías recibir un llamado de atención.",
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: "#00B8D9",
                cancelButtonColor: "#3a1a6e",
                confirmButtonText: "Sí, enviar reporte",
                cancelButtonText: "Cancelar",
                background: "#1e0a3c",
                color: "#ffffff",
                didOpen: (popup) => {
                    popup.style.border = "1px solid #3a1a6e";
                }
            }).then((result) => {
                if (result.isConfirmed) {
                    form.submit();
                }
            });
        });
    });

});
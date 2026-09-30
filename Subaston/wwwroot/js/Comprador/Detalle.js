/* REPORTE */
document.addEventListener("DOMContentLoaded", function () {

    var formReporte = document.querySelector("form.form-reporte");
    if (formReporte) {
        formReporte.addEventListener("submit", function (e) {
            e.preventDefault();
            var form = this;

            Swal.fire({
                title: "¿Estás seguro?",
                text: "Si mandas un reporte sin razón válida, podrías recibir un llamado de atención.",
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: "#00B8D9",
                cancelButtonColor: "#3a1a6e",
                confirmButtonText: "Sí, reportar",
                cancelButtonText: "Cancelar",
                background: "#1e0a3c",
                color: "#ffffff",
                didOpen: (popup) => {
                    popup.style.border = "1px solid #3a1a6e";
                }
            }).then((result) => {
                if (result.isConfirmed) form.submit();
            });
        });
    }

    /* PUJA */
    var formPuja = document.querySelector("form.form-puja");
    if (formPuja) {
        formPuja.addEventListener("submit", function (e) {
            e.preventDefault();
            var form = this;
            var pujaActual = parseFloat(form.dataset.pujaActual) || 0;
            var inputPuja = form.querySelector("input[name='nuevaPuja']");
            var valorIngresado = parseFloat(inputPuja.value);

            if (isNaN(valorIngresado) || valorIngresado <= pujaActual) {
                Swal.fire({
                    title: "Puja no válida",
                    text: "Tu puja debe ser mayor a la puja actual de $" + pujaActual + ".",
                    icon: "error",
                    confirmButtonColor: "#00B8D9",
                    background: "#1e0a3c",
                    color: "#ffffff",
                    didOpen: (popup) => {
                        popup.style.border = "1px solid #3a1a6e";
                    }
                });
                return;
            }

            Swal.fire({
                title: "¿Confirmas tu puja?",
                text: "Una vez enviada no podrás revertirla.",
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: "#00B8D9",
                cancelButtonColor: "#3a1a6e",
                confirmButtonText: "Sí, pujar",
                cancelButtonText: "Cancelar",
                reverseButtons: true,
                background: "#1e0a3c",
                color: "#ffffff",
                didOpen: (popup) => {
                    popup.style.border = "1px solid #3a1a6e";
                }
            }).then((result) => {
                if (result.isConfirmed) {
                    Swal.fire({
                        toast: true,
                        position: "top-end",
                        icon: "success",
                        title: "¡Puja enviada!",
                        showConfirmButton: false,
                        timer: 1500,
                        timerProgressBar: true,
                        background: "#1e0a3c",
                        color: "#ffffff",
                        didOpen: (popup) => {
                            popup.style.border = "1px solid #3a1a6e";
                        }
                    }).then(() => {
                        form.submit();
                    });
                } else if (result.dismiss === Swal.DismissReason.cancel) {
                    Swal.fire({
                        toast: true,
                        position: "top-end",
                        icon: "error",
                        title: "Puja cancelada",
                        showConfirmButton: false,
                        timer: 2000,
                        timerProgressBar: true,
                        background: "#1e0a3c",
                        color: "#ffffff",
                        didOpen: (popup) => {
                            popup.style.border = "1px solid #3a1a6e";
                        }
                    });
                }
            });
        });
    }

});
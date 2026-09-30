/* CONFIRMAR PAGO */
document.addEventListener("DOMContentLoaded", function () {

    var botonesPagar = document.querySelectorAll(".btn-pagar");

    botonesPagar.forEach(function (btn) {
        btn.addEventListener("click", function (e) {
            e.preventDefault();
            var url = this.href;

            Swal.fire({
                toast: true,
                position: "top-end",
                title: "¿Confirmas el pago?",
                text: "Seras redirigido a Mercado Pago para completar la compra.",
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: "#00B8D9",
                cancelButtonColor: "#3a1a6e",
                confirmButtonText: "Si, ir a pagar",
                cancelButtonText: "Cancelar",
                background: "#1e0a3c",
                color: "#ffffff",
                didOpen: (popup) => {
                    popup.style.border = "1px solid #3a1a6e";
                }
            }).then((result) => {
                if (result.isConfirmed) {
                    window.location.href = url;
                }
            });
        });
    });

});

document.addEventListener("DOMContentLoaded", () => {

    document.querySelectorAll(".js-confirm").forEach(btn => {

        btn.addEventListener("click", function (e) {

            e.preventDefault();

            const url = this.href;

            Swal.fire({
                title: this.dataset.confirmTitle || "¿Estás seguro?",
                text: this.dataset.confirmText || "",
                icon: "warning",

                showCancelButton: true,

                confirmButtonText: this.dataset.confirmOk || "Sí",
                cancelButtonText: "Cancelar",

                position: "top-end",
                toast: true,

                width: "300px",

                background: "#17082e",
                color: "#fff",

                confirmButtonColor: "#dc3545",
                cancelButtonColor: "#6c757d",

                showClass: {
                    popup: "animate__animated animate__fadeInRight"
                },
                hideClass: {
                    popup: "animate__animated animate__fadeOutRight"
                }

            }).then((result) => {

                if (result.isConfirmed) {
                    window.location.href = url;
                }

            });

        });

    });

});
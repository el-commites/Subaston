$(function () {
    if (window.toastr) {
        toastr.options = {
            closeButton: true,
            progressBar: true,
            positionClass: "toast-top-right",
            timeOut: 4500
        };

        const mensajes = window.subastonTempData || {};
        if (mensajes.correcto) toastr.success(mensajes.correcto);
        if (mensajes.error) toastr.error(mensajes.error);
        if (mensajes.info) toastr.info(mensajes.info);
    }

    $(".js-datatable").each(function () {
        const $tabla = $(this);
        if ($.fn.DataTable && !$.fn.DataTable.isDataTable(this)) {
            new DataTable(this, {
                responsive: true,
                pageLength: 10,
                layout: {
                    topStart: $tabla.data("export") === true || $tabla.data("export") === "true"
                        ? { buttons: ["copy", "excel", "pdf", "print"] }
                        : "pageLength",
                    topEnd: "search",
                    bottomStart: "info",
                    bottomEnd: "paging"
                },
                language: {
                    url: "https://cdn.datatables.net/plug-ins/2.0.8/i18n/es-MX.json"
                }
            });
        }
    });

    $(document).on("click", ".js-confirm", function (event) {
        if (!window.Swal) return;

        event.preventDefault();
        const elemento = this;
        Swal.fire({
            title: elemento.dataset.confirmTitle || "Confirmar accion",
            text: elemento.dataset.confirmText || "Esta accion no se puede deshacer.",
            icon: elemento.dataset.confirmIcon || "warning",
            showCancelButton: true,
            confirmButtonText: elemento.dataset.confirmOk || "Si, continuar",
            cancelButtonText: "Cancelar"
        }).then((result) => {
            if (result.isConfirmed) {
                if (elemento.tagName === "A") {
                    window.location.href = elemento.href;
                    return;
                }

                const form = elemento.closest("form");
                if (form) {
                    // Usar submit nativo para evitar que el handler de jQuery lo intercepte de nuevo
                    form.dataset.confirmed = "true";
                    HTMLFormElement.prototype.submit.call(form);
                }
            }
        });
    });

    $("form").on("submit", function () {
        const $form = $(this);
        const botonConfirmacion = $form.find(".js-confirm[type='submit']").first();
        if (botonConfirmacion.length && this.dataset.confirmed !== "true") {
            botonConfirmacion.trigger("click");
            return false;
        }

        if ($form.valid && !$form.valid()) {
            if (window.toastr) toastr.error("Revisa los campos marcados antes de continuar.");
            return false;
        }
    });
});
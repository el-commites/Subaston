document.addEventListener("DOMContentLoaded", () => {

    document.querySelectorAll(".btn-ban").forEach(btn => {

        btn.addEventListener("click", function (e) {

            e.preventDefault();

            const url = this.href;

            Swal.fire({
                title: '¿Banear usuario?',
                text: 'Se eliminará este usuario de la plataforma. Esta acción no se puede deshacer.',
                icon: 'warning',
                position: 'top-end',
                width: '300px',
                background: '#17082e',
                color: '#ffffff',
                showCancelButton: true,
                confirmButtonText: 'Sí, banear',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: '#d33',
                cancelButtonColor: '#3085d6',
                showClass: {
                    popup: 'animate__animated animate__fadeInRight'
                },
                hideClass: {
                    popup: 'animate__animated animate__fadeOutRight'
                }
            }).then((result) => {

                if (result.isConfirmed) {
                    window.location.href = url;
                }

            });

        });

    });

});

document.addEventListener("DOMContentLoaded", function () {

    if (window.productoCreado) {

        Swal.fire({
            toast: true,
            position: 'top-end',
            icon: 'success',
            title: 'Producto enviado a revision correctamente',
            showConfirmButton: false,
            timer: 4000,
            timerProgressBar: true,
            background: '#1a0933',
            color: 'white',
            didOpen: (toast) => {
                const bar = toast.querySelector('.swal2-timer-progress-bar');
                if (bar) bar.style.setProperty('background', 'white', 'important');
            },
            showClass: {
                popup: 'animate__animated animate__fadeInRight animate__faster'
            },
            hideClass: {
                popup: 'animate__animated animate__fadeOutRight animate__faster'
            }
        });

    }

});   
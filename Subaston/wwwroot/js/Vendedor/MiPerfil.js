function previewYSubir(input) {
    if (!input.files || !input.files[0]) return;
    const file = input.files[0];
    if (file.size > 2 * 1024 * 1024) {
        Swal.fire({
            icon: 'error',
            title: 'Oops...',
            text: 'La imagen no puede superar 2 MB.',
            background: '#1a0933',
            color: 'white',
            confirmButtonColor: '#00B8D9'
        });
        input.value = '';
        return;
    }
    const reader = new FileReader();
    reader.onload = function (e) {
        const contenedor = document.getElementById('foto-contenedor');
        contenedor.innerHTML = '<img id="preview-foto" src="' + e.target.result + '" style="width:100%;height:100%;object-fit:cover;" />';
    };
    reader.readAsDataURL(file);
    document.getElementById('form-foto').submit();
}

document.addEventListener("DOMContentLoaded", function () {

    if (window.perfilActualizado) {
        Swal.fire({
            toast: true,
            position: 'top-end',
            icon: 'success',
            title: 'Perfil actualizado correctamente',
            showConfirmButton: false,
            timer: 6000,
            timerProgressBar: true,
            didOpen: (toast) => {
                const bar = toast.querySelector('.swal2-timer-progress-bar');
                if (bar) bar.style.setProperty('background', 'white', 'important');
            },
            background: '#1a0933',
            color: 'white',
            showClass: { popup: 'animate__animated animate__fadeInRight animate__faster' },
            hideClass: { popup: 'animate__animated animate__fadeOutRight animate__faster' }
        });
    }

    if (window.perfilError) {
        Swal.fire({
            toast: true,
            position: 'top-end',
            icon: 'error',
            title: window.perfilError,
            showConfirmButton: false,
            timer: 4000,
            timerProgressBar: true,
            didOpen: (toast) => {
                const bar = toast.querySelector('.swal2-timer-progress-bar');
                if (bar) bar.style.setProperty('background', 'white', 'important');
            },
            background: '#1a0933',
            color: 'white',
            showClass: { popup: 'animate__animated animate__fadeInRight animate__faster' },
            hideClass: { popup: 'animate__animated animate__fadeOutRight animate__faster' }
        });
    }

    ['btn-verificacion', 'btn-verificacion-card'].forEach(function (id) {
        var btn = document.getElementById(id);
        if (btn) {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                var url = this.href;
                Swal.fire({
                    title: '¿Quieres cambiar tu contrasena?',
                    text: 'Seras redirigido al proceso de verificacion.',
                    icon: 'question',
                    showCancelButton: true,
                    confirmButtonText: 'Si, continuar',
                    cancelButtonText: 'No, cancelar',
                    reverseButtons: true,
                    background: '#1a0933',
                    color: 'white',
                    confirmButtonColor: '#00B8D9',
                    cancelButtonColor: '#ff0f0f'
                }).then((result) => {
                    if (result.isConfirmed) {
                        window.location.href = url;
                    }
                });
            });
        }
    });

    /* VALIDACION GUARDAR CAMBIOS */
    var formPerfil = document.querySelector("form[action*='ActualizarPerfil']");
    if (formPerfil) {
        var inputNombre = formPerfil.querySelector("input[name='nombre']");
        var inputApodo = formPerfil.querySelector("input[name='apodo']");

        var nombreOriginal = inputNombre ? inputNombre.value.trim() : '';
        var apodoOriginal = inputApodo ? inputApodo.value.trim() : '';

        formPerfil.addEventListener("submit", function (e) {
            var nombreActual = inputNombre ? inputNombre.value.trim() : '';
            var apodoActual = inputApodo ? inputApodo.value.trim() : '';

            if (nombreActual === nombreOriginal && apodoActual === apodoOriginal) {
                e.preventDefault();
                Swal.fire({
                    toast: true,
                    position: 'top-end',
                    icon: 'info',
                    title: 'No hay cambios que guardar',
                    text: 'El nombre y apodo son los mismos.',
                    showConfirmButton: false,
                    timer: 3500,
                    timerProgressBar: true,
                    background: '#1a0933',
                    color: 'white',
                    didOpen: (toast) => {
                        const bar = toast.querySelector('.swal2-timer-progress-bar');
                        if (bar) bar.style.setProperty('background', '#00B8D9', 'important');
                        toast.style.border = '1px solid rgba(0,184,217,0.3)';
                    },
                    showClass: { popup: 'animate__animated animate__fadeInRight animate__faster' },
                    hideClass: { popup: 'animate__animated animate__fadeOutRight animate__faster' }
                });
            }
        });
    }

});
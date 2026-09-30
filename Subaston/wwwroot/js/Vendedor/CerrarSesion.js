document.getElementById('btn-logout').addEventListener('click', function (e) {
    e.preventDefault();
    var url = this.href;
    Swal.fire({
        toast: true,
        position: 'top-start',
        title: '¿Cerrar tu sesión?',
        text: 'Se cerrara tu sesión actual.',
        icon: 'question',
        iconColor: '#00B8D9',
        showCancelButton: true,
        confirmButtonText: 'Si, salir',
        cancelButtonText: 'No, cancelar',
        reverseButtons: true,
        background: '#1a0933',
        color: 'white',
        confirmButtonColor: '#00B8D9',
        cancelButtonColor: '#ff0f0f',
        showClass: {
            popup: 'animate__animated animate__fadeInDown animate__faster'
        },
        hideClass: {
            popup: 'animate__animated animate__fadeOutUp animate__faster'
        },
        didOpen: (popup) => {
            popup.style.borderRadius = '16px';
            popup.style.border = '1px solid rgba(0,184,217,0.3)';
            popup.style.boxShadow = '0 0 20px rgba(0,184,217,0.15)';
            const btns = popup.querySelectorAll('button');
            btns.forEach(btn => {
                btn.style.borderRadius = '20px';
                btn.style.fontSize = '13px';
                btn.style.padding = '6px 18px';
            });
        }
    }).then(function (result) {
        if (result.isConfirmed) {
            window.location.href = url;
        }
    });
});
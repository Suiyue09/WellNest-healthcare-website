// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.notice.auto-dismiss').forEach((notice) => {
        window.setTimeout(() => {
            notice.classList.add('is-hidden');
            window.setTimeout(() => notice.remove(), 350);
        }, 3500);
    });
});

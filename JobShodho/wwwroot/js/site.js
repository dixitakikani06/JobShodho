document.addEventListener('DOMContentLoaded', function () {
    var toggle = document.getElementById('sidebarToggle');
    var sidebar = document.getElementById('appSidebar');
    if (toggle && sidebar) {
        toggle.addEventListener('click', function () {
            sidebar.classList.toggle('show');
        });
    }

    var currentPath = window.location.pathname.split('/')[1] || 'Dashboard';
    document.querySelectorAll('.sidebar .nav-link').forEach(function (link) {
        var linkController = (link.getAttribute('href') || '').split('/')[1];
        if (linkController && linkController.toLowerCase() === currentPath.toLowerCase()) {
            link.classList.add('active');
        }
    });
});

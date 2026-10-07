// Задание 5: AJAX «Отметить выполненной».
// Антифоргери-токен берём из <meta name="csrftoken"> и отправляем в заголовке X-CSRF-TOKEN.
(function () {
    'use strict';

    var meta = document.querySelector('meta[name="csrftoken"]');
    var csrfToken = meta ? meta.getAttribute('content') : '';

    function render(btn, isCompleted) {
        var card = btn.closest('.task-card');
        if (card) {
            card.classList.toggle('done', isCompleted);
            var badge = card.querySelector('.js-status');
            if (badge) {
                badge.textContent = isCompleted ? 'Выполнена' : 'В работе';
                badge.classList.toggle('text-bg-success', isCompleted);
                badge.classList.toggle('text-bg-warning', !isCompleted);
            }
        }
        var text = btn.querySelector('.js-toggle-text');
        if (text) text.textContent = isCompleted ? 'Вернуть в работу' : 'Отметить выполненной';
        var icon = btn.querySelector('i');
        if (icon) icon.className = 'bi ' + (isCompleted ? 'bi-arrow-counterclockwise' : 'bi-check2-circle');
    }

    document.querySelectorAll('.js-toggle').forEach(function (btn) {
        btn.addEventListener('click', function () {
            btn.disabled = true;
            fetch(btn.dataset.url, {
                method: 'POST',
                headers: { 'X-CSRF-TOKEN': csrfToken, 'X-Requested-With': 'XMLHttpRequest' },
                credentials: 'same-origin'
            })
                .then(function (resp) {
                    if (!resp.ok) throw new Error('HTTP ' + resp.status);
                    return resp.json();
                })
                .then(function (data) { render(btn, data.isCompleted); })
                .catch(function (err) { alert('Не удалось выполнить действие: ' + err.message); })
                .finally(function () { btn.disabled = false; });
        });
    });
})();

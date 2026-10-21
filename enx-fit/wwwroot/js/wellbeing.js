(() => {
    'use strict';

    document.querySelectorAll('[data-wb-open]').forEach(button => {
        button.addEventListener('click', () => {
            const dialog = document.getElementById(button.dataset.wbOpen);
            if (!dialog || typeof dialog.showModal !== 'function') return;
            dialog.showModal();
            dialog.dataset.openedBy = button.id || '';
        });
    });
    document.querySelectorAll('dialog').forEach(dialog => {
        dialog.querySelectorAll('[data-wb-close]').forEach(button => button.addEventListener('click', () => dialog.close()));
        dialog.addEventListener('click', event => {
            if (event.target !== dialog) return;
            const bounds = dialog.getBoundingClientRect();
            if (event.clientX < bounds.left || event.clientX > bounds.right || event.clientY < bounds.top || event.clientY > bounds.bottom) dialog.close();
        });
    });
    document.querySelectorAll('[data-wb-submit]').forEach(input => {
        input.addEventListener('change', () => input.form?.requestSubmit());
    });

    document.querySelectorAll('[data-rhythm]').forEach(root => {
        const svg = root.querySelector('.wb-path');
        svg.setAttribute('role', 'group');
        const points = Array.from(root.querySelectorAll('[data-path-point]'));
        let visible = points;
        const select = point => {
            points.forEach(item => item.setAttribute('aria-pressed', String(item === point)));
            root.querySelector('[data-path-readout]').textContent = `${point.dataset.date} · занятий: ${point.dataset.count}${point.dataset.rest === 'true' ? ' · выбран отдых' : ''}`;
            root.querySelector('[data-path-history]').href = point.dataset.historyUrl;
        };
        const render = days => {
            visible = points.slice(-days);
            const max = Math.max(3, ...visible.map(point => Number(point.dataset.rolling)));
            const positions = visible.map((point, index) => ({ point, x: 26 + index * 548 / (visible.length - 1), y: 152 - Number(point.dataset.rolling) * 112 / max }));
            points.forEach(point => {
                const hidden = !visible.includes(point);
                point.toggleAttribute('hidden', hidden);
                point.setAttribute('aria-hidden', String(hidden));
                point.setAttribute('tabindex', hidden ? '-1' : '0');
            });
            positions.forEach(({ point, x, y }) => {
                point.querySelectorAll('[data-path-hit],[data-path-dot]').forEach(circle => { circle.setAttribute('cx', String(x)); circle.setAttribute('cy', String(y)); });
                point.querySelector('[data-rest-dot]')?.setAttribute('cx', String(x));
                point.querySelector('[data-path-hit]').setAttribute('r', days === 10 ? '18' : '7');
            });
            const line = positions.map(({ x, y }, i) => `${i ? 'L' : 'M'}${x},${y}`).join(' ');
            root.querySelector('[data-path-line]').setAttribute('d', line);
            root.querySelector('[data-path-area]').setAttribute('d', `${line} L574,164 L26,164Z`);
            root.querySelector('.wb-axis').textContent = String(max);
            root.querySelector('[data-path-first]').textContent = visible[0].dataset.date;
            root.querySelectorAll('[data-path-range]').forEach(button => button.setAttribute('aria-pressed', String(Number(button.dataset.pathRange) === days)));
            select(visible.at(-1));
        };
        root.querySelectorAll('[data-path-range]').forEach(button => button.addEventListener('click', () => render(Number(button.dataset.pathRange))));
        // Choose the nearest date even when dense 30-day hit areas overlap.
        svg.addEventListener('click', event => {
            const bounds = svg.getBoundingClientRect();
            const x = (event.clientX - bounds.left) / bounds.width * 600;
            const index = Math.max(0, Math.min(visible.length - 1, Math.round((x - 26) / 548 * (visible.length - 1))));
            select(visible[index]);
        });
        points.forEach(point => point.addEventListener('keydown', event => {
            const index = visible.indexOf(point);
            if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); select(point); }
            if (event.key === 'ArrowRight' || event.key === 'ArrowLeft') {
                event.preventDefault();
                const target = visible[Math.max(0, Math.min(visible.length - 1, index + (event.key === 'ArrowRight' ? 1 : -1)))];
                target.focus(); select(target);
            }
        }));
        render(10);
    });

    document.querySelectorAll('[data-recap]').forEach(dialog => {
        const slides = Array.from(dialog.querySelectorAll('[data-recap-slide]'));
        const tracks = Array.from(dialog.querySelectorAll('.wb-story-tracks i'));
        const previous = dialog.querySelector('[data-recap-prev]');
        const next = dialog.querySelector('[data-recap-next]');
        let current = 0;
        const show = index => {
            current = index;
            slides.forEach((slide, i) => { slide.hidden = i !== index; });
            tracks.forEach((track, i) => track.classList.toggle('is-active', i <= index));
            dialog.querySelector('[data-recap-count]').textContent = `${index + 1} из ${slides.length}`;
            previous.disabled = index === 0;
            next.setAttribute('aria-label', index === slides.length - 1 ? 'Закончить просмотр' : 'Следующая страница');
        };
        previous.addEventListener('click', () => show(Math.max(0, current - 1)));
        next.addEventListener('click', () => current === slides.length - 1 ? dialog.close() : show(current + 1));
        dialog.addEventListener('close', () => show(0));
        dialog.addEventListener('keydown', event => {
            if (event.key === 'ArrowLeft' && current > 0) { event.preventDefault(); show(current - 1); }
            if (event.key === 'ArrowRight') { event.preventDefault(); current === slides.length - 1 ? dialog.close() : show(current + 1); }
        });
    });

    document.querySelectorAll('[data-progress-chart]').forEach(root => {
        const show = point => {
            const result = root.querySelector('[data-progress-result]');
            if (!result) return;
            root.querySelectorAll('[data-chart-point]').forEach(item => item.setAttribute('aria-pressed', String(item === point)));
            result.hidden = false;
            result.querySelector('span').textContent = point.dataset.detail;
            const link = result.querySelector('a');
            link.hidden = !point.dataset.workoutUrl;
            if (point.dataset.workoutUrl) link.href = point.dataset.workoutUrl;
        };
        root.querySelectorAll('[data-chart-point]').forEach(point => {
            point.addEventListener('click', () => show(point));
            point.addEventListener('keydown', event => {
                if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); show(point); }
            });
        });
    });
})();

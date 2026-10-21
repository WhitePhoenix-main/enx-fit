// One active-session presentation for empty, template, program and supervised workouts.
// training.js remains the owner of persistence and deadlines.
(() => {
    'use strict';
    const root = document.querySelector('[data-workout-session]');
    if (!root) return;
    const libraryRoot = document.getElementById('workout-library'), library = libraryRoot.exerciseLibrary;
    const pickerDialog = document.getElementById('exercise-dialog');
    const restDialog = document.getElementById('quick-rest-dialog');
    const key = `enix-quick-open:${root.dataset.viewer}:${root.dataset.sessionId}`;
    let state = JSON.parse(document.getElementById('workout-execution-state').textContent);
    let openId, nextForm = null, adding = false;
    try { openId = localStorage.getItem(key); } catch { /* A presentation preference only. */ }
    const forms = host => [...host.querySelectorAll('.set-form')];
    const field = (form, name) => form?.elements.namedItem(name);
    const working = form => field(form, 'completed').checked && !field(form, 'AddSet.IsWarmup').checked &&
        !form.classList.contains('is-skipped') && Number(field(form, 'AddSet.Reps').value) > 0;
    const pending = form => !field(form, 'completed').checked && !form.classList.contains('is-skipped');
    const noun = n => n % 100 >= 11 && n % 100 <= 14 ? 'подходов записано' : n % 10 === 1 ? 'подход записан' :
        n % 10 >= 2 && n % 10 <= 4 ? 'подхода записано' : 'подходов записано';
    const clock = seconds => `${String(Math.floor(seconds / 60)).padStart(2, '0')}:${String(Math.floor(seconds % 60)).padStart(2, '0')}`;
    function remember() { try { localStorage.setItem(key, openId || 'closed'); } catch { /* Optional. */ } }
    function renderCards() {
        const cards = [...root.querySelectorAll('.qw-exercise')];
        if (openId !== 'closed' && !cards.some(card => card.id === openId))
            openId = cards.find(card => forms(card).some(pending))?.id || cards[0]?.id;
        cards.forEach(card => {
            const expanded = card.id === openId, count = forms(card).filter(working).length;
            card.dataset.qwExpanded = String(expanded);
            card.querySelector('.qw-exercise-body').hidden = !expanded;
            card.querySelectorAll('[data-qw-toggle]').forEach(button => button.setAttribute('aria-expanded', String(expanded)));
            card.querySelector('.qs-exercise-chevron').setAttribute('aria-label', `${expanded ? 'Свернуть' : 'Развернуть'}: ${card.querySelector('h2').textContent.trim()}`);
            card.querySelector('[data-qw-card-summary]').textContent = `${count} ${noun(count)}`;
        });
        root.querySelector('.qs-add-exercise').hidden = !cards.length;
    }
    function renderNext() {
        const all = forms(root), after = all.find(form => Number(field(form, 'setId').value) === state.restAfterSetId);
        nextForm = (after && forms(after.closest('.session-exercise')).find(pending)) || all.find(pending) || null;
        const host = root.querySelector('[data-qw-next-set]');
        host.hidden = !nextForm;
        const context = root.querySelector('[data-qw-rest-context]');
        context.textContent = after ? `После подхода ${field(after, 'AddSet.SetNumber').value} · ${after.closest('.session-exercise').querySelector('h2').textContent.trim()}` : 'Между подходами';
        if (!nextForm) return;
        root.querySelector('[data-qw-next-name]').textContent = nextForm.closest('.session-exercise').querySelector('h2').textContent.trim();
        const bodyweight = field(nextForm, 'AddSet.IsBodyweight').checked;
        for (const [selector, name] of [['[data-qw-next-weight]', 'AddSet.Weight'], ['[data-qw-next-reps]', 'AddSet.Reps']]) {
            const input = root.querySelector(selector);
            if (document.activeElement !== input) input.value = field(nextForm, name).value;
            input.disabled = state.status !== 'InProgress';
        }
        root.querySelector('[data-qw-next-weight]').hidden = bodyweight;
        root.querySelector('[data-qw-next-weight-unit]').hidden = bodyweight;
        root.querySelector('[data-qw-next-bodyweight]').hidden = !bodyweight;
    }
    function renderDock() {
        const clockText = root.querySelector('[data-rest-clock]').textContent;
        root.querySelector('[data-qw-mini-clock]').textContent = clockText;
        const all = forms(root), after = all.find(form => Number(field(form, 'setId').value) === state.restAfterSetId);
        const rest = Number(after ? field(after, 'AddSet.RestSeconds').value : root.querySelector('[data-qw-rest-form] [name="seconds"]').value) || 0;
        const mode = root.querySelector('[data-qw-mode="stopwatch"]').getAttribute('aria-pressed') === 'true' ? 'stopwatch' : 'timer';
        root.querySelector('#quick-rest-title').textContent = mode === 'stopwatch' ? 'Секундомер' : 'Отдых';
        const remaining = state.remainingRestSeconds ?? state.restRemainingSeconds;
        const activeRest = state.restEndsAtUtc || remaining != null;
        const restPaused = state.status === 'Paused' || (mode === 'timer' && activeRest && !state.restEndsAtUtc && remaining > 0);
        root.dataset.qwRestPaused = String(restPaused);
        const total = mode === 'timer' ? `из ${clock(rest)}` : 'Секундомер';
        root.querySelector('[data-qw-rest-total]').textContent = total;
        root.querySelector('[data-qw-mini-caption]').textContent = restPaused ? 'Отдых на паузе' : mode === 'stopwatch' ? 'Секундомер' :
            activeRest && remaining <= 0 ? 'Можно продолжать' : `Отдых · ${rest} с`;
        const percentage = mode === 'stopwatch' ? 0 : activeRest ? Math.min(100, Math.max(0, (remaining ?? rest) / Math.max(rest, 1) * 100)) : 100;
        root.querySelector('[data-qw-mini-progress]').style.width = `${percentage}%`;
        root.querySelector('[data-qw-ring]').style.setProperty('--rest-progress', `${percentage}%`);
        root.querySelector('[data-qw-timer-skip] span').textContent = mode === 'stopwatch' ? 'Сбросить секундомер' : 'Пропустить отдых';
        root.querySelector('[data-qw-mini-progress]').closest('.qs-mini-track').hidden = mode === 'stopwatch';
    }
    root.addEventListener('click', event => {
        const notes = event.target.closest('[data-session-notes]');
        if (notes) {
            event.stopPropagation();
            const card = notes.closest('.session-exercise');
            const form = forms(card).find(pending) || forms(card)[0];
            if (form) {
                notes.closest('details')?.removeAttribute('open');
                root.querySelectorAll('.log-set-options[open]').forEach(menu => { menu.open = false; });
                form.querySelector('.log-set-options').open = true;
                field(form, 'AddSet.Notes').focus();
            }
        }
        const toggle = event.target.closest('[data-qw-toggle]');
        if (toggle) {
            const card = toggle.closest('.qw-exercise');
            openId = openId === card.id ? 'closed' : card.id;
            remember(); renderCards();
        }
        if (event.target.closest('[data-qw-timer-skip]')) restDialog.close();
        if (event.target.closest('[data-qw-mode],[data-qw-preset],[data-qw-adjust]')) queueMicrotask(renderDock);
    });
    root.addEventListener('workout-state', event => { state = event.detail; renderCards(); renderNext(); renderDock(); });
    root.addEventListener('workout-exercises-changed', event => {
        if (event.detail?.exerciseId) openId = event.detail.exerciseId;
        renderCards(); renderNext(); renderDock(); remember();
    });
    root.addEventListener('input', event => {
        const input = event.target;
        if (input.matches('[data-qw-next-weight],[data-qw-next-reps]') && nextForm && state.status === 'InProgress') {
            const target = field(nextForm, input.hasAttribute('data-qw-next-weight') ? 'AddSet.Weight' : 'AddSet.Reps');
            target.value = input.value;
            target.dispatchEvent(new Event('input', {bubbles: true}));
        } else queueMicrotask(() => { renderCards(); renderNext(); });
    });
    // Immediate addition uses the same serialized, idempotent picker command as the full library.
    libraryRoot.addEventListener('click', async event => {
        const button = event.target.closest('[data-library-add]');
        if (!button || button.disabled) return;
        event.preventDefault(); event.stopImmediatePropagation();
        if (adding || library.busy || state.status !== 'InProgress') return;
        adding = true;
        try {
            const seconds = Number(root.querySelector('[data-qw-rest-form] [name="seconds"]').value);
            await root.pickWorkoutExercises([{exerciseId: button.value, setsCount: 3, weight: 0, reps: 8, restSeconds: seconds}], Number(document.querySelector('[data-exercise-entry]').value) || 0);
        } catch (error) { library.message(error.message); }
        finally { adding = false; }
    }, true);
    pickerDialog.addEventListener('close', () => library.message(''));
    // Avoid covering focused set inputs with the fixed action bar while a phone keyboard is open.
    function keyboard() {
        const viewport = window.visualViewport;
        const inset = viewport ? Math.max(0, innerHeight - viewport.height - viewport.offsetTop) : 0;
        const editing = root.contains(document.activeElement) && document.activeElement.matches('.set-form input');
        document.body.classList.toggle('qs-keyboard', editing && inset > 120);
    }
    window.visualViewport?.addEventListener('resize', keyboard);
    document.addEventListener('focusin', keyboard);
    document.addEventListener('focusout', () => setTimeout(keyboard, 0));
    renderCards(); renderNext(); renderDock();
})();

(() => {
  'use strict';
  const root = document.querySelector('[data-active-workout]');
  const initial = document.getElementById('workout-execution-state');
  if (!root || !initial) return;
  let state = JSON.parse(initial.textContent);
  if (!['InProgress', 'Paused'].includes(state.status)) return;
  const storageKey = `enix-workout-view:${root.dataset.viewer}:${root.dataset.sessionId}`;
  const reduced = matchMedia('(prefers-reduced-motion: reduce)');
  const overview = root.querySelector('[data-app-overview]');
  const sidebar = root.querySelector('[data-app-sidebar]');
  const outline = document.querySelector('[data-app-outline]');
  const dock = root.querySelector('[data-app-dock]');
  const action = root.querySelector('[data-app-action]');
  const feedback = root.querySelector('[data-app-feedback]');
  let selected = null, feedbackTimeout, structure = '';
  const cards = () => [...root.querySelectorAll('.session-exercise')];
  const forms = card => [...(card || root).querySelectorAll('.set-form')];
  const completed = form => form.elements.namedItem('completed').checked;
  const pending = form => !completed(form) && !form.classList.contains('is-skipped');
  const title = card => card.querySelector('h2').textContent;
  const current = () => cards().find(card => card.id === selected);
  const clock = seconds => `${String(Math.floor(Math.max(0, seconds) / 60)).padStart(2, '0')}:${String(Math.floor(Math.max(0, seconds) % 60)).padStart(2, '0')}`;
  try { selected = localStorage.getItem(storageKey); } catch { /* The server session remains usable without local storage. */ }
  root.classList.add('workout-app');
  overview.hidden = dock.hidden = false;
  function select(id, scroll = false) {
    const card = cards().find(item => item.id === id);
    if (!card) return;
    selected = card.id;
    try { localStorage.setItem(storageKey, selected); } catch { /* Selection is optional local state. */ }
    render();
    document.getElementById('workout-outline-dialog').close();
    if (scroll) card.scrollIntoView({behavior: reduced.matches ? 'instant' : 'smooth', block: 'start'});
  }
  function buildOutline() {
    const signature = cards().map(card => `${card.id}:${title(card)}`).join('|');
    if (signature === structure) return;
    structure = signature;
    sidebar.replaceChildren(); outline.replaceChildren();
    cards().forEach((card, index) => {
      for (const container of [sidebar, outline]) {
        const button = document.createElement('button'); button.type = 'button'; button.className = 'workout-app-exercise'; button.dataset.exerciseTarget = card.id;
        const number = document.createElement('span'); number.className = 'workout-app-exercise-number'; number.textContent = index + 1;
        const copy = document.createElement('span'); const name = document.createElement('strong'); name.textContent = title(card);
        const count = document.createElement('small'); count.dataset.exerciseCount = ''; copy.append(name, count); button.append(number, copy);
        button.addEventListener('click', () => select(card.id, true)); container.append(button);
      }
    });
  }
  function render() {
    buildOutline();
    if (!current()) selected = cards().find(card => forms(card).some(pending))?.id || cards()[0]?.id;
    const card = current(), all = forms(), done = all.filter(completed).length, total = all.filter(form => completed(form) || pending(form)).length;
    const progress = root.querySelector('[data-app-progress]'); progress.max = Math.max(1, total); progress.value = done;
    root.querySelector('[data-app-count]').textContent = total ? `${done} / ${total} подходов` : 'Начните с первого упражнения';
    root.querySelector('[data-app-outline-count]').textContent = `Состав · ${cards().length}`;
    sidebar.hidden = cards().length === 0;
    cards().forEach(item => {
      const selectedCard = item === card;
      item.classList.toggle('is-current', selectedCard);
      item.setAttribute('aria-label', title(item));
      const next = forms(item).find(pending);
      forms(item).forEach(form => form.classList.toggle('is-next-set', form === next));
      if (selectedCard && next) next.querySelector('input[name="AddSet.Reps"]').setAttribute('min', '1');
    });
    for (const button of document.querySelectorAll('[data-exercise-target]')) {
      const item = cards().find(c => c.id === button.dataset.exerciseTarget);
      if (!item) continue;
      const sets = forms(item), count = sets.filter(completed).length;
      button.setAttribute('aria-current', item === card ? 'step' : 'false');
      button.classList.toggle('is-done', sets.length > 0 && !sets.some(pending));
      button.querySelector('[data-exercise-count]').textContent = `${count} / ${sets.length} подходов`;
    }
    const paused = state.status === 'Paused', rest = state.remainingRestSeconds;
    const next = card && forms(card).find(pending);
    const another = cards().find(item => item !== card && forms(item).some(pending));
    let caption = card ? title(card) : 'Свободная тренировка', detail, label, mode;
    if (paused) { caption = 'Занятие на паузе'; detail = rest != null ? `Отдых остановлен · ${clock(rest)}` : 'Продолжите, когда будете готовы'; label = 'Продолжить занятие'; mode = 'resume'; }
    else if (rest > 0) { caption = 'Отдых после подхода'; detail = clock(rest); label = 'Закончить отдых'; mode = 'rest'; }
    else if (!card) { detail = 'Собирайте занятие по ходу'; label = 'Выбрать упражнение'; mode = 'add'; }
    else if (next) { detail = `Подход ${next.elements.namedItem('AddSet.SetNumber').value} · вес и повторения выше`; label = 'Подход выполнен'; mode = 'set'; }
    else if (another) { detail = 'Подходы этого упражнения разобраны'; label = 'Следующее упражнение'; mode = 'next'; }
    else { detail = 'Можно добавить подходы или завершить'; label = 'Завершить тренировку'; mode = 'finish'; }
    root.querySelector('[data-app-action-caption]').textContent = caption;
    root.querySelector('[data-app-action-detail]').textContent = detail;
    action.textContent = label; action.dataset.mode = mode;
    dock.classList.toggle('is-resting', rest > 0 && !paused); dock.classList.toggle('is-paused', paused);
    root.querySelector('[data-app-rest-tools]').hidden = rest == null || rest <= 0 || paused;
    if (innerWidth >= 1024) {
      const bounds = root.querySelector('.session-exercises').getBoundingClientRect();
      root.style.setProperty('--workout-dock-left', `${bounds.left}px`);
      root.style.setProperty('--workout-dock-width', `${bounds.width}px`);
    }
  }
  function notify(text, undoForm) {
    clearTimeout(feedbackTimeout); feedback.replaceChildren();
    const message = document.createElement('span'); message.textContent = text; feedback.append(message);
    if (undoForm) {
      const undo = document.createElement('button'); undo.type = 'button'; undo.textContent = 'Отменить';
      undo.addEventListener('click', () => {
        if (undoForm.isConnected && state.status !== 'Paused') {
          const setId = Number(undoForm.elements.namedItem('setId').value);
          const input = undoForm.elements.namedItem('completed'); input.checked = false; input.dispatchEvent(new Event('change', {bubbles: true}));
          if (state.restAfterSetId === setId) root.querySelector('[data-control="skip-rest"]').click();
          render();
        }
        feedback.hidden = true;
      }); feedback.append(undo);
    }
    feedback.hidden = false;
    feedbackTimeout = setTimeout(() => { feedback.hidden = true; }, 6500);
  }
  root.addEventListener('workout-state', event => { state = event.detail; render(); });
  root.addEventListener('workout-exercises-changed', event => {
    if (event.detail.exerciseId) selected = event.detail.exerciseId;
    render(); if (selected) select(selected);
    notify('Упражнение добавлено в занятие');
  });
  root.addEventListener('change', event => {
    const form = event.target.closest('.set-form');
    if (!form) return;
    queueMicrotask(() => {
      render();
      if (event.target.name === 'completed' && completed(form) && form.classList.contains('is-completed')) {
        form.classList.remove('app-set-pulse'); void form.offsetWidth; form.classList.add('app-set-pulse');
        notify('Подход отмечен', form);
      }
    });
  });
  root.addEventListener('input', event => { if (event.target.closest('.set-form')) render(); });
  action.addEventListener('click', () => {
    switch (action.dataset.mode) {
      case 'resume': root.querySelector('[data-pause-button]').click(); break;
      case 'rest': root.querySelector('[data-control="skip-rest"]').click(); break;
      case 'add': root.querySelector('[data-add-exercise]').click(); break;
      case 'next': select(cards().find(item => item !== current() && forms(item).some(pending)).id, true); break;
      case 'finish': root.querySelector('[data-finish]').click(); break;
      case 'set': {
        const form = forms(current()).find(pending), input = form.elements.namedItem('completed');
        input.checked = true;
        const reps = form.elements.namedItem('AddSet.Reps');
        reps.setCustomValidity(Number(reps.value) < 1 ? 'Укажите хотя бы одно повторение.' : '');
        if (!form.reportValidity()) { input.checked = false; form.scrollIntoView({behavior: reduced.matches ? 'instant' : 'smooth', block: 'center'}); return; }
        input.dispatchEvent(new Event('change', {bubbles: true})); break;
      }
    }
  });
  root.querySelector('[data-app-pause]').addEventListener('click', () => root.querySelector('[data-pause-button]').click());
  // Removal by the execution client must update counts and navigation as well.
  new MutationObserver(() => render()).observe(root.querySelector('.session-exercises'), {childList: true, subtree: true});
  new ResizeObserver(() => render()).observe(root.querySelector('.session-exercises'));
  let viewportHeight = window.visualViewport?.height || innerHeight;
  const keyboardLayout = () => {
    const viewport = window.visualViewport;
    if (!viewport) return;
    const editing = document.activeElement?.matches('input:not([type="hidden"]),textarea');
    const inset = Math.max(0, innerHeight - viewport.height - viewport.offsetTop);
    const open = editing && (inset > 120 || viewportHeight - viewport.height > 120);
    document.body.classList.toggle('workout-keyboard-open', !!open);
    root.style.setProperty('--workout-keyboard-inset', `${open ? inset : 0}px`);
    if (open) {
      const inputBounds = document.activeElement.getBoundingClientRect(), dockBounds = dock.getBoundingClientRect();
      if (inputBounds.bottom > dockBounds.top - 16) window.scrollBy({top:inputBounds.bottom - dockBounds.top + 16,behavior:'instant'});
    }
    if (!editing) viewportHeight = viewport.height;
  };
  window.visualViewport?.addEventListener('resize', keyboardLayout);
  window.visualViewport?.addEventListener('scroll', keyboardLayout);
  document.addEventListener('focusin', keyboardLayout); document.addEventListener('focusout', () => setTimeout(keyboardLayout, 0));
  render();
})();

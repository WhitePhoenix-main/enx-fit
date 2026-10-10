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
  const orderDialog = document.getElementById('workout-outline-dialog');
  const dock = root.querySelector('[data-app-dock]');
  const action = root.querySelector('[data-app-action]');
  const feedback = root.querySelector('[data-app-feedback]');
  let selected = null, feedbackTimeout, structure = '';
  let editingOrder = false, orderBusy = false, draftOrder = [], dragged = null;
  const cards = () => [...root.querySelectorAll('.session-exercise')];
  const canReorder = () => cards().some((card,index,entries) => index > 0 && entries[index-1].dataset.blockKey === card.dataset.blockKey);
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
    if (editingOrder) return;
    const signature = cards().map(card => `${card.id}:${title(card)}:${card.dataset.blockKey}`).join('|');
    if (signature === structure) return;
    structure = signature;
    sidebar.replaceChildren(); outline.replaceChildren();
    for (const container of [sidebar, outline]) buildRows(container, cards());
  }
  function buildRows(container, entries) {
    container.replaceChildren(); let previous = null;
    entries.forEach((card, index) => {
        if (card.dataset.blockKey !== previous) {
          previous = card.dataset.blockKey;
          const heading = document.createElement('div'); heading.className = 'workout-outline-block'; heading.dataset.kind = card.dataset.blockKind;
          const type = document.createElement('span'); type.textContent = card.dataset.blockType;
          const name = document.createElement('strong'); name.textContent = card.dataset.blockName;
          heading.append(type); if (name.textContent !== type.textContent) heading.append(name);
          if (container === outline && card.dataset.blockKind === 'superset') {
            const hint = document.createElement('small'); hint.textContent = 'Связанные упражнения · подходы отмечаются отдельно'; heading.append(hint);
          }
          container.append(heading);
        }
        const row = document.createElement('div'); row.className = 'workout-order-row'; row.dataset.orderKey = card.id;
        const button = document.createElement('button'); button.type = 'button'; button.className = 'workout-app-exercise app-item-row'; button.dataset.exerciseTarget = card.id;
        const number = document.createElement('span'); number.className = 'workout-app-exercise-number'; number.textContent = index + 1;
        const copy = document.createElement('span'); const name = document.createElement('strong'); name.textContent = title(card);
        const count = document.createElement('small'); count.dataset.exerciseCount = '';
        const stepState = document.createElement('small'); stepState.dataset.exerciseStatus = ''; stepState.className = 'app-exercise-state';
        copy.append(name, count, stepState); button.append(number, copy);
        button.disabled = container === outline && editingOrder;
        button.addEventListener('click', () => select(card.id, true)); row.append(button);
        if (container === outline && editingOrder) {
          row.draggable = true; row.dataset.orderEntry = card.id;
          for (const delta of [-1,1]) {
            const move = document.createElement('button'); move.type = 'button'; move.className = 'icon-button'; move.dataset.orderMove = `${card.id}:${delta}`;
            move.textContent = delta < 0 ? '↑' : '↓'; move.setAttribute('aria-label', `${delta < 0 ? 'Поднять' : 'Опустить'} ${title(card)}`);
            move.disabled = orderBusy || entries[index + delta]?.dataset.blockKey !== card.dataset.blockKey;
            move.addEventListener('click', () => moveOrder(card.id, index + delta, `${card.id}:${-delta}`)); row.append(move);
          }
        }
        container.append(row);
    });
  }
  const orderMessage = message => { const status = orderDialog.querySelector('[data-order-status]'); status.textContent = message; status.hidden = !message; };
  function renderOrder() {
    buildRows(outline, draftOrder.map(id => cards().find(card => card.id === id)).filter(Boolean));
    render();
  }
  function moveOrder(id, destination, focusKey) {
    const from = draftOrder.indexOf(id), target = cards().find(card => card.id === draftOrder[destination]), source = cards().find(card => card.id === id);
    if (!target || !source || source.dataset.blockKey !== target.dataset.blockKey || from === destination || orderBusy) return;
    const before = window.ReorderMotion?.capture(outline);
    draftOrder.splice(from,1); draftOrder.splice(destination,0,id); renderOrder();
    window.ReorderMotion?.play(outline,before);
    orderDialog.querySelector(`[data-order-move="${focusKey || id + ':1'}"]`)?.focus();
    orderMessage(`${title(source)} · позиция ${destination + 1}. Сохраните, чтобы применить.`);
  }
  function editOrder(value) {
    editingOrder = value; root.dataset.orderEditing = String(value); orderDialog.classList.toggle('is-ordering',value);
    for (const selector of ['[data-order-help]','[data-order-actions]']) orderDialog.querySelector(selector).hidden = !value;
    orderDialog.querySelector('[data-order-edit]').hidden = value || !canReorder();
    orderDialog.querySelector('[data-add-exercise]').hidden = value;
    orderMessage('');
    if (value) { draftOrder = cards().map(card => card.id); renderOrder(); }
    else { structure = ''; render(); if (orderDialog.open) orderDialog.querySelector('[data-order-edit]').focus(); }
  }
  orderDialog.querySelector('[data-order-edit]').addEventListener('click', () => editOrder(true));
  orderDialog.querySelector('[data-order-cancel]').addEventListener('click', () => editOrder(false));
  orderDialog.addEventListener('close', () => { if (editingOrder && !orderBusy) editOrder(false); });
  orderDialog.addEventListener('cancel', event => { if (orderBusy) event.preventDefault(); });
  orderDialog.querySelector('[data-order-save]').addEventListener('click', async () => {
    if (orderBusy || !editingOrder || !root.reorderWorkout) return;
    orderBusy = true;
    orderDialog.querySelectorAll('button').forEach(button => { button.disabled = true; });
    orderMessage('Сохраняем порядок…');
    try {
      const result = await root.reorderWorkout(draftOrder.map(id => Number(cards().find(card => card.id === id).dataset.entryId)));
      const container = root.querySelector('.session-exercises');
      const marker = document.createComment('exercise order'); container.insertBefore(marker,container.querySelector('.session-exercise'));
      result.order.forEach(id => { const card = cards().find(card => Number(card.dataset.entryId) === id); if (card) container.insertBefore(card,marker); });
      marker.remove();
      cards().forEach((card,index) => { card.querySelector('.exercise-order').textContent = index + 1; });
      orderBusy = false; editOrder(false); notify('Порядок сохранён. Результаты подходов на месте.');
      orderMessage('Порядок сохранён');
    } catch (error) { orderMessage(error.message); }
    finally {
      orderBusy = false;
      orderDialog.querySelectorAll('button').forEach(button => { button.disabled = false; });
      if (editingOrder) renderOrder();
    }
  });
  outline.addEventListener('dragstart', event => {
    const row = event.target.closest('[data-order-entry]');
    if (!row || orderBusy) return event.preventDefault();
    dragged = row.dataset.orderEntry; event.dataTransfer.setData('text/plain',dragged); event.dataTransfer.effectAllowed = 'move';
  });
  outline.addEventListener('dragover', event => {
    const row = event.target.closest('[data-order-entry]'), source = cards().find(card => card.id === dragged), target = cards().find(card => card.id === row?.dataset.orderEntry);
    if (source && target && source.dataset.blockKey === target.dataset.blockKey) event.preventDefault();
  });
  outline.addEventListener('drop', event => { const row = event.target.closest('[data-order-entry]'); if (row && dragged) { event.preventDefault(); moveOrder(dragged,draftOrder.indexOf(row.dataset.orderEntry)); } dragged = null; });
  outline.addEventListener('dragend', () => { dragged = null; });
  function render() {
    buildOutline();
    if (!current()) selected = cards().find(card => forms(card).some(pending))?.id || cards()[0]?.id;
    const card = current(), all = forms(), done = all.filter(completed).length, total = all.filter(form => completed(form) || pending(form)).length;
    const progress = root.querySelector('[data-app-progress]'); progress.max = Math.max(1, total); progress.value = done;
    root.querySelector('[data-app-count]').textContent = total ? `${done} / ${total} подходов` : 'Начните с первого упражнения';
    root.querySelector('[data-app-outline-count]').textContent = `Состав · ${cards().length}`;
    sidebar.hidden = cards().length === 0;
    if (!editingOrder) orderDialog.querySelector('[data-order-edit]').hidden = !canReorder();
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
      const hasPending = sets.some(pending), skipped = sets.filter(form => form.classList.contains('is-skipped')).length;
      const isCurrent = item === card;
      let stepState = !sets.length ? 'empty' : hasPending ? count ? 'partial' : 'pending' : count === sets.length ? 'done' : count ? 'partial' : 'skipped';
      let stepLabel = !sets.length ? 'Пока без подходов' : hasPending ? count ? 'Часть выполнена' : 'Ожидает выполнения' : skipped === sets.length ? 'Все пропущены' : skipped ? 'Разобрано · есть пропуски' : 'Все выполнены';
      if (isCurrent && hasPending) {
        const ownRest = state.remainingRestSeconds > 0 && sets.some(form => Number(form.elements.namedItem('setId').value) === state.restAfterSetId);
        stepState = state.status === 'Paused' ? 'paused' : ownRest ? 'rest' : 'current';
        stepLabel = state.status === 'Paused' ? 'На паузе' : ownRest ? 'Отдых после подхода' : 'Сейчас';
      }
      button.dataset.appStep = stepState;
      button.classList.toggle('is-done', stepState === 'done');
      button.querySelector('[data-exercise-count]').textContent = `${count} / ${sets.length} подходов`;
      button.querySelector('[data-exercise-status]').textContent = stepLabel;
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
  let viewportHeight = window.visualViewport?.height || innerHeight, viewportWidth = innerWidth;
  const keyboardLayout = () => {
    const viewport = window.visualViewport;
    if (!viewport) return;
    if (viewportWidth !== innerWidth) { viewportWidth = innerWidth; viewportHeight = viewport.height; }
    const input = document.activeElement;
    const editing = matchMedia('(max-width:1023px)').matches && root.contains(input) &&
      !input?.closest('dialog') && input?.matches('input:not([type="hidden"]):not([type="checkbox"]):not([type="radio"]),textarea');
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

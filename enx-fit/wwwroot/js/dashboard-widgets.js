(() => {
  'use strict';
  const grid = document.querySelector('[data-modular-dashboard]');
  if (!grid) return;
  const config = JSON.parse(document.getElementById('dashboard-layout-data').textContent);
  const pinnedId = config.pinnedWidgetId;
  const copy = value => value.map(widget => ({ ...widget }))
    .sort((a, b) => Number(b.id === pinnedId) - Number(a.id === pinnedId));
  let saved = copy(config.widgets);
  let draft = copy(saved);
  let editing = false;
  let saving = false;
  let drag = null;
  const widgets = new Map([...grid.querySelectorAll('[data-widget-id]')].map(node => [node.dataset.widgetId, node]));
  const editor = document.getElementById('layout-editor');
  const feedback = document.querySelector('[data-layout-feedback]');
  const empty = document.querySelector('[data-layout-empty]');
  const editButtons = [...document.querySelectorAll('[data-layout-edit]')];
  const saveButton = document.querySelector('[data-layout-save]');
  const hasChanges = () => JSON.stringify(draft) !== JSON.stringify(saved);
  const say = (message, error = false) => {
    feedback.textContent = message;
    feedback.classList.toggle('is-error', error);
    feedback.hidden = !message;
  };
  const render = () => {
    const focused = document.activeElement;
    const visible = draft.filter(widget => widget.visible);
    for (const [position, state] of draft.entries()) {
      const node = widgets.get(state.id);
      node.hidden = !state.visible;
      node.dataset.widgetWidth = state.width;
      node.querySelector('.widget-editbar').hidden = !editing;
      node.querySelector('[data-widget-width-select]').value = state.width;
      const index = visible.findIndex(widget => widget.id === state.id);
      const pinned = state.id === pinnedId;
      node.querySelector('[data-widget-drag]').disabled = pinned;
      node.querySelector('[data-widget-move="-1"]').disabled = pinned || index <= 0 || visible[index - 1]?.id === pinnedId;
      node.querySelector('[data-widget-move="1"]').disabled = pinned || index === visible.length - 1;
      // Move only the changed nodes, preserving focus and mobile scroll position.
      if (grid.children[position] !== node) grid.insertBefore(node, grid.children[position] || null);
    }
    document.querySelectorAll('[data-widget-toggle]').forEach(toggle => {
      toggle.checked = draft.find(widget => widget.id === toggle.dataset.widgetToggle).visible;
    });
    document.querySelector('[data-widget-count]').textContent = `${visible.length} из ${draft.length}`;
    empty.hidden = visible.length > 0;
    if (focused instanceof HTMLElement && focused.closest('[data-widget-id]') && !focused.closest('[data-widget-id]').hidden)
      focused.focus({ preventScroll: true });
  };
  const setEditing = value => {
    editing = value;
    editor.hidden = !value;
    document.body.classList.toggle('is-layout-editing', value);
    editButtons.forEach(button => button.setAttribute('aria-expanded', String(value)));
    render();
  };
  const begin = () => {
    if (saving) return;
    if (!editing) { draft = copy(saved); setEditing(true); say(''); }
    editor.scrollIntoView({ block: 'start', behavior: 'auto' });
    editor.querySelector('summary').focus({ preventScroll: true });
  };
  const changed = message => say(`${message} Сохраните раскладку, когда закончите.`);
  const moveRelative = (id, direction) => {
    if (!editing || saving || id === pinnedId) return;
    const visible = draft.filter(widget => widget.visible);
    const index = visible.findIndex(widget => widget.id === id);
    const target = visible[index + direction];
    if (!target || target.id === pinnedId) return;
    const from = draft.findIndex(widget => widget.id === id);
    const to = draft.findIndex(widget => widget.id === target.id);
    [draft[from], draft[to]] = [draft[to], draft[from]];
    render();
    widgets.get(id).querySelector('[data-widget-drag]').focus({ preventScroll: true });
    widgets.get(id).querySelector('[data-widget-drag]').scrollIntoView({ block: 'nearest', behavior: 'instant' });
    changed(`Блок «${widgets.get(id).dataset.widgetTitle}» перемещён ${direction < 0 ? 'раньше' : 'позже'}.`);
  };
  editButtons.forEach(button => button.addEventListener('click', begin));
  document.querySelector('[data-layout-cancel]').addEventListener('click', () => {
    if (saving) return;
    finishDrag(false);
    draft = copy(saved);
    setEditing(false);
    say('Изменения отменены. Восстановлена сохранённая раскладка.');
    editButtons[0].focus({ preventScroll: true });
  });
  document.querySelector('[data-layout-reset]').addEventListener('click', () => {
    if (saving) return;
    draft = copy(config.defaults);
    render();
    changed('Восстановлена раскладка по умолчанию.');
  });
  editor.addEventListener('change', event => {
    if (saving || !event.target.matches('[data-widget-toggle]')) return;
    const state = draft.find(widget => widget.id === event.target.dataset.widgetToggle);
    state.visible = event.target.checked;
    render();
    changed(`Блок «${widgets.get(state.id).dataset.widgetTitle}» ${state.visible ? 'добавлен' : 'скрыт'}.`);
  });
  grid.addEventListener('change', event => {
    if (!editing || saving || !event.target.matches('[data-widget-width-select]')) return;
    const node = event.target.closest('[data-widget-id]');
    draft.find(widget => widget.id === node.dataset.widgetId).width = event.target.value;
    render();
    changed(`Ширина блока «${node.dataset.widgetTitle}» изменена.`);
  });
  grid.addEventListener('click', event => {
    if (!editing || saving) return;
    const node = event.target.closest('[data-widget-id]');
    if (!node) return;
    const move = event.target.closest('[data-widget-move]');
    if (move) moveRelative(node.dataset.widgetId, Number(move.dataset.widgetMove));
    if (event.target.closest('[data-widget-hide]')) {
      draft.find(widget => widget.id === node.dataset.widgetId).visible = false;
      render();
      changed(`Блок «${node.dataset.widgetTitle}» скрыт. Его можно вернуть из каталога.`);
      editButtons[0].focus({ preventScroll: true });
    }
  });
  grid.addEventListener('keydown', event => {
    if (!event.target.matches('[data-widget-drag]')) return;
    if (['ArrowUp', 'ArrowLeft', 'ArrowDown', 'ArrowRight'].includes(event.key)) {
      event.preventDefault();
      moveRelative(event.target.closest('[data-widget-id]').dataset.widgetId,
        ['ArrowUp', 'ArrowLeft'].includes(event.key) ? -1 : 1);
    }
  });

  saveButton.addEventListener('click', async () => {
    if (saving || !editing) return;
    finishDrag(false);
    saving = true;
    const controls = [...editor.querySelectorAll('button, input'), ...grid.querySelectorAll('.widget-editbar button, .widget-editbar select')];
    const disabled = controls.map(control => control.disabled);
    controls.forEach(control => { control.disabled = true; });
    saveButton.textContent = 'Сохраняем…';
    saveButton.setAttribute('aria-busy', 'true');
    say('Сохраняем вашу раскладку…');
    try {
      const response = await fetch(grid.dataset.layoutUrl, {
        method: 'POST', credentials: 'same-origin',
        headers: {
          'Content-Type': 'application/json',
          'RequestVerificationToken': document.querySelector('[data-layout-token] input').value
        },
        body: JSON.stringify({ widgets: draft }),
        signal: AbortSignal.timeout(15000)
      });
      if (response.redirected || response.status === 401)
        throw new Error('Сессия истекла. Откройте вход в другой вкладке и повторите сохранение.');
      if (!response.headers.get('content-type')?.includes('application/json'))
        throw new Error('Сохранение недоступно. Проверьте подключение и доступ к странице, затем повторите попытку.');
      const result = await response.json();
      if (!response.ok) throw new Error(result.error || 'Не удалось сохранить раскладку. Попробуйте ещё раз.');
      saved = copy(result.widgets);
      draft = copy(saved);
      setEditing(false);
      say('Раскладка сохранена. Она будет доступна при следующем входе и на других устройствах.');
      editButtons[0].focus({ preventScroll: true });
    } catch (error) {
      say(error instanceof Error && error.name !== 'TypeError' && error.name !== 'TimeoutError'
        ? error.message : 'Нет ответа от сервера. Ваши изменения остались на экране — повторите сохранение.', true);
    } finally {
      saving = false;
      controls.forEach((control, index) => { control.disabled = disabled[index]; });
      saveButton.textContent = 'Сохранить';
      saveButton.removeAttribute('aria-busy');
    }
  });

  const clearDropTargets = () => widgets.forEach(node => node.classList.remove('drop-before', 'drop-after'));
  const updateDropTarget = () => {
    if (!drag) return;
    clearDropTargets();
    const node = document.elementFromPoint(drag.x, drag.y)?.closest('[data-widget-id]');
    drag.target = null;
    if (!node || node === drag.node || node.hidden) return;
    const box = node.getBoundingClientRect();
    // At row edges the vertical position disambiguates full-width modules.
    const after = node.dataset.widgetId === pinnedId || drag.y > box.bottom - Math.min(70, box.height / 3) ||
      (drag.y >= box.top + Math.min(70, box.height / 3) && drag.x > box.left + box.width / 2);
    drag.target = node;
    drag.after = after;
    node.classList.add(after ? 'drop-after' : 'drop-before');
  };
  const dragFrame = () => {
    if (!drag?.active) return;
    const margin = 90;
    if (drag.y < margin) window.scrollBy(0, -Math.ceil((margin - drag.y) / 5));
    else if (drag.y > innerHeight - margin) window.scrollBy(0, Math.ceil((drag.y - innerHeight + margin) / 5));
    updateDropTarget();
    drag.frame = requestAnimationFrame(dragFrame);
  };
  function finishDrag(commit) {
    if (!drag) return;
    const current = drag;
    drag = null;
    cancelAnimationFrame(current.frame);
    if (current.handle.hasPointerCapture(current.pointerId)) current.handle.releasePointerCapture(current.pointerId);
    current.ghost?.remove();
    current.node.classList.remove('is-dragging');
    document.body.classList.remove('widget-dragging');
    clearDropTargets();
    if (commit && current.active && current.target) {
      const from = draft.findIndex(widget => widget.id === current.node.dataset.widgetId);
      const [state] = draft.splice(from, 1);
      const to = draft.findIndex(widget => widget.id === current.target.dataset.widgetId);
      draft.splice(Math.max(1, to + (current.after ? 1 : 0)), 0, state);
      render();
      current.handle.focus({ preventScroll: true });
      changed(`Положение блока «${current.node.dataset.widgetTitle}» изменено.`);
    }
  }
  grid.addEventListener('pointerdown', event => {
    const handle = event.target.closest('[data-widget-drag]');
    if (!handle || handle.disabled || !editing || saving || event.button !== 0) return;
    handle.focus({ preventScroll: true });
    handle.setPointerCapture(event.pointerId);
    drag = { handle, pointerId: event.pointerId, node: handle.closest('[data-widget-id]'), startX: event.clientX, startY: event.clientY, x: event.clientX, y: event.clientY, active: false, target: null };
    event.preventDefault();
  });
  grid.addEventListener('pointermove', event => {
    if (!drag || event.pointerId !== drag.pointerId) return;
    drag.x = event.clientX; drag.y = event.clientY;
    if (!drag.active && Math.hypot(drag.x - drag.startX, drag.y - drag.startY) > 7) {
      drag.active = true;
      drag.ghost = document.createElement('div');
      drag.ghost.className = 'widget-drag-ghost';
      drag.ghost.textContent = drag.node.dataset.widgetTitle;
      document.body.append(drag.ghost);
      drag.node.classList.add('is-dragging');
      document.body.classList.add('widget-dragging');
      drag.frame = requestAnimationFrame(dragFrame);
    }
    if (drag.active) {
      drag.ghost.style.left = `${Math.min(innerWidth - 200, Math.max(8, drag.x + 16))}px`;
      drag.ghost.style.top = `${Math.min(innerHeight - 55, Math.max(8, drag.y + 16))}px`;
      updateDropTarget();
    }
  });
  grid.addEventListener('pointerup', () => finishDrag(true));
  grid.addEventListener('pointercancel', () => finishDrag(false));
  grid.addEventListener('lostpointercapture', () => finishDrag(false));
  window.addEventListener('keydown', event => { if (event.key === 'Escape') finishDrag(false); });
  window.addEventListener('beforeunload', event => {
    if (!editing || !hasChanges()) return;
    event.preventDefault();
    event.returnValue = '';
  });

  // Navigation to a deliberately hidden module may reveal it for this visit,
  // without silently overwriting the saved visibility preference.
  const revealAnchor = () => {
    let target;
    try { target = document.getElementById(decodeURIComponent(location.hash.slice(1))); }
    catch { return; }
    const node = target?.closest('[data-widget-id]');
    if (!node?.hidden) return;
    if (editing) {
      draft.find(widget => widget.id === node.dataset.widgetId).visible = true;
      render();
      changed(`Блок «${node.dataset.widgetTitle}» добавлен.`);
    } else {
      node.hidden = false;
      empty.hidden = true;
      say(`Блок «${node.dataset.widgetTitle}» открыт по ссылке. Сохранённая раскладка не изменилась.`);
    }
    target.scrollIntoView({ block: 'start' });
  };
  window.addEventListener('hashchange', revealAnchor);
  render();
  revealAnchor();
})();

(() => {
  'use strict';
  const form = document.querySelector('.program-builder [data-program-editor]');
  if (!form) return;
  const panels = [...form.querySelectorAll('[data-editor-workout]')];
  const buttons = [...form.querySelectorAll('[data-select-workout]')];
  const selected = form.querySelector('[data-selected-workout]');
  const status = form.querySelector('[data-plan-save-status]');
  const initialStatus = {text:status.textContent, state:status.dataset.appState};
  const scrollKey = `plan-editor-scroll:${location.pathname}`;
  const about = form.querySelector('[data-plan-about]');
  about.addEventListener('toggle', () => { form.querySelector('[data-about-expanded]').value = String(about.open); });
  let dirty = form.dataset.unsaved === 'true', submitting = false;
  form.classList.add('plan-enhanced');
  form.noValidate = true;
  form.querySelector('[data-editor-navigation]').hidden = panels.length === 0;
  const selectDay = (index, focus = false) => {
    const value = Math.max(0, Math.min(panels.length - 1, Number(index) || 0));
    selected.value = String(value);
    panels.forEach((panel, i) => { panel.hidden = i !== value; });
    buttons.forEach((button, i) => {
      button.classList.toggle('is-current', i === value);
      button.setAttribute('aria-current', i === value ? 'true' : 'false');
    });
    const outline = form.querySelector('[data-editor-outline]'), button = buttons[value];
    if (button) {
      const listBounds = outline.getBoundingClientRect(), buttonBounds = button.getBoundingClientRect();
      if (buttonBounds.left < listBounds.left) outline.scrollLeft -= listBounds.left - buttonBounds.left;
      else if (buttonBounds.right > listBounds.right) outline.scrollLeft += buttonBounds.right - listBounds.right;
    }
    if (focus && panels[value]) panels[value].querySelector('[data-day-name]').focus({preventScroll:true});
  };
  buttons.forEach(button => button.addEventListener('click', () => selectDay(button.dataset.selectWorkout)));
  selectDay(selected.value);
  const update = () => {
    let count = 0;
    const days = [];
    panels.forEach((panel, index) => {
      const name = panel.querySelector('[data-day-name]').value.trim() || 'Новая тренировка';
      const day = panel.querySelector('[data-day-schedule]');
      const exercises = panel.querySelectorAll('[data-plan-exercise]').length;
      const duration = panel.querySelector('[data-day-duration]').value;
      count += exercises;
      if (day.value) days.push(day.value);
      panel.querySelector('[data-workout-title]').textContent = name;
      buttons[index].querySelector('[data-day-title]').textContent = name;
      buttons[index].querySelector('[data-day-meta]').textContent = `${day.value ? day.selectedOptions[0].text : 'Без дня'} · ${exercises} упр. · ${duration || '—'} мин`;
    });
    form.querySelector('[data-plan-summary]').textContent = `Тренировок: ${panels.length} · упражнений: ${count}. Каждая тренировка повторяется в выбранный день недели.`;
    const schedule = form.querySelector('[data-plan-schedule]');
    const frequency = Number(form.querySelector('[name="Input.DaysPerWeek"]').value);
    const repeated = new Set(days).size !== days.length;
    schedule.hidden = days.length === 0;
    schedule.classList.toggle('is-warning', repeated || (days.length > 0 && days.length !== frequency));
    schedule.textContent = repeated ? 'В один день можно назначить одну тренировку. Выберите другой день для повторяющихся.' :
      days.length !== frequency ? `Дней в расписании: ${days.length}, тренировок в неделю: ${frequency}. Согласуйте их перед сохранением.` : `Расписание готово. Тренировочных дней в неделю: ${days.length}. Остальные дни — отдых.`;
  };
  const setDirty = () => { dirty = true; status.textContent = 'Изменения не сохранены'; status.classList.add('is-dirty'); window.AppComponents?.setNoticeState(status, 'pending'); update(); };
  form.addEventListener('input', setDirty);
  form.addEventListener('change', setDirty);
  if (dirty) status.classList.add('is-dirty');
  update();
  const revealField = field => {
    const panel = field.closest('[data-editor-workout]');
    if (panel) selectDay(panel.dataset.workoutIndex);
    for (let parent = field.parentElement; parent && parent !== form; parent = parent.parentElement)
      if (parent instanceof HTMLDetailsElement) parent.open = true;
    field.focus();
  };
  form.addEventListener('submit', event => {
    const button = event.submitter;
    const structure = button && new URL(button.formAction).searchParams.get('handler') === 'Structure';
    if (!structure) {
      const invalid = form.querySelector('input:invalid,select:invalid,textarea:invalid');
      if (invalid) { event.preventDefault(); revealField(invalid); invalid.reportValidity(); return; }
    } else {
      try { sessionStorage.setItem(scrollKey, JSON.stringify({y:window.scrollY,at:Date.now(),day:selected.value})); } catch {}
    }
    submitting = true;
    status.textContent = structure ? 'Обновляем состав…' : 'Сохраняем программу…';
    window.AppComponents?.setNoticeState(status, 'pending');
    if (button) button.setAttribute('aria-busy', 'true');
  });
  window.addEventListener('beforeunload', event => {
    if (dirty && !submitting) { event.preventDefault(); event.returnValue = ''; }
  });
  window.addEventListener('pageshow', () => {
    submitting = false;
    form.querySelectorAll('[aria-busy="true"]').forEach(button => button.removeAttribute('aria-busy'));
    status.textContent = dirty ? 'Изменения не сохранены' : initialStatus.text;
    window.AppComponents?.setNoticeState(status, dirty ? 'pending' : initialStatus.state);
  });
  try {
    const previous = JSON.parse(sessionStorage.getItem(scrollKey) || 'null');
    sessionStorage.removeItem(scrollKey);
    if (previous && Date.now() - previous.at < 60000)
      requestAnimationFrame(() => {
        if (previous.day === selected.value) window.scrollTo({top:previous.y,behavior:'instant'});
        else panels[Number(selected.value)]?.scrollIntoView({block:'start',behavior:'instant'});
      });
  } catch {}
  const serverError = form.querySelector('.input-validation-error:not([type="hidden"])');
  if (serverError) revealField(serverError);
  else if (form.querySelector('.validation-summary-errors')) form.querySelector('.validation-summary-errors').scrollIntoView({block:'start',behavior:'instant'});

  const savebar = form.querySelector('.plan-savebar');
  let viewportHeight = window.visualViewport?.height || innerHeight, viewportWidth = innerWidth;
  const keyboardLayout = () => {
    const viewport = window.visualViewport;
    if (!viewport) return;
    if (viewportWidth !== innerWidth) { viewportWidth = innerWidth; viewportHeight = viewport.height; }
    const input = document.activeElement;
    const editing = matchMedia('(max-width:1023px)').matches && form.contains(input) && !input?.closest('dialog') &&
      input?.matches('input:not([type="hidden"]):not([type="checkbox"]):not([type="radio"]),textarea');
    const inset = Math.max(0, innerHeight - viewport.height - viewport.offsetTop);
    const open = editing && (inset > 120 || viewportHeight - viewport.height > 120);
    document.body.classList.toggle('plan-keyboard-open', !!open);
    const bounds = form.getBoundingClientRect();
    form.style.setProperty('--plan-keyboard-inset', `${open ? inset : 0}px`);
    form.style.setProperty('--plan-save-left', `${bounds.left}px`);
    form.style.setProperty('--plan-save-width', `${bounds.width}px`);
    if (open) {
      const inputBounds = input.getBoundingClientRect(), saveBounds = savebar.getBoundingClientRect();
      if (inputBounds.bottom > saveBounds.top - 16) window.scrollBy({top:inputBounds.bottom - saveBounds.top + 16,behavior:'instant'});
    }
    if (!editing) viewportHeight = viewport.height;
  };
  window.visualViewport?.addEventListener('resize', keyboardLayout);
  window.visualViewport?.addEventListener('scroll', keyboardLayout);
  document.addEventListener('focusin', keyboardLayout);
  document.addEventListener('focusout', () => setTimeout(keyboardLayout, 0));

  const dialog = document.getElementById('plan-exercise-picker');
  if (!dialog || typeof dialog.showModal !== 'function') return;
  const search = dialog.querySelector('[data-plan-search]');
  const equipment = dialog.querySelector('[data-plan-equipment]');
  const choices = [...dialog.querySelectorAll('[data-plan-pick]')];
  const commit = dialog.querySelector('[data-plan-pick-commit]');
  const picked = new Set();
  let addButton, capacity = 0;
  const normalize = value => value.toLocaleLowerCase('ru-RU').replaceAll('ё', 'е').trim();
  const filter = () => {
    const query = normalize(search.value);
    let visible = 0;
    choices.forEach(choice => {
      choice.hidden = !normalize(`${choice.dataset.name} ${choice.dataset.muscle}`).includes(query) ||
        (equipment.value !== '' && choice.dataset.equipment !== equipment.value);
      if (!choice.hidden) visible++;
    });
    dialog.querySelector('[data-plan-search-empty]').hidden = visible > 0;
    dialog.querySelector('[data-picker-found]').textContent = `Найдено: ${visible}`;
  };
  const updatePicked = () => {
    choices.forEach(choice => {
      const active = picked.has(choice.dataset.planPick);
      choice.setAttribute('aria-pressed', String(active));
      choice.querySelector('[data-pick-mark]').textContent = choice.dataset.added === 'true' ? '✓' : active ? '✓' : '+';
      choice.disabled = choice.dataset.added === 'true' || (!active && picked.size >= capacity);
    });
    commit.disabled = picked.size === 0;
    commit.textContent = picked.size ? `Добавить (${picked.size})` : 'Добавить';
    dialog.querySelector('[data-picker-selected]').textContent = picked.size ? `Выбрано: ${picked.size}` : `Можно добавить: ${capacity}`;
  };
  form.querySelectorAll('[data-plan-add-exercise]').forEach(button => button.addEventListener('click', event => {
    event.preventDefault();
    addButton = button;
    const panel = button.closest('[data-editor-workout]');
    const existing = [...panel.querySelectorAll('[data-plan-exercise]')].map(input => input.value);
    capacity = Math.max(0, 30 - existing.length);
    picked.clear(); search.value = ''; equipment.value = '';
    choices.forEach(choice => { choice.dataset.added = String(existing.includes(choice.dataset.planPick)); });
    dialog.querySelector('[data-picker-day]').textContent = panel.querySelector('[data-day-name]').value;
    updatePicked(); filter(); dialog.showModal();
    if (matchMedia('(min-width: 900px)').matches) search.focus();
  }));
  choices.forEach(choice => choice.addEventListener('click', () => {
    const id = choice.dataset.planPick;
    if (picked.has(id)) picked.delete(id); else if (picked.size < capacity) picked.add(id);
    updatePicked();
  }));
  search.addEventListener('input', filter);
  equipment.addEventListener('change', filter);
  commit.addEventListener('click', () => {
    if (!addButton || picked.size === 0) return;
    form.querySelectorAll('[name="selectedExerciseIds"]').forEach(input => input.remove());
    picked.forEach(id => {
      const input = document.createElement('input');
      input.type = 'hidden'; input.name = 'selectedExerciseIds'; input.value = id; form.append(input);
    });
    dialog.close();
    form.requestSubmit(addButton);
  });
})();

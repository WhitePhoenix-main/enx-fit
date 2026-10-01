(() => {
  'use strict';
  const clocks = [...document.querySelectorAll('[data-session-clock]')];
  const tick = () => clocks.forEach(node => {
    const end = node.dataset.sessionEnd ? Date.parse(node.dataset.sessionEnd) : Date.now();
    const seconds = Math.max(0, Math.floor((end - Date.parse(node.dataset.sessionClock)) / 1000));
    node.textContent = `${Math.floor(seconds / 3600).toString().padStart(2, '0')}:${Math.floor(seconds / 60 % 60).toString().padStart(2, '0')}:${(seconds % 60).toString().padStart(2, '0')}`;
  });
  tick(); if (clocks.some(node => !node.dataset.sessionEnd)) setInterval(tick, 1000);
  document.querySelectorAll('[data-local-time]').forEach(node => {
    node.textContent = new Date(node.dataset.localTime).toLocaleTimeString('ru', { hour: '2-digit', minute: '2-digit' });
  });
  document.addEventListener('click', event => {
    const trigger = event.target.closest('[data-replace-exercise],[data-add-exercise]');
    if (!trigger) return;
    const input = document.querySelector('[data-exercise-entry]');
    if (input) input.value = trigger.dataset.replaceExercise || '0';
    const heading = document.getElementById('exercise-dialog-title');
    if (heading) heading.textContent = trigger.dataset.replaceExercise ? 'Заменить упражнение' : 'Добавить упражнение';
    document.querySelectorAll('.exercise-menu[open]').forEach(menu => menu.open = false);
  });
  // One queue for the session avoids out-of-order writes when changing several sets quickly.
  let queue = Promise.resolve();
  const dirty = new Set();
  const save = form => {
    if (!form.reportValidity()) return;
    const values = new FormData(form);
    const version = (form.saveVersion || 0) + 1;
    form.saveVersion = version;
    dirty.add(form);
    const status = form.querySelector('.set-save-status');
    status.hidden = false; status.textContent = 'Сохраняем…'; status.classList.remove('is-error');
    queue = queue.then(async () => {
      try {
        const response = await fetch(form.action, { method: 'POST', body: values, headers: { 'X-Requested-With': 'XMLHttpRequest' } });
        const result = await response.json();
        if (!response.ok || !result.saved) throw new Error(result.error || 'Не удалось сохранить. Нажмите «Сохранить подход» повторно.');
        if (form.saveVersion === version) {
          dirty.delete(form); status.textContent = 'Сохранено';
          form.classList.toggle('is-completed', form.elements.completed.checked);
        }
      } catch (error) {
        status.classList.add('is-error');
        status.textContent = error.message.includes('JSON') ? 'Нет связи. Изменения не сохранены — повторите сохранение.' : error.message;
        form.querySelector('details').open = true;
      }
    });
  };
  document.querySelectorAll('.set-form').forEach(form => {
    form.addEventListener('input', () => dirty.add(form));
    form.addEventListener('change', () => save(form));
    form.addEventListener('submit', event => {
      if (event.submitter?.name === 'remove') { dirty.delete(form); return; }
      event.preventDefault(); save(form);
    });
  });
  document.querySelectorAll('form:not(.set-form)').forEach(form => form.addEventListener('submit', async event => {
    if (form.dataset.ready === 'true' || dirty.size === 0) return;
    event.preventDefault();
    const submitter = event.submitter;
    [...dirty].forEach(save);
    await queue;
    if (dirty.size > 0) { [...dirty][0].scrollIntoView({ block: 'center' }); return; }
    form.dataset.ready = 'true'; form.requestSubmit(submitter);
  }));
  window.addEventListener('beforeunload', event => { if (dirty.size) { event.preventDefault(); event.returnValue = ''; } });
})();

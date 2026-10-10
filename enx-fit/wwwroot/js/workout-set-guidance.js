(() => {
  'use strict';
  const root = document.querySelector('[data-active-workout]');
  if (!root) return;
  const refresh = () => root.querySelectorAll('.set-form').forEach(form => {
    const completed = form.elements.namedItem('completed')?.checked;
    const warmup = form.elements.namedItem('AddSet.IsWarmup')?.checked;
    const skipped = !completed && form.classList.contains('is-skipped');
    const label = form.querySelector('[data-set-result-state]');
    const text = completed ? 'Выполнен · фактические значения' : skipped ? 'Пропущен' : 'Ещё не выполнен';
    if (label && label.textContent !== text) label.textContent = text;
    const badge = form.querySelector('[data-set-warmup]');
    if (badge) badge.hidden = !warmup;
    const prompt = form.querySelector('[data-effort-prompt]');
    if (prompt) {
      const reserve = form.elements.namedItem('AddSet.Rir');
      prompt.hidden = !completed || warmup || form.dataset.effortNeeded !== 'true' || reserve.value !== '';
      prompt.querySelector('[data-add-effort]').hidden = false;
    }
  });
  root.addEventListener('input', () => queueMicrotask(refresh));
  root.addEventListener('change', () => queueMicrotask(refresh));
  root.addEventListener('workout-state', refresh);
  root.addEventListener('click', event => {
    const trigger = event.target.closest('[data-add-effort]');
    if (!trigger) return;
    const form = trigger.closest('.set-form');
    form.querySelector('.set-extra').open = true;
    form.elements.namedItem('AddSet.Rir').focus();
  });
  root.addEventListener('invalid', event => {
    const details = event.target.closest('.set-extra');
    if (details) details.open = true;
  }, true);
  const list = root.querySelector('.session-exercises');
  if (list) new MutationObserver(refresh).observe(list, {childList:true, subtree:true});
  refresh();
})();

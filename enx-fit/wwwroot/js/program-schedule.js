(() => {
  'use strict';
  const root = document.querySelector('[data-program-calendar]');
  if (!root) return;
  const content = root.querySelector('[data-schedule-content]');
  const host = root.querySelector('[data-schedule-details-host]');
  const groups = [...content.querySelectorAll('[data-details-day]')];
  const triggers = [...root.querySelectorAll('[data-schedule-day]')];
  const dialog = document.getElementById('schedule-day-dialog');
  const mobile = matchMedia('(max-width:1023px)');
  const canDialog = typeof dialog?.showModal === 'function';
  const week = root.querySelector('.schedule-week');
  let day = root.dataset.selectedDay || root.querySelector('.schedule-day.has-training.is-today')?.dataset.scheduleDay ||
    root.querySelector('.schedule-day.has-training')?.dataset.scheduleDay || root.dataset.today;
  if (!groups.some(group => group.dataset.detailsDay === day)) day = groups[0]?.dataset.detailsDay;
  const revealSelectedDay = () => requestAnimationFrame(() => {
    const selected = week.querySelector('.is-selected');
    if (!selected) return;
    const bounds = week.getBoundingClientRect(), item = selected.getBoundingClientRect();
    if (item.left < bounds.left) week.scrollLeft += item.left - bounds.left;
    else if (item.right > bounds.right) week.scrollLeft += item.right - bounds.right;
  });
  const select = value => {
    day = value;
    groups.forEach(group => { group.hidden = group.dataset.detailsDay !== day; });
    triggers.forEach(trigger => {
      const active = trigger.dataset.scheduleDay === day;
      trigger.classList.toggle('is-selected', active);
      if (active) trigger.setAttribute('aria-current', 'true'); else trigger.removeAttribute('aria-current');
    });
    revealSelectedDay();
  };
  const layout = () => {
    if (mobile.matches && canDialog) {
      dialog.querySelector('[data-schedule-dialog-host]').append(content); host.hidden = true;
    } else {
      if (dialog?.open) dialog.close(); host.append(content); host.hidden = false;
    }
  };
  triggers.forEach(trigger => trigger.addEventListener('click', event => {
    event.preventDefault(); select(trigger.dataset.scheduleDay);
    if (mobile.matches && canDialog) dialog.showModal();
  }));
  mobile.addEventListener('change', layout);
  window.addEventListener('resize', revealSelectedDay);
  root.classList.add('schedule-enhanced'); select(day); layout();
  document.querySelectorAll('.schedule-move form').forEach(form => {
    const input = form.querySelector('input[name="date"]');
    const choices = [...form.querySelectorAll('[data-date-choice]')];
    const summary = form.querySelector('[data-date-summary]');
    const original = form.closest('[data-appointment-date]').dataset.appointmentDate;
    const update = () => {
      choices.forEach(choice => choice.setAttribute('aria-pressed', String(choice.dataset.dateChoice === input.value)));
      summary.hidden = !input.value || !input.validity.valid || input.value === original;
      if (!summary.hidden) summary.textContent = 'Новая дата: '+new Intl.DateTimeFormat('ru-RU',{weekday:'long',day:'numeric',month:'long'}).format(new Date(input.value+'T12:00:00'));
    };
    choices.forEach(choice => choice.addEventListener('click', () => { input.value = choice.dataset.dateChoice; update(); }));
    input.addEventListener('change', update); update();
  });
})();

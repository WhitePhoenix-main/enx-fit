(() => {
  'use strict';
  document.querySelectorAll('[data-public-preview]').forEach(preview => {
    const tablist = preview.querySelector('[role="tablist"]');
    const tabs = [...preview.querySelectorAll('[data-preview-tab]')];
    const panels = [...preview.querySelectorAll('[data-preview-panel]')];
    if (!tablist || tabs.length !== panels.length) return;
    const select = (tab, focus = false) => {
      tabs.forEach(item => {
        const active = item === tab;
        item.setAttribute('aria-selected', String(active));
        item.tabIndex = active ? 0 : -1;
      });
      panels.forEach(panel => {
        const active = panel.dataset.previewPanel === tab.dataset.previewTab;
        panel.open = active;
        panel.hidden = !active;
      });
      if (focus) tab.focus();
    };
    panels.forEach(panel => {
      panel.setAttribute('role', 'tabpanel');
      panel.setAttribute('aria-labelledby', 'preview-tab-' + panel.dataset.previewPanel);
      panel.tabIndex = 0;
    });
    tabs.forEach((tab, index) => {
      tab.addEventListener('click', () => select(tab));
      tab.addEventListener('keydown', event => {
        let next;
        if (event.key === 'ArrowRight') next = (index + 1) % tabs.length;
        else if (event.key === 'ArrowLeft') next = (index + tabs.length - 1) % tabs.length;
        else if (event.key === 'Home') next = 0;
        else if (event.key === 'End') next = tabs.length - 1;
        else return;
        event.preventDefault(); select(tabs[next], true);
      });
    });
    select(tabs[0]);
    preview.classList.add('is-interactive');
    tablist.hidden = false;
  });
  const copy = document.querySelector('[data-copy-support]');
  const address = document.querySelector('#support-address');
  const feedback = document.querySelector('[data-copy-feedback]');
  if (copy && address && feedback) {
    copy.hidden = false;
    copy.addEventListener('click', async () => {
      copy.disabled = true;
      feedback.textContent = 'Копируем адрес…';
      window.AppComponents?.setNoticeState(feedback, 'pending');
      try {
        if (!navigator.clipboard?.writeText) throw new Error('Clipboard unavailable');
        await navigator.clipboard.writeText(address.value);
        feedback.textContent = 'Адрес скопирован.';
        window.AppComponents?.setNoticeState(feedback, 'saved');
      } catch {
        address.focus(); address.select();
        feedback.textContent = 'Адрес выделен. Скопируй его вручную.';
        window.AppComponents?.setNoticeState(feedback, 'error');
      } finally {
        copy.disabled = false;
      }
    });
  }
})();

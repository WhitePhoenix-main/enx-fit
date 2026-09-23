(() => {
  'use strict';
  document.addEventListener('click', event => {
    document.querySelectorAll('.program-menu[open]').forEach(menu => {
      if (!menu.contains(event.target)) menu.open = false;
    });
  });
  document.querySelectorAll('form[data-busy]').forEach(form => form.addEventListener('submit', event => {
    if (!form.checkValidity()) return;
    const button = event.submitter;
    if (button) {
      // Keep the submitter's name/value in the POST.
      button.setAttribute('aria-busy', 'true');
      setTimeout(() => { button.disabled = true; }, 0);
    }
  }));
  window.addEventListener('pageshow', () => document.querySelectorAll('[aria-busy=true]').forEach(button => {
    button.disabled = false; button.removeAttribute('aria-busy');
  }));
  const editor = document.querySelector('[data-program-editor]');
  if (editor) {
    const key = `program-editor-scroll:${location.pathname}`;
    try { const y = sessionStorage.getItem(key); if (y) { window.scrollTo(0, Number(y)); sessionStorage.removeItem(key); } } catch {}
    editor.addEventListener('submit', event => {
      if (event.submitter?.formAction.includes('handler=Structure')) {
        try { sessionStorage.setItem(key, String(window.scrollY)); } catch {}
      }
    });
  }
})();

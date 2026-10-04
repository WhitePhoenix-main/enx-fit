(() => {
  'use strict';
  document.documentElement.classList.add('landing-ready');
  const button = document.querySelector('.menu-toggle');
  const navigation = document.querySelector('.landing-nav');
  const close = (restoreFocus = false) => {
    const open = button.getAttribute('aria-expanded') === 'true';
    button.setAttribute('aria-expanded', 'false');
    button.setAttribute('aria-label', 'Открыть меню');
    navigation.classList.remove('is-open');
    if (open && restoreFocus) button.focus();
  };
  button.addEventListener('click', () => {
    const open = button.getAttribute('aria-expanded') !== 'true';
    button.setAttribute('aria-expanded', String(open));
    button.setAttribute('aria-label', open ? 'Закрыть меню' : 'Открыть меню');
    navigation.classList.toggle('is-open', open);
  });
  navigation.querySelectorAll('a').forEach(link => link.addEventListener('click', () => close()));
  document.addEventListener('keydown', event => { if (event.key === 'Escape') close(true); });
  document.addEventListener('click', event => {
    if (!button.contains(event.target) && !navigation.contains(event.target)) close();
  });
  matchMedia('(min-width: 1101px)').addEventListener('change', event => { if (event.matches) close(); });
})();

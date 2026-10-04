(() => {
  'use strict';
  document.documentElement.classList.add('landing-ready');
  const menu = document.querySelector('[data-public-menu]');
  const summary = menu?.querySelector('summary');
  const close = restore => {
    if (!menu?.open) return;
    menu.open = false;
    if (restore) summary.focus();
  };
  menu?.querySelectorAll('a').forEach(link => link.addEventListener('click', () => close(false)));
  document.addEventListener('keydown', event => {
    if (event.key === 'Escape' && menu?.open) { event.preventDefault(); close(true); }
  });
  document.addEventListener('click', event => { if (!menu?.contains(event.target)) close(false); });
  document.addEventListener('focusin', event => { if (!menu?.contains(event.target)) close(false); });
  const openLinkedAnswer = () => {
    const target = document.getElementById(location.hash.slice(1));
    if (target?.matches('.faq-items details')) target.open = true;
  };
  window.addEventListener('hashchange', openLinkedAnswer);
  openLinkedAnswer();
  const header = document.querySelector('.landing-header');
  let frame = 0;
  const update = () => { header?.classList.toggle('is-scrolled', scrollY > 40); frame = 0; };
  window.addEventListener('scroll', () => { if (!frame) frame = requestAnimationFrame(update); }, { passive:true });
  update();
})();

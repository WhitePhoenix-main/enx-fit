(() => {
  const form = document.querySelector('[data-workout-filters]');
  if (!form) return;
  const search = form.querySelector('input[type="search"]');
  const program = form.querySelector('select');
  let timer;
  const submit = () => {
    clearTimeout(timer);
    form.requestSubmit();
  };
  program.addEventListener('change', submit);
  // Preserve typing focus and the cursor through the server-rendered filter update.
  form.addEventListener('submit', () => {
    if (document.activeElement === search) {
      sessionStorage.setItem('enix-workout-search-focus', 'true');
    }
  });
  search.addEventListener('input', () => {
    clearTimeout(timer);
    timer = setTimeout(submit, 650);
  });
  if (sessionStorage.getItem('enix-workout-search-focus')) {
    sessionStorage.removeItem('enix-workout-search-focus');
    search.focus({ preventScroll: true });
    const length = search.value.length;
    search.setSelectionRange(length, length);
  }
})();

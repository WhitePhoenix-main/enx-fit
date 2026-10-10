(() => {
  'use strict';
  const tabButtons = [...document.querySelectorAll('[data-mobile-tab]')];
  const selectTab = button => {
    tabButtons.forEach(tab => {
      const selected = tab === button;
      tab.classList.toggle('active', selected);
      tab.setAttribute('aria-selected', String(selected));
      tab.tabIndex = selected ? 0 : -1;
      document.getElementById(tab.dataset.mobileTab).hidden = !selected;
    });
  };
  tabButtons.forEach((button, index) => {
    button.addEventListener('click', () => selectTab(button));
    button.addEventListener('keydown', event => {
      if (!['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) return;
      event.preventDefault();
      const next = event.key === 'Home' ? 0 : event.key === 'End' ? tabButtons.length - 1 : (index + (event.key === 'ArrowRight' ? 1 : -1) + tabButtons.length) % tabButtons.length;
      selectTab(tabButtons[next]);
      tabButtons[next].focus();
    });
  });
  if (tabButtons.length && new URLSearchParams(location.search).has('Month') && location.hash !== '#mobile-history') selectTab(tabButtons[1]);
  document.querySelectorAll('[data-current-timezone="false"]').forEach(label => {
    const zone = Intl.DateTimeFormat().resolvedOptions().timeZone;
    label.textContent = zone === 'Asia/Irkutsk' ? 'Иркутск (UTC+8)' : zone;
  });

  const library = document.querySelector('.em-library[data-exercise-library]');
  if (!library) return;
  const cards = [...library.querySelectorAll('[data-library-id]')];
  const search = library.querySelector('[data-library-search]');
  const group = library.querySelector('[data-library-group]');
  const tabs = [...library.querySelectorAll('[data-library-tab]')];
  const reference = library.dataset.libraryReference === 'true';
  const key = `enix-exercise-favorites:${library.dataset.libraryOwner}:${reference ? 'reference' : 'live'}`;
  let favorites;
  try { const saved = JSON.parse(localStorage.getItem(key)); favorites = new Set((Array.isArray(saved) ? saved : reference ? [-1] : []).map(window.ExerciseIds.normalize).filter(Boolean)); }
  catch { favorites = new Set(reference ? [window.ExerciseIds.normalize(-1)] : []); }
  let activeTab = 'all';
  const render = () => {
    const query = search.value.trim().toLocaleLowerCase('ru');
    let count = 0;
    cards.forEach(card => {
      const favorite = favorites.has(card.dataset.libraryId);
      const button = card.querySelector('[data-library-favorite]');
      button.setAttribute('aria-pressed', String(favorite));
      button.setAttribute('aria-label', `${favorite ? 'Убрать из избранного' : 'Добавить в избранное'}: ${card.querySelector('h2').textContent}`);
      card.classList.toggle('is-favorite', favorite);
      card.hidden = !card.dataset.libraryText.toLocaleLowerCase('ru').includes(query) || (group.value && card.dataset.libraryMuscle !== group.value) || (activeTab === 'favorites' && !favorite) || (activeTab === 'recent' && card.dataset.libraryRecent !== 'true');
      if (!card.hidden) count++;
    });
    library.querySelector('[data-library-empty]').hidden = count > 0;
    const plural = count % 100 >= 11 && count % 100 <= 14 ? 'упражнений' : count % 10 === 1 ? 'упражнение' : count % 10 >= 2 && count % 10 <= 4 ? 'упражнения' : 'упражнений';
    library.querySelector('[data-library-count]').textContent = reference && !query && !group.value && activeTab === 'all' ? '24 упражнения' : `${count} ${plural}`;
  };
  search.addEventListener('input', render);
  group.addEventListener('change', render);
  tabs.forEach((button, index) => {
    const select = () => { activeTab = button.dataset.libraryTab; tabs.forEach(tab => { const selected = tab === button; tab.classList.toggle('active', selected); tab.setAttribute('aria-selected', String(selected)); tab.tabIndex = selected ? 0 : -1; }); render(); };
    button.addEventListener('click', select);
    button.addEventListener('keydown', event => {
      if (!['ArrowLeft', 'ArrowRight'].includes(event.key)) return;
      event.preventDefault();
      const next = tabs[(index + (event.key === 'ArrowRight' ? 1 : -1) + tabs.length) % tabs.length];
      next.click(); next.focus();
    });
  });
  cards.forEach(card => card.querySelector('[data-library-favorite]').addEventListener('click', () => {
    const id = card.dataset.libraryId;
    if (favorites.has(id)) favorites.delete(id); else favorites.add(id);
    try { localStorage.setItem(key, JSON.stringify([...favorites])); } catch { /* Filtering remains usable without storage. */ }
    render();
  }));
  render();
})();

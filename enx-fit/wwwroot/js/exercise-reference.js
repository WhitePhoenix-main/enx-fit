(() => {
    const root = document.querySelector('[data-ref-library]');
    if (!root) return;
    const cards = [...root.querySelectorAll('[data-ref-exercise]')];
    let ordered = cards;
    const grid = root.querySelector('[data-ref-exercise-grid]');
    const popular = root.classList.contains('ref-library-free') ? document.createElement('div') : null;
    if (popular) { popular.className = 'ref-popular-grid'; grid.before(popular); }
    const search = root.querySelector('[data-ref-search]');
    const equipment = root.querySelector('[data-ref-equipment]');
    const key = `enix-exercise-favorites:${root.dataset.viewer}:live`;
    let favorites = new Set();
    try { const saved = JSON.parse(localStorage.getItem(key)); if (Array.isArray(saved)) favorites = new Set(saved.map(String)); } catch {}
    let muscle = '', tab = 'all';
    const normalize = value => value.toLocaleLowerCase('ru').replaceAll('ё', 'е');
    const compared = new Set();
    root.querySelectorAll('[data-ref-assign]').forEach(button => button.addEventListener('click', () => {
        document.querySelector('[data-ref-assign-id]').value = button.dataset.refAssign;
        document.querySelector('[data-ref-assign-caption]').textContent = button.dataset.refAssignName;
        const client = root.querySelector('[data-ref-client]');
        if (client) document.querySelector('#assign-exercise-dialog select[name=clientId]').value = client.value;
    }));
    function render() {
        const query = normalize(search.value.trim());
        let count = 0;
        cards.forEach(card => {
            const favorite = favorites.has(card.dataset.refExercise);
            card.querySelector('[data-ref-favorite]').setAttribute('aria-pressed', String(favorite));
            let group = normalize(card.dataset.muscle);
            if (/квадриц|ягод|бедр|икр/.test(group)) group += ' ног';
            if (/бицеп|трицеп|предплеч/.test(group)) group += ' рук';
            if (/живот|мышцы кора/.test(group)) group += ' пресс';
            const gear = normalize(card.dataset.equipment);
            const gearMatches = !equipment.value || (equipment.value === 'вес тела'
                ? /вес тела|свой вес|собственн|без оборудования/.test(gear)
                : gear.includes(equipment.value));
            card.hidden = !normalize(`${card.dataset.name} ${card.dataset.muscle} ${card.dataset.equipment}`).includes(query)
                || (muscle && !group.includes(muscle)) || !gearMatches
                || (tab === 'favorites' && !favorite) || (tab === 'recent' && card.dataset.recent !== 'true');
            if (!card.hidden) count++;
        });
        root.querySelector('[data-ref-count]').textContent = `${count}`;
        root.querySelector('[data-ref-empty]').hidden = count !== 0;
        const visible = ordered.filter(card => !card.hidden);
        cards.forEach(card => card.classList.remove('is-featured'));
        visible.slice(0,4).forEach(card => card.classList.add('is-featured'));
        if (popular) {
            ordered.forEach(card => grid.append(card));
            visible.slice(0,4).forEach(card => popular.append(card));
            popular.hidden = visible.length === 0;
        }
        const heading = root.querySelector('[data-ref-library-subheading]');
        if (heading) { heading.hidden = visible.length < 5; if (visible[4]) visible[4].before(heading); }
    }
    root.querySelectorAll('[data-ref-muscle]').forEach(button => button.addEventListener('click', () => {
        muscle = button.dataset.refMuscle;
        root.querySelectorAll('[data-ref-muscle]').forEach(b => b.setAttribute('aria-pressed', String(b === button)));
        render();
    }));
    root.querySelectorAll('[data-ref-tab]').forEach(button => button.addEventListener('click', () => {
        tab = button.dataset.refTab;
        root.querySelectorAll('[data-ref-tab]').forEach(b => b.setAttribute('aria-pressed', String(b === button)));
        render();
    }));
    cards.forEach(card => card.querySelector('[data-ref-favorite]').addEventListener('click', () => {
        const id = card.dataset.refExercise;
        favorites.has(id) ? favorites.delete(id) : favorites.add(id);
        try { localStorage.setItem(key, JSON.stringify([...favorites])); } catch {}
        render();
    }));
    search.addEventListener('input', render); equipment.addEventListener('change', render);
    root.querySelector('[data-ref-reset]').addEventListener('click', () => { search.value = ''; equipment.value = ''; muscle = ''; tab = 'all'; root.querySelector('[data-ref-muscle=""]').click(); });
    root.querySelector('[data-ref-sidebar-reset]')?.addEventListener('click', () => root.querySelector('[data-ref-reset]').click());
    root.querySelector('[data-ref-sort]').addEventListener('change', event => {
        const sorted = event.target.value === 'name' ? [...cards].sort((a,b) => a.dataset.name.localeCompare(b.dataset.name, 'ru')) : cards;
        ordered = sorted;
        sorted.forEach(card => root.querySelector('[data-ref-exercise-grid]').append(card));
        render();
    });
    const updateComparison = () => {
        root.querySelectorAll('[data-ref-compare-count]').forEach(n => n.textContent = compared.size);
        const bar = root.querySelector('[data-ref-compare-bar]'); if (bar) bar.hidden = !compared.size;
    };
    cards.forEach(card => card.querySelector('[data-ref-compare]')?.addEventListener('change', event => {
        if (event.target.checked && compared.size >= 3) { event.target.checked = false; return; }
        event.target.checked ? compared.add(card) : compared.delete(card); updateComparison();
    }));
    root.querySelector('[data-ref-compare-reset]')?.addEventListener('click', () => { compared.clear(); cards.forEach(c => { const box = c.querySelector('[data-ref-compare]'); if (box) box.checked = false; }); updateComparison(); });
    root.querySelectorAll('[data-ref-compare-open]').forEach(button => button.addEventListener('click', () => {
        const host = document.querySelector('[data-ref-compare-content]'); host.replaceChildren();
        if (compared.size < 2) { const p = document.createElement('p'); p.textContent = 'Выберите от двух до трёх упражнений для сравнения.'; host.append(p); }
        else { const table = document.createElement('table'); table.className = 'ref-compare-table';
            [['Упражнение', 'Мышцы', 'Оборудование'], ...[...compared].map(c => [c.dataset.name,c.dataset.muscle,c.dataset.equipment])].forEach((row,i) => { const tr = table.insertRow(); row.forEach(text => { const cell = document.createElement(i ? 'td' : 'th'); cell.textContent = text; tr.append(cell); }); }); host.append(table); }
        document.getElementById('exercise-comparison').showModal();
    }));
    render();
})();

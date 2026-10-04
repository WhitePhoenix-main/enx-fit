(() => {
  'use strict';
  document.addEventListener('click', event => {
    const link = event.target.closest('a[data-dialog^="progress-"]');
    if (link) event.preventDefault();
    document.querySelectorAll('.epr-period[open]').forEach(menu => {
      if (!menu.contains(event.target)) menu.open = false;
    });
  });
  document.addEventListener('keydown', event => {
    if (event.key === 'Escape') document.querySelectorAll('.epr-period[open]').forEach(menu => { menu.open = false; });
  });
  const workspace = document.querySelector('[data-progress-workspace]');
  if (workspace) {
    const mobile = matchMedia('(max-width:767px)');
    const tabs = [...workspace.querySelectorAll('[data-progress-metric]')];
    const panels = [...workspace.querySelectorAll('[data-progress-panel]')];
    const selectMetric = (metric, animate = false) => {
      if (!tabs.some(tab => tab.dataset.progressMetric === metric)) return;
      workspace.dataset.metric = metric;
      tabs.forEach(tab => {
        const selected = tab.dataset.progressMetric === metric;
        tab.setAttribute('aria-selected', String(selected));
        tab.tabIndex = selected ? 0 : -1;
      });
      panels.forEach(panel => {
        const selected = panel.dataset.progressPanel === metric;
        panel.classList.remove('is-entering');
        if (mobile.matches) {
          panel.setAttribute('role', 'tabpanel');
          panel.setAttribute('aria-labelledby', `pw-tab-${panel.dataset.progressPanel}`);
          panel.setAttribute('aria-hidden', String(!selected));
          if (selected && animate) panel.classList.add('is-entering');
        } else {
          panel.removeAttribute('role');
          panel.removeAttribute('aria-hidden');
          panel.setAttribute('aria-labelledby', `pw-${panel.dataset.progressPanel}-title`);
        }
      });
    };
    tabs.forEach((tab, index) => {
      tab.addEventListener('click', event => {
        if (!mobile.matches || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey || event.button !== 0) return;
        event.preventDefault();
        selectMetric(tab.dataset.progressMetric, true);
        const url = new URL(location.href);
        url.searchParams.set('ProgressMetric', tab.dataset.progressMetric);
        history.replaceState(null, '', url);
      });
      tab.addEventListener('keydown', event => {
        if (!mobile.matches || !['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) return;
        event.preventDefault();
        const next = event.key === 'Home' ? 0 : event.key === 'End' ? tabs.length - 1 : (index + (event.key === 'ArrowRight' ? 1 : -1) + tabs.length) % tabs.length;
        tabs[next].click();
        tabs[next].focus();
      });
    });
    mobile.addEventListener('change', () => selectMetric(workspace.dataset.metric));
    selectMetric(workspace.dataset.metric);
  }
  document.querySelectorAll('[data-progress-chart]').forEach(chart => {
    const svg = chart.querySelector('svg');
    const originalViewBox = svg.getAttribute('viewBox').split(/\s+/).map(Number);
    const geometry = [...svg.querySelectorAll('[x],[x1],[x2],[cx],[y],[y1],[y2],[cy],path[d]')].map(element => ({ element,
      coordinates: ['x', 'x1', 'x2', 'cx', 'y', 'y1', 'y2', 'cy'].filter(name => element.hasAttribute(name)).map(name => [name, Number(element.getAttribute(name))]),
      path: element.getAttribute('d')
    }));
    const resize = () => {
      const responsive = window.innerWidth < 768 || chart.closest('[data-progress-workspace]');
      const width = responsive ? svg.clientWidth : originalViewBox[2];
      const height = responsive ? svg.clientHeight : originalViewBox[3];
      if (width <= 0 || height <= 0) return;
      const ratioX = width / originalViewBox[2];
      const ratioY = height / originalViewBox[3];
      svg.setAttribute('viewBox', `0 0 ${width} ${height}`);
      geometry.forEach(({ element, coordinates, path }) => {
        coordinates.forEach(([name, value]) => element.setAttribute(name, value * (['x', 'x1', 'x2', 'cx'].includes(name) ? ratioX : ratioY)));
        if (path) element.setAttribute('d', path.replace(/([ML])([\d.]+),([\d.]+)/g, (_, command, x, y) => `${command}${Number(x) * ratioX},${Number(y) * ratioY}`));
      });
    };
    new ResizeObserver(resize).observe(svg);
    resize();
    const tooltip = chart.querySelector('output');
    svg.addEventListener('click', event => {
      if (event.target.matches('[data-chart-point]')) return;
      const closest = [...chart.querySelectorAll('[data-chart-point]')].map(point => {
        const box = point.getBoundingClientRect(); return {point,distance:Math.hypot(event.clientX-box.left-box.width/2,event.clientY-box.top-box.height/2)};
      }).sort((a,b)=>a.distance-b.distance)[0];
      if (closest?.distance <= 24) closest.point.dispatchEvent(new MouseEvent('click', {bubbles:false}));
    });
    const hide = () => { tooltip.hidden = true; };
    const points = [...chart.querySelectorAll('[data-chart-point]')];
    points.forEach((point, index) => {
      const select = () => {
        const result = chart.querySelector('[data-progress-result]');
        if (!result) return;
        points.forEach(other => other.setAttribute('aria-pressed', String(other === point)));
        result.hidden = false;
        result.querySelector(':scope > span').textContent = point.dataset.detail || `${point.dataset.date} · ${point.dataset.value} кг${point.dataset.reps ? ` × ${point.dataset.reps}` : ''}`;
        const link = result.querySelector('a');
        link.hidden = !point.dataset.workoutUrl;
        if (point.dataset.workoutUrl) link.href = point.dataset.workoutUrl;
        result.querySelector('[data-point-link-label]').textContent = point.dataset.linkLabel || 'Открыть тренировку';
      };
      point.addEventListener('click', select);
      point.addEventListener('keydown', event => {
        if (['Enter', ' '].includes(event.key)) { event.preventDefault(); select(); }
        else if (['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) {
          event.preventDefault();
          const next = event.key === 'Home' ? 0 : event.key === 'End' ? points.length - 1 : Math.max(0, Math.min(points.length - 1, index + (event.key === 'ArrowRight' ? 1 : -1)));
          points[next].focus(); points[next].dispatchEvent(new MouseEvent('click'));
        }
      });
      const show = () => {
        tooltip.textContent = point.dataset.detail || `${point.dataset.date} · ${point.dataset.value}`;
        tooltip.hidden = false;
        const bounds = chart.getBoundingClientRect();
        const position = point.getBoundingClientRect();
        tooltip.style.left = `${Math.max(0, Math.min(bounds.width - tooltip.offsetWidth, position.left - bounds.left - tooltip.offsetWidth / 2))}px`;
        tooltip.style.top = `${Math.max(0, position.top - bounds.top - tooltip.offsetHeight - 10)}px`;
      };
      point.addEventListener('mouseenter', show);
      point.addEventListener('mouseleave', hide);
      point.addEventListener('focus', show);
      point.addEventListener('blur', hide);
    });
    if (chart.dataset.autoSelect === 'true') points.at(-1)?.dispatchEvent(new MouseEvent('click'));
  });
})();

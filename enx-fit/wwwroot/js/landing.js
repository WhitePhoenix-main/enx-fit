(() => {
  'use strict';

  const menuButton = document.querySelector('.menu-toggle');
  const navigation = document.querySelector('.landing-nav');
  const closeMenu = () => {
    menuButton.setAttribute('aria-expanded', 'false');
    menuButton.setAttribute('aria-label', 'Открыть меню');
    navigation.classList.remove('is-open');
  };
  menuButton.addEventListener('click', () => {
    const open = menuButton.getAttribute('aria-expanded') !== 'true';
    menuButton.setAttribute('aria-expanded', String(open));
    menuButton.setAttribute('aria-label', open ? 'Закрыть меню' : 'Открыть меню');
    navigation.classList.toggle('is-open', open);
  });
  navigation.querySelectorAll('a').forEach(link => link.addEventListener('click', closeMenu));
  document.addEventListener('keydown', event => {
    if (event.key === 'Escape') closeMenu();
  });

  const billingSwitch = document.getElementById('billing-switch');
  billingSwitch.addEventListener('click', () => {
    const yearly = billingSwitch.getAttribute('aria-checked') !== 'true';
    billingSwitch.setAttribute('aria-checked', String(yearly));
    document.querySelectorAll('[data-monthly]').forEach(price => {
      price.textContent = yearly ? price.dataset.yearly : price.dataset.monthly;
    });
  });

  // Illustrative values are shared by the drawn curve, tooltip and keyboard controls.
  const metrics = {
    volume: { label: 'тренировочного объёма', min: 0, max: 20000, scale: ['20K', '15K', '10K', '5K', '0'], values: [3200, 4800, 5200, 7600, 7400, 10200, 12000, 16420, 15700, 17700, 18200, 19620], format: value => `${value.toLocaleString('ru-RU')} кг` },
    weight: { label: 'рабочего веса', min: 40, max: 120, scale: ['120', '100', '80', '60', '40'], values: [66, 70, 74, 72, 80, 84, 83, 94.2, 96, 100, 103, 110], format: value => `${value} кг` },
    reps: { label: 'количества повторений', min: 0, max: 400, scale: ['400', '300', '200', '100', '0'], values: [90, 130, 120, 175, 150, 210, 195, 248, 225, 290, 275, 340], format: value => `${value} повторов` },
    time: { label: 'времени тренировок', min: 0, max: 360, scale: ['6 ч', '4.5 ч', '3 ч', '1.5 ч', '0'], values: [105, 120, 150, 130, 180, 185, 230, 272, 250, 290, 305, 330], format: value => `${Math.floor(value / 60)} ч ${value % 60} мин` }
  };
  const chart = document.getElementById('progress-chart');
  const chartLine = document.getElementById('chart-line');
  const period = document.getElementById('chart-period');
  let selectedMetric = 'volume';
  let selectedPoint = 7;
  let chartPoints = [];
  chart.setAttribute('role', 'slider');
  chart.setAttribute('tabindex', '0');
  chart.setAttribute('aria-describedby', 'chart-help');

  const showChartPoint = index => {
    selectedPoint = Math.max(0, Math.min(chartPoints.length - 1, index));
    const metric = metrics[selectedMetric];
    const week = 13 - chartPoints.length + selectedPoint;
    const value = metric.values[week - 1];
    const previous = metric.values[week - 2];
    const change = previous ? Math.round((value - previous) / previous * 100) : 0;
    const [x, y] = chartPoints[selectedPoint];
    document.getElementById('tooltip-period').textContent = `Неделя ${week}`;
    document.getElementById('tooltip-value').textContent = metric.format(value);
    document.getElementById('tooltip-change').textContent = previous ? `${change >= 0 ? '+' : ''}${change}%` : '—';
    document.querySelector('.chart-tooltip > span:last-child').lastChild.textContent = previous ? ' к предыдущей' : ' первая неделя';
    document.querySelectorAll('.chart-point, .chart-point-ring').forEach(point => {
      point.setAttribute('cx', x);
      point.setAttribute('cy', y);
    });
    document.querySelector('.chart-guide').setAttribute('d', `M${x} ${y}V160`);
    document.querySelector('.chart-tooltip').style.left = `${Math.max(0, Math.min(65, x / 480 * 100 - 14))}%`;
    chart.setAttribute('aria-valuenow', String(week));
    chart.setAttribute('aria-valuetext', `Неделя ${week}: ${metric.format(value)}`);
  };
  const updateChart = (animate = true) => {
    const metric = metrics[selectedMetric];
    const count = Number(period.value);
    const values = metric.values.slice(-count);
    chartPoints = values.map((value, index) => [index * 480 / (count - 1), 160 - (value - metric.min) / (metric.max - metric.min) * 160]);
    let path = `M${chartPoints[0][0]} ${chartPoints[0][1]}`;
    for (let index = 1; index < chartPoints.length; index++) {
      const previous = chartPoints[index - 1];
      const current = chartPoints[index];
      const middle = (previous[0] + current[0]) / 2;
      path += ` C${middle} ${previous[1]} ${middle} ${current[1]} ${current[0]} ${current[1]}`;
    }
    chartLine.setAttribute('d', path);
    document.getElementById('chart-area').setAttribute('d', `${path} L480 160 H0Z`);
    chart.setAttribute('aria-label', `Выбор недели: график ${metric.label}`);
    chart.setAttribute('aria-valuemin', String(13 - count));
    chart.setAttribute('aria-valuemax', '12');
    showChartPoint(count === 12 ? 7 : 1);
    const scale = document.getElementById('chart-scale');
    scale.replaceChildren(...metric.scale.map(value => {
      const label = document.createElement('span');
      label.textContent = value;
      return label;
    }));
    const labels = document.getElementById('chart-labels');
    labels.replaceChildren(...Array.from({ length: count }, (_, index) => {
      const label = document.createElement('span');
      label.textContent = String(13 - count + index);
      return label;
    }));
    if (animate) chart.dispatchEvent(new Event('enix:chart-change'));
  };
  const selectPointFromPointer = event => {
    const bounds = chart.getBoundingClientRect();
    showChartPoint(Math.round((event.clientX - bounds.left) / bounds.width * (chartPoints.length - 1)));
  };
  chart.addEventListener('pointermove', event => {
    if (event.pointerType !== 'touch') selectPointFromPointer(event);
  });
  chart.addEventListener('pointerdown', selectPointFromPointer);
  chart.addEventListener('keydown', event => {
    const directions = { ArrowRight: 1, ArrowUp: 1, ArrowLeft: -1, ArrowDown: -1 };
    if (event.key in directions) showChartPoint(selectedPoint + directions[event.key]);
    else if (event.key === 'Home') showChartPoint(0);
    else if (event.key === 'End') showChartPoint(chartPoints.length - 1);
    else return;
    event.preventDefault();
  });
  document.querySelectorAll('[data-metric]').forEach(button => {
    button.addEventListener('click', () => {
      selectedMetric = button.dataset.metric;
      document.querySelectorAll('[data-metric]').forEach(tab => {
        const active = tab === button;
        tab.classList.toggle('active', active);
        tab.setAttribute('aria-pressed', String(active));
      });
      updateChart();
    });
  });
  period.addEventListener('change', () => updateChart());
  updateChart(false);

  // The hero is a small working product preview, with the same controls on touch and keyboard.
  const heroBars = [...document.querySelectorAll('[data-bar-value]')];
  const heroTooltip = document.getElementById('hero-bar-tooltip');
  const showHeroBar = button => {
    heroBars.forEach(bar => bar.setAttribute('aria-pressed', String(bar === button)));
    heroTooltip.querySelector('b').textContent = `${Number(button.dataset.barValue).toLocaleString('ru-RU')} кг`;
    heroTooltip.querySelector('span').textContent = `${button.dataset.barDate} · тренировочный объём`;
  };
  heroBars.forEach((button, index) => {
    button.addEventListener('pointerenter', event => {
      if (event.pointerType !== 'touch') showHeroBar(button);
    });
    button.addEventListener('focus', () => showHeroBar(button));
    button.addEventListener('click', () => showHeroBar(button));
    button.addEventListener('keydown', event => {
      let next;
      if (event.key === 'ArrowRight') next = (index + 1) % heroBars.length;
      else if (event.key === 'ArrowLeft') next = (index - 1 + heroBars.length) % heroBars.length;
      else if (event.key === 'Home') next = 0;
      else if (event.key === 'End') next = heroBars.length - 1;
      else return;
      event.preventDefault();
      heroBars[next].focus();
    });
  });

  const dayNames = { 'Пн': 'Понедельник', 'Вт': 'Вторник', 'Ср': 'Среда', 'Чт': 'Четверг', 'Пт': 'Пятница', 'Сб': 'Суббота', 'Вс': 'Воскресенье' };
  document.querySelectorAll('[data-day]').forEach(day => {
    day.addEventListener('click', () => {
      document.querySelectorAll('[data-day]').forEach(item => item.setAttribute('aria-pressed', String(item === day)));
      const detail = day.dataset.detail ? ` · ${day.dataset.detail}` : '';
      const status = day.dataset.completed === 'true' ? ' · Выполнено ✓' : day.dataset.workout === 'Отдых' ? ' · Время восстановиться' : ' · Запланировано';
      document.getElementById('plan-selection').textContent = `${dayNames[day.dataset.day]} · ${day.dataset.workout}${detail}${status}`;
    });
  });

  const infoDialog = document.getElementById('info-dialog');
  const showInfo = (title, description) => {
    document.getElementById('info-title').textContent = title;
    document.getElementById('info-description').textContent = description;
    infoDialog.showModal();
  };
  const information = {
    goals: ['Постановка целей', 'Начни с измеримой цели: нужного рабочего веса, количества тренировок или изменения параметров тела. Записывай результаты и сравнивай их с тем, к чему стремишься.'],
    achievements: ['Каждая тренировка — шаг вперёд', 'Сохраняй историю тренировок, замечай личные рекорды и следи за регулярностью занятий. Посмотри свою динамику в разделе аналитики.'],
    pro: ['Enix Fit Pro', 'Расширенный тариф готовится к запуску. Сейчас можно бесплатно создать аккаунт и начать вести дневник тренировок. Оплата на этой странице не принимается.'],
    team: ['Enix Fit для команды', 'Возможности для тренеров и клубов готовятся к запуску. Пока ты можешь создать личный аккаунт и познакомиться с дневником тренировок и аналитикой.'],
    support: ['Поддержка Enix Fit', 'Раздел поддержки готовится к запуску. В личном кабинете уже доступны дневник тренировок, упражнения и показатели тела.'],
    privacy: ['Конфиденциальность', 'Политика конфиденциальности готовится к публикации. На главной странице показаны демонстрационные данные, а не данные реальных пользователей.'],
    terms: ['Условия использования', 'Полные условия использования готовятся к публикации. Тарифы на этой странице представлены для ознакомления; подключение платных подписок пока недоступно.'],
    social: ['Будем на связи', 'Официальные страницы Enix Fit в социальных сетях скоро появятся здесь.']
  };
  document.querySelectorAll('[data-info]').forEach(button => {
    button.addEventListener('click', () => showInfo(...information[button.dataset.info]));
  });
  document.querySelectorAll('[data-integration]').forEach(button => {
    button.addEventListener('click', () => showInfo(button.dataset.integration, 'Синхронизация с этим сервисом готовится к запуску. Сейчас тренировки и показатели можно добавлять вручную в личном кабинете.'));
  });

  const demoDialog = document.getElementById('demo-dialog');
  document.querySelectorAll('[data-demo]').forEach(button => button.addEventListener('click', () => demoDialog.showModal()));
  document.querySelectorAll('[data-close-dialog]').forEach(button => button.addEventListener('click', () => button.closest('dialog').close()));
  document.querySelectorAll('dialog').forEach(dialog => {
    dialog.addEventListener('click', event => {
      const bounds = dialog.getBoundingClientRect();
      if (event.target === dialog && (event.clientX < bounds.left || event.clientX > bounds.right || event.clientY < bounds.top || event.clientY > bounds.bottom)) dialog.close();
    });
  });
  let setCount = 3;
  const addSetButton = document.getElementById('add-demo-set');
  addSetButton.addEventListener('click', () => {
    if (setCount >= 5) return;
    setCount++;
    const row = document.createElement('div');
    [String(setCount), '70 кг', '8'].forEach((text, index) => {
      const cell = document.createElement(index === 0 ? 'span' : 'b');
      cell.textContent = text;
      row.append(cell);
    });
    document.getElementById('demo-sets').append(row);
    const volume = (1930 + (setCount - 3) * 560).toLocaleString('ru-RU');
    document.getElementById('demo-total').textContent = `${setCount} ${setCount === 5 ? 'подходов' : 'подхода'} · Общий объём: ${volume} кг`;
    if (setCount === 5) {
      addSetButton.disabled = true;
      addSetButton.textContent = 'Тренировка готова!';
    }
  });
})();

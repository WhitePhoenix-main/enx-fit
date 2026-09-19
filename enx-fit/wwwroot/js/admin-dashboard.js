(() => {
    const canvas = document.getElementById('admin-activity-chart');
    const source = document.getElementById('admin-chart-data');
    if (!canvas || !source || typeof Chart === 'undefined') return;
    const data = JSON.parse(source.textContent);
    new Chart(canvas, {
        type: 'line',
        data: { labels: data.labels, datasets: [{ label: 'Тренировки', data: data.values, borderColor: '#4293df', backgroundColor: '#4293df0d', borderWidth: 2, fill: true, tension: .25, pointRadius: data.labels.length <= 7 ? 3 : 0, pointHoverRadius: 5, pointBackgroundColor: '#4293df' }] },
        options: {
            responsive: true, maintainAspectRatio: false,
            animation: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? false : { duration: 350 },
            interaction: { mode: 'index', intersect: false },
            plugins: { legend: { display: false }, tooltip: { backgroundColor: '#263747', padding: 12, displayColors: false } },
            scales: {
                x: { grid: { display: false }, border: { display: false }, ticks: { maxTicksLimit: 7, maxRotation: 0, color: '#a0aab8', font: { size: 10 } } },
                y: { beginAtZero: true, suggestedMax: 4, border: { display: false }, grid: { color: '#f0f3f7' }, ticks: { precision: 0, maxTicksLimit: 5, padding: 10, color: '#a0aab8', font: { size: 10 } } }
            }
        }
    });
})();

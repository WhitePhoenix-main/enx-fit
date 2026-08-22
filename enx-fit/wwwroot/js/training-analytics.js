(() => {
    const dataElement = document.getElementById("training-analytics-chart-data");

    if (!dataElement || typeof Chart === "undefined") {
        return;
    }

    const data = JSON.parse(dataElement.textContent);

    const createChart = (elementId, label, chartData, color) => {
        const element = document.getElementById(elementId);

        if (!element) {
            return;
        }

        new Chart(element, {
            type: "line",
            data: {
                labels: chartData.labels,
                datasets: [{
                    label,
                    data: chartData.values,
                    borderColor: color,
                    backgroundColor: `${color}20`,
                    fill: true,
                    tension: 0.25
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: false
                    }
                }
            }
        });
    };

    createChart("working-weight-chart", "Working weight (kg)", data.workingWeight, "#0d6efd");
    createChart("estimated-one-rep-max-chart", "Estimated 1RM (kg)", data.estimatedOneRepMax, "#198754");
    createChart("exercise-volume-chart", "Training volume", data.volume, "#6f42c1");
})();

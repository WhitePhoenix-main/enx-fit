(() => {
    const dataElement = document.getElementById("body-measurement-chart-data");

    if (!dataElement || typeof Chart === "undefined") {
        return;
    }

    const data = JSON.parse(dataElement.textContent);

    const createChart = (elementId, label, values, color) => {
        const element = document.getElementById(elementId);

        if (!element) {
            return;
        }

        new Chart(element, {
            type: "line",
            data: {
                labels: data.labels,
                datasets: [{
                    label,
                    data: values,
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

    createChart("weight-chart", "Weight (kg)", data.weights, "#0d6efd");
    createChart("bmi-chart", "BMI", data.bmis, "#198754");
})();

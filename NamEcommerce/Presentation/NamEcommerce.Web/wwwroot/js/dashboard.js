(function () {
    function readChartData() {
        var dataElement = document.getElementById('dashboardChartData');
        if (!dataElement) {
            return null;
        }

        try {
            return JSON.parse(dataElement.textContent || '{}');
        } catch {
            return null;
        }
    }

    function formatCurrency(value) {
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND',
            maximumFractionDigits: 0
        }).format(value || 0);
    }

    function renderRevenueChart(payload) {
        var canvas = document.getElementById('dashboardRevenueChart');
        if (!canvas || !payload?.revenueTrend) {
            return;
        }

        var ctx = canvas.getContext('2d');
        var revenueGradient = ctx.createLinearGradient(0, 0, 0, 260);
        revenueGradient.addColorStop(0, 'rgba(13, 110, 253, 0.24)');
        revenueGradient.addColorStop(1, 'rgba(13, 110, 253, 0.02)');

        new Chart(canvas, {
            type: 'line',
            data: {
                labels: payload.revenueTrend.labels || [],
                datasets: [
                    {
                        label: 'Doanh thu',
                        data: payload.revenueTrend.revenues || [],
                        borderColor: '#0d6efd',
                        backgroundColor: revenueGradient,
                        fill: true,
                        borderWidth: 2,
                        pointRadius: 2,
                        pointHoverRadius: 4,
                        tension: 0.32
                    },
                    {
                        label: 'Lãi gộp',
                        data: payload.revenueTrend.profits || [],
                        borderColor: '#198754',
                        backgroundColor: 'rgba(25, 135, 84, 0.08)',
                        fill: false,
                        borderWidth: 2,
                        pointRadius: 2,
                        pointHoverRadius: 4,
                        tension: 0.32
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { mode: 'index', intersect: false },
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: { boxWidth: 10, usePointStyle: true }
                    },
                    tooltip: {
                        callbacks: {
                            label: function (context) {
                                return context.dataset.label + ': ' + formatCurrency(context.parsed.y);
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        grid: { display: false },
                        ticks: { maxRotation: 0, autoSkip: true, maxTicksLimit: 10 }
                    },
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function (value) {
                                return formatCurrency(value);
                            }
                        }
                    }
                }
            }
        });
    }

    function renderProfitChart(payload) {
        var canvas = document.getElementById('dashboardProfitChart');
        if (!canvas || !payload?.profitBreakdown) {
            return;
        }

        var labels = payload.profitBreakdown.labels || [];
        var values = (payload.profitBreakdown.values || []).map(function (value) {
            return Math.max(0, Number(value) || 0);
        });
        var hasData = values.some(function (value) {
            return value > 0;
        });

        new Chart(canvas, {
            type: 'doughnut',
            data: {
                labels: hasData ? labels : ['Chưa có dữ liệu'],
                datasets: [
                    {
                        data: hasData ? values : [1],
                        backgroundColor: hasData ? ['#dc3545', '#198754', '#ffc107'] : ['#e9ecef'],
                        borderColor: '#fff',
                        borderWidth: 3,
                        hoverOffset: 4
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '62%',
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: { boxWidth: 10, usePointStyle: true }
                    },
                    tooltip: {
                        callbacks: {
                            label: function (context) {
                                if (!hasData) {
                                    return 'Chưa có dữ liệu';
                                }

                                return context.label + ': ' + formatCurrency(context.parsed);
                            }
                        }
                    }
                }
            }
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        if (typeof Chart !== 'function') {
            return;
        }

        var payload = readChartData();
        if (!payload) {
            return;
        }

        renderRevenueChart(payload);
        renderProfitChart(payload);
    });
})();

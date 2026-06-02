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

    function valueClass(value) {
        if (value < 0) return 'kpi-detail-value is-negative';
        if (value > 0) return 'kpi-detail-value is-positive';
        return 'kpi-detail-value';
    }

    function detailRow(label, value, isSigned) {
        var displayValue = isSigned && value !== 0
            ? (value < 0 ? '− ' + formatCurrency(Math.abs(value)) : '+ ' + formatCurrency(value))
            : formatCurrency(value);

        return '<div class="kpi-detail-row">'
            + '<span class="kpi-detail-label">' + label + '</span>'
            + '<span class="' + valueClass(value) + '">' + displayValue + '</span>'
            + '</div>';
    }

    function subtotalRow(label, value) {
        return '<div class="kpi-detail-row kpi-detail-subtotal">'
            + '<span>' + label + '</span>'
            + '<span class="' + valueClass(value) + '">' + formatCurrency(value) + '</span>'
            + '</div>';
    }

    function totalRow(label, value) {
        return '<div class="kpi-detail-row kpi-detail-total">'
            + '<span>' + label + '</span>'
            + '<span class="' + valueClass(value) + '">' + formatCurrency(value) + '</span>'
            + '</div>';
    }

    function buildRevenueDetail(data) {
        var html = detailRow('Doanh thu gộp', data.gross, false);
        if (data.returns !== 0) {
            html += detailRow('Trả hàng', -data.returns, true);
        }
        html += totalRow('Doanh thu thuần', data.net);
        return html;
    }

    function buildProfitDetail(data) {
        var html = detailRow('Doanh thu thuần', data.revenue, false);
        html += detailRow('Giá vốn hàng bán', -data.cogs, true);
        html += subtotalRow('Lãi gộp', data.grossProfit);
        html += detailRow('Chi phí vận hành', -data.opex, true);
        html += totalRow('Lợi nhuận ròng', data.net);
        return html;
    }

    var periodLabels = {
        today: 'Hôm nay',
        month: 'Tháng này',
        quarter: 'Quý này',
        year: 'Năm nay'
    };

    var typeLabels = {
        revenue: 'Doanh số',
        profit: 'Lợi nhuận'
    };

    function initKpiDetailModals(payload) {
        var modalEl = document.getElementById('kpiDetailModal');
        if (!modalEl || typeof bootstrap === 'undefined') {
            return;
        }

        var modal = new bootstrap.Modal(modalEl);
        var modalTitle = document.getElementById('kpiDetailModalLabel');
        var modalBody = document.getElementById('kpiDetailModalBody');

        function openDetail(type, period) {
            var label = typeLabels[type] + ' — ' + periodLabels[period];
            modalTitle.textContent = label;

            var html = '';
            if (type === 'revenue' && payload.revenueDetail) {
                var data = payload.revenueDetail[period];
                if (data) {
                    html = buildRevenueDetail(data);
                }
            } else if (type === 'profit' && payload.profitDetail) {
                var pdata = payload.profitDetail[period];
                if (pdata) {
                    html = buildProfitDetail(pdata);
                }
            }

            modalBody.innerHTML = html || '<p class="text-muted mb-0">Không có dữ liệu.</p>';
            modal.show();
        }

        document.querySelectorAll('[data-kpi-detail]').forEach(function (card) {
            function handleActivate() {
                var parts = card.getAttribute('data-kpi-detail').split('-');
                var type = parts[0];
                var period = parts[1];
                openDetail(type, period);
            }

            card.addEventListener('click', handleActivate);
            card.addEventListener('keydown', function (e) {
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    handleActivate();
                }
            });
        });
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
        var payload = readChartData();
        if (!payload) {
            return;
        }

        initKpiDetailModals(payload);

        if (typeof Chart !== 'function') {
            return;
        }

        renderRevenueChart(payload);
        renderProfitChart(payload);
    });
})();

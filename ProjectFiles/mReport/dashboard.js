function jitterVal(base, range) { return +(base + (Math.random() - 0.5) * range * 2).toFixed(1); }

var flowLabels = Array.from({length: 20}, function(_, i) { return '-' + ((20 - i) * 2) + 's'; });
function mkFlowData() { return Array.from({length: 20}, function() { return jitterVal(21, 0.5); }); }

var gridColor = 'rgba(200,210,220,0.7)';
var tickColor = '#7a8299';
var monoFont = 'ui-monospace, Consolas, monospace';

var flowCtx = document.getElementById('flowChart').getContext('2d');
var flowChart = new Chart(flowCtx, {
  type: 'line',
  data: {
    labels: flowLabels,
    datasets: [
      { label: 'P-101A', data: mkFlowData(), borderColor: '#1a9e50', backgroundColor: 'rgba(26,158,80,0.08)', borderWidth: 1.5, pointRadius: 0, tension: 0.4, fill: true },
      { label: 'P-101B', data: mkFlowData(), borderColor: '#0066cc', backgroundColor: 'rgba(0,102,204,0.07)', borderWidth: 1.5, pointRadius: 0, tension: 0.4, fill: true },
      { label: 'P-101C', data: mkFlowData(), borderColor: '#c8860b', backgroundColor: 'rgba(200,134,11,0.07)', borderWidth: 1.5, pointRadius: 0, tension: 0.4, fill: true }
    ]
  },
  options: {
    responsive: true, maintainAspectRatio: false, animation: false,
    interaction: { mode: 'index', intersect: false },
    plugins: { legend: { labels: { color: tickColor, font: { family: monoFont, size: 10 }, boxWidth: 12, padding: 12 } } },
    scales: {
      x: { ticks: { color: tickColor, font: { family: monoFont, size: 9 }, maxTicksLimit: 6 }, grid: { color: gridColor } },
      y: { min: 18, max: 24, ticks: { color: tickColor, font: { family: monoFont, size: 9 } }, grid: { color: gridColor } }
    }
  }
});

var pressCtx = document.getElementById('pressureChart').getContext('2d');
var pressChart = new Chart(pressCtx, {
  type: 'bar',
  data: {
    labels: ['P-101A', 'P-101B', 'P-101C', 'P-201'],
    datasets: [{
      label: 'Pressure (bar)',
      data: [5.0, 5.0, 4.9, 3.8],
      backgroundColor: ['rgba(26,158,80,0.2)', 'rgba(0,102,204,0.2)', 'rgba(200,134,11,0.2)', 'rgba(232,93,0,0.2)'],
      borderColor: ['#1a9e50', '#0066cc', '#c8860b', '#e85d00'],
      borderWidth: 1.5,
      borderRadius: 3
    }]
  },
  options: {
    responsive: true, maintainAspectRatio: false, animation: false,
    plugins: { legend: { display: false } },
    scales: {
      x: { ticks: { color: tickColor, font: { family: monoFont, size: 10 } }, grid: { color: gridColor } },
      y: { min: 0, max: 7, ticks: { color: tickColor, font: { family: monoFont, size: 9 } }, grid: { color: gridColor } }
    }
  }
});

var tankLevel = 84.1;

function updateData() {
  var paFlow = jitterVal(21.0, 0.4);
  var pbFlow = jitterVal(21.0, 0.4);
  var pcFlow = jitterVal(20.7, 0.3);
  var p201Flow = jitterVal(58.2, 0.8);
  var paBar = jitterVal(5.0, 0.08);
  var pbBar = jitterVal(5.0, 0.08);
  var pcBar = jitterVal(4.9, 0.07);
  var p201Bar = jitterVal(3.8, 0.06);

  tankLevel += (Math.random() - 0.5) * 0.3;
  if (tankLevel > 90) tankLevel = 89.5;
  if (tankLevel < 78) tankLevel = 78.5;
  tankLevel = +tankLevel.toFixed(1);
  var pct = Math.round(tankLevel);

  document.getElementById('pa-flow').textContent = paFlow;
  document.getElementById('pb-flow').textContent = pbFlow;
  document.getElementById('pc-flow').textContent = pcFlow;
  document.getElementById('p201-flow').textContent = p201Flow;
  document.getElementById('pa-bar').textContent = paBar;
  document.getElementById('pb-bar').textContent = pbBar;
  document.getElementById('pc-bar').textContent = pcBar;
  document.getElementById('p201-bar').textContent = p201Bar;

  document.getElementById('t-pa-flow').textContent = paFlow;
  document.getElementById('t-pb-flow').textContent = pbFlow;
  document.getElementById('t-pc-flow').textContent = pcFlow;
  document.getElementById('t-p201-flow').textContent = p201Flow;
  document.getElementById('t-pa-bar').textContent = paBar;
  document.getElementById('t-pb-bar').textContent = pbBar;
  document.getElementById('t-pc-bar').textContent = pcBar;
  document.getElementById('t-p201-bar').textContent = p201Bar;

  document.getElementById('tank-display').textContent = tankLevel;
  document.getElementById('kpi-tank').textContent = tankLevel;
  document.getElementById('tank-pct').textContent = pct + '%';
  document.getElementById('tank-fill').style.height = pct + '%';

  document.getElementById('kpi-flow').textContent = +(paFlow + pbFlow + pcFlow).toFixed(1);
  document.getElementById('kpi-out').textContent = p201Flow;

  flowChart.data.datasets[0].data.push(paFlow); flowChart.data.datasets[0].data.shift();
  flowChart.data.datasets[1].data.push(pbFlow); flowChart.data.datasets[1].data.shift();
  flowChart.data.datasets[2].data.push(pcFlow); flowChart.data.datasets[2].data.shift();
  flowChart.update('none');

  pressChart.data.datasets[0].data = [paBar, pbBar, pcBar, p201Bar];
  pressChart.update('none');
}

setInterval(updateData, 2000);
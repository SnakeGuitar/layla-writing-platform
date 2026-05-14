// Renders a 12-month rolling line chart of new users.
// `data` is expected as the oldest bucket first, newest last —
// matching the AdminService.DashboardStats.NewUsersPerMonth shape.
function createBarChart(canvasId, data) {
  const canvas = document.getElementById(canvasId);
  if (!canvas) {
    console.error(`Canvas chart element with id "${canvasId}" not found.`);
    return;
  }

  // Build month labels relative to "today" so the rightmost bar is the
  // current month, regardless of which calendar month we're in.
  const monthNames = [
    "Jan", "Feb", "Mar", "Apr", "May", "Jun",
    "Jul", "Aug", "Sep", "Oct", "Nov", "Dec",
  ];
  const now = new Date();
  const labels = [];
  for (let i = 11; i >= 0; i--) {
    const d = new Date(now.getFullYear(), now.getMonth() - i, 1);
    labels.push(monthNames[d.getMonth()]);
  }

  try {
    // Tear down any previous chart bound to the canvas so navigating back
    // into the dashboard doesn't pile up overlapping <canvas> instances.
    if (canvas._chart) {
      canvas._chart.destroy();
    }

    canvas._chart = new Chart(canvas, {
      type: "line",
      data: {
        labels,
        datasets: [{
          label: "New users",
          data,
          backgroundColor: "rgba(180, 138, 82, 0.18)",
          borderColor: "rgba(180, 138, 82, 0.95)",
          borderWidth: 2,
          tension: 0.25,
          fill: true,
          pointRadius: 3,
        }],
      },
      options: {
        responsive: true,
        plugins: {
          legend: { position: "top", labels: { color: "#d6d3d1" } },
          title: { display: false },
        },
        scales: {
          x: { ticks: { color: "#a8a29e" }, grid: { color: "rgba(255,255,255,0.05)" } },
          y: { beginAtZero: true, ticks: { color: "#a8a29e" }, grid: { color: "rgba(255,255,255,0.05)" } },
        },
      },
    });
  } catch (error) {
    console.error(
      `Error creating chart "${canvasId}": ${error}`
    );
  }
}
window.createBarChart = createBarChart;

function scrollToElement(elementId) {
    const element = document.getElementById(elementId);
    if (element instanceof HTMLElement) {
        element.scrollIntoView({
            behavior: "smooth",
            block: "start",
            inline: "center"
        });
    }
}

function resetMath() {
    MathJax.texReset();
    MathJax.typeset();
}

let chart;

function createChart(functionString, step, maxT) {
    const ctx = document.getElementById('myChart');
    const data = generateData(functionString, step, maxT);

    chart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: data.labels,
            datasets: [{
                label: 'h(t)',
                data: data.values,
                borderColor: 'rgba(75, 192, 192, 1)',
                borderWidth: 1,
                pointStyle: false
            }]
        },
        options: {
            scales: {
                x: {
                    type: 'linear',
                    position: 'bottom'
                }
            }
        }
    });
}

function updateChart(functionString, step, maxT) {
    if (chart) {
        const data = generateData(functionString, step, maxT);
        chart.data.labels = data.labels;
        chart.data.datasets[0].data = data.values;
        chart.update();
    } else {
        createChart(functionString, step, maxT);
    }
}

function deleteChart() {
    if (chart) {
        chart.destroy();
        chart = null;
    }
}

function generateData(functionString, step, maxT) {
    const labels = [];
    const values = [];

    for (let t = 0; t <= maxT; t += step) {
        labels.push(t.toFixed(6));
        values.push(calculateFunction(functionString, t));
    }

    return { labels, values };
}

function calculateFunction(functionString, t) {
    const func = new Function('t', 'return ' + functionString.replace(/exp/g, 'Math.exp').replace(/cos/g, 'Math.cos'));
    return func(t);
}

window.triggerFileDownload = (fileName, url) => {
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName ?? '';
    anchorElement.click();
    anchorElement.remove();
};

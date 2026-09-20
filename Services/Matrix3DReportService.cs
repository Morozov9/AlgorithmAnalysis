using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AlgorithmAnalysis.Models.Algorithms;

namespace AlgorithmAnalysis.Services;

/// <summary>
/// Сервис для проведения 3D-эксперимента матричного умножения A(n×m) × B(m×k)
/// и генерации интерактивного 3D-отчёта на Plotly.js.
/// </summary>
public class Matrix3DReportService
{
    private readonly int _runsPerPoint = 3;
    private readonly int _randomSeed = 42;

    public async Task<string> RunAndGenerateReportAsync(
        Action<string, double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Формируем сетку размеров: n и m от 20 до 200 с шагом 20 (10x10 = 100 точек)
        int[] nSizes = [20, 40, 60, 80, 100, 120, 140, 160, 180, 200];
        int[] mSizes = [20, 40, 60, 80, 100, 120, 140, 160, 180, 200];

        int maxN = nSizes.Max();
        int maxM = mSizes.Max();
        int maxK = maxN; // k = n: B имеет размер m x n, C имеет размер n x n

        progress?.Invoke("Генерация мастер-данных матриц...", 0.05);

        var matrixAlgo = new MatrixMultiplication();
        var random = new Random(_randomSeed);

        // Предвыделение буферов один раз для исключения LOH/GC пауз во время замеров
        matrixAlgo.GenerateMasterData(maxN, maxM, maxK, random);

        // Прогрев JIT
        matrixAlgo.PrepareData(20, 20, 20);
        matrixAlgo.Execute();
        matrixAlgo.Execute();

        int totalPoints = nSizes.Length * mSizes.Length;
        int completed = 0;

        // zExp[m_idx][n_idx] — индексация для Plotly.js (Ny строк, Nx колонок)
        double[][] zExp = new double[mSizes.Length][];
        for (int mIdx = 0; mIdx < mSizes.Length; mIdx++)
        {
            zExp[mIdx] = new double[nSizes.Length];
        }

        var stopwatch = new Stopwatch();

        // 2. Серия замеров по сетке (n, m)
        await Task.Run(() =>
        {
            for (int mIdx = 0; mIdx < mSizes.Length; mIdx++)
            {
                int m = mSizes[mIdx];

                for (int nIdx = 0; nIdx < nSizes.Length; nIdx++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    int n = nSizes[nIdx];
                    int k = n; // B(m x n), результат C(n x n)

                    double totalMs = 0;

                    for (int run = 0; run < _runsPerPoint; run++)
                    {
                        matrixAlgo.PrepareData(n, m, k);

                        stopwatch.Restart();
                        matrixAlgo.Execute();
                        stopwatch.Stop();

                        totalMs += stopwatch.Elapsed.TotalMilliseconds;
                    }

                    double avgMs = totalMs / _runsPerPoint;
                    zExp[mIdx][nIdx] = avgMs;

                    completed++;
                    double frac = 0.05 + 0.85 * ((double)completed / totalPoints);
                    progress?.Invoke($"Замер сетки: n={n}, m={m} ({completed}/{totalPoints})", frac);
                }
            }
        }, cancellationToken);

        // 3. Аппроксимация МНК для теоретической модели T = c · (n² · m)
        progress?.Invoke("Расчёт теоретической аппроксимации МНК...", 0.92);

        double numerator = 0;
        double denominator = 0;

        for (int mIdx = 0; mIdx < mSizes.Length; mIdx++)
        {
            int m = mSizes[mIdx];
            for (int nIdx = 0; nIdx < nSizes.Length; nIdx++)
            {
                int n = nSizes[nIdx];
                double fn = (double)n * n * m; // n^2 * m
                double tExp = zExp[mIdx][nIdx];

                numerator += tExp * fn;
                denominator += fn * fn;
            }
        }

        double fittedC = denominator > 0 ? numerator / denominator : 0;

        // Формируем теоретическую матрицу zTheo[m_idx][n_idx] и считаем MSE
        double[][] zTheo = new double[mSizes.Length][];
        double sumSqErrors = 0;

        for (int mIdx = 0; mIdx < mSizes.Length; mIdx++)
        {
            zTheo[mIdx] = new double[nSizes.Length];
            int m = mSizes[mIdx];

            for (int nIdx = 0; nIdx < nSizes.Length; nIdx++)
            {
                int n = nSizes[nIdx];
                double fn = (double)n * n * m;
                double tTheo = fittedC * fn;
                zTheo[mIdx][nIdx] = tTheo;

                double err = zExp[mIdx][nIdx] - tTheo;
                sumSqErrors += err * err;
            }
        }

        double mse = sumSqErrors / totalPoints;

        // 4. Генерация HTML-файла с Plotly 3D
        progress?.Invoke("Генерация 3D-графика Plotly...", 0.97);

        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", "Matrix3D");
        Directory.CreateDirectory(outputDir);
        string htmlPath = Path.Combine(outputDir, $"matrix_3d_{DateTime.Now:yyyyMMdd_HHmmss}.html");

        string htmlContent = GeneratePlotlyHtml(nSizes, mSizes, zExp, zTheo, fittedC, mse);
        await File.WriteAllTextAsync(htmlPath, htmlContent, Encoding.UTF8, cancellationToken);

        progress?.Invoke("3D-анализ успешно сформирован!", 1.0);
        return htmlPath;
    }

    private static string GeneratePlotlyHtml(
        int[] nSizes,
        int[] mSizes,
        double[][] zExp,
        double[][] zTheo,
        double fittedC,
        double mse)
    {
        string xJson = JsonSerializer.Serialize(nSizes);
        string yJson = JsonSerializer.Serialize(mSizes);
        string zExpJson = JsonSerializer.Serialize(zExp);
        string zTheoJson = JsonSerializer.Serialize(zTheo);

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"ru\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <title>3D-анализ матричного умножения: T(n, m)</title>");
        sb.AppendLine("  <script src=\"https://cdn.plot.ly/plotly-2.35.2.min.js\"></script>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    * { box-sizing: border-box; }");
        sb.AppendLine("    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; margin: 0; padding: 28px 40px; background: #f8fafc; color: #1e293b; }");
        sb.AppendLine("    .container { width: 100%; max-width: 100%; margin: 0; }");
        sb.AppendLine("    .header { background: #ffffff; padding: 24px 30px; border-radius: 8px; border: 1px solid #e2e8f0; border-left: 4px solid #2563eb; margin-bottom: 24px; box-shadow: 0 1px 3px rgba(0,0,0,0.03); }");
        sb.AppendLine("    h1 { margin: 0 0 8px 0; color: #0f172a; font-size: 22px; font-weight: 700; }");
        sb.AppendLine("    .meta-badges { display: flex; flex-wrap: wrap; gap: 10px; margin-top: 14px; }");
        sb.AppendLine("    .badge { background: #f1f5f9; border: 1px solid #e2e8f0; padding: 6px 12px; border-radius: 6px; font-size: 13px; font-weight: 500; color: #475569; }");
        sb.AppendLine("    .badge strong { color: #0f172a; }");
        sb.AppendLine("    .plot-card { background: #ffffff; border-radius: 8px; padding: 20px; box-shadow: 0 1px 3px rgba(0,0,0,0.03); margin-bottom: 24px; border: 1px solid #e2e8f0; }");
        sb.AppendLine("    #plot3d { width: 100%; height: 720px; }");
        sb.AppendLine("    .info-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; margin-top: 20px; }");
        sb.AppendLine("    @media (max-width: 900px) { .info-grid { grid-template-columns: 1fr; } }");
        sb.AppendLine("    .info-card { background: #ffffff; border: 1px solid #e2e8f0; border-radius: 8px; padding: 20px 24px; }");
        sb.AppendLine("    .info-card h3 { margin-top: 0; color: #0f172a; font-size: 16px; font-weight: 600; }");
        sb.AppendLine("    .info-card p, .info-card li { font-size: 14px; line-height: 1.6; color: #334155; }");
        sb.AppendLine("    code { background: #f1f5f9; padding: 2px 6px; border-radius: 4px; color: #0f172a; font-family: monospace; border: 1px solid #e2e8f0; font-size: 12px; }");
        sb.AppendLine("    .controls { display: flex; gap: 10px; margin-bottom: 15px; }");
        sb.AppendLine("    .btn { background: #2563eb; color: white; border: 1px solid #1d4ed8; padding: 8px 16px; border-radius: 6px; cursor: pointer; font-weight: 500; font-size: 13px; transition: background 0.15s; }");
        sb.AppendLine("    .btn:hover { background: #1d4ed8; }");
        sb.AppendLine("    .btn-secondary { background: #ffffff; color: #334155; border: 1px solid #cbd5e1; }");
        sb.AppendLine("    .btn-secondary:hover { background: #f1f5f9; }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <div class=\"container\">");
        sb.AppendLine("    <div class=\"header\">");
        sb.AppendLine("      <h1>3D-анализ матричного умножения: T(n, m)</h1>");
        sb.AppendLine("      <p>Исследование зависимости времени вычисления матричного произведения <code>C = A × B</code> от двух независимых размерностей: строк <code>n</code> и внутренних столбцов <code>m</code> (где <code>A: n×m</code>, <code>B: m×k</code>, <code>k = n</code>).</p>");
        sb.AppendLine("      <div class=\"meta-badges\">");
        sb.AppendLine("        <div class=\"badge\">Теоретическая сложность: <strong>O(n² · m)</strong></div>");
        sb.AppendLine($"        <div class=\"badge\">Коэффициент аппроксимации c: <strong>{fittedC:E3}</strong></div>");
        sb.AppendLine($"        <div class=\"badge\">MSE аппроксимации: <strong>{mse:E3}</strong></div>");
        sb.AppendLine($"        <div class=\"badge\">Сетка замеров: <strong>{nSizes.Length} × {mSizes.Length} = {nSizes.Length * mSizes.Length} точек</strong></div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("");
        sb.AppendLine("    <div class=\"plot-card\">");
        sb.AppendLine("      <div class=\"controls\">");
        sb.AppendLine("        <button class=\"btn\" onclick=\"toggleTheory()\">Показать / скрыть теоретическую поверхность</button>");
        sb.AppendLine("        <button class=\"btn btn-secondary\" onclick=\"resetCamera()\">Сбросить угол камеры</button>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div id=\"plot3d\"></div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("");
        sb.AppendLine("    <div class=\"info-grid\">");
        sb.AppendLine("      <div class=\"info-card\">");
        sb.AppendLine("        <h3>Теоретическая модель</h3>");
        sb.AppendLine("        <p>Для матриц <code>A (n × m)</code> и <code>B (m × k)</code> при <code>k = n</code>:</p>");
        sb.AppendLine("        <ul>");
        sb.AppendLine("          <li>Каждый элемент <code>C[i, j]</code> требует ровно <code>m</code> умножений и <code>m - 1</code> сложений.</li>");
        sb.AppendLine("          <li>Результирующая матрица содержит <code>n × n = n²</code> элементов.</li>");
        sb.AppendLine("          <li>Общее количество операций: <code>T(n, m) = c · n² · m = O(n² · m)</code>.</li>");
        sb.AppendLine("          <li>На графике теоретическая поверхность показана полупрозрачной красной сеткой (Wireframe).</li>");
        sb.AppendLine("        </ul>");
        sb.AppendLine("      </div>");
        sb.AppendLine("");
        sb.AppendLine("      <div class=\"info-card\">");
        sb.AppendLine("        <h3>Влияние архитектуры процессора и кэша L1/L2</h3>");
        sb.AppendLine("        <p>Особенности реального эмпирического поведения:</p>");
        sb.AppendLine("        <ul>");
        sb.AppendLine("          <li>В порядке циклов <code>i-j-p</code> обращение к элементам матрицы <code>B[p, j]</code> происходит с шагом по памяти (stride <code>n × 8</code> байт).</li>");
        sb.AppendLine("          <li>При малых <code>n, m ≤ 80</code> данные целиком помещаются в кэш L1/L2, обеспечивая идеальное соответствие теории.</li>");
        sb.AppendLine("          <li>При росте <code>n, m > 140</code> увеличивается частота кэш-промахов (Cache Misses), что приводит к ускоренному росту времени на практике.</li>");
        sb.AppendLine("        </ul>");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("");
        sb.AppendLine("  <script>");
        sb.AppendLine($"    const xData = {xJson};");
        sb.AppendLine($"    const yData = {yJson};");
        sb.AppendLine($"    const zExpData = {zExpJson};");
        sb.AppendLine($"    const zTheoData = {zTheoJson};");
        sb.AppendLine("");
        sb.AppendLine("    const traceExp = {");
        sb.AppendLine("      name: 'Эксперимент (мс)',");
        sb.AppendLine("      x: xData,");
        sb.AppendLine("      y: yData,");
        sb.AppendLine("      z: zExpData,");
        sb.AppendLine("      type: 'surface',");
        sb.AppendLine("      colorscale: 'Viridis',");
        sb.AppendLine("      colorbar: { title: 'T (мс)', tickfont: { color: '#334155' }, titlefont: { color: '#334155' } },");
        sb.AppendLine("      contours: {");
        sb.AppendLine("        z: { show: true, usecolormap: true, highlightcolor: '#2563eb', project: { z: true } }");
        sb.AppendLine("      },");
        sb.AppendLine("      hovertemplate: 'n: %{x}<br>m: %{y}<br>T эксп: %{z:.3f} мс<extra></extra>'");
        sb.AppendLine("    };");
        sb.AppendLine("");
        sb.AppendLine("    const traceTheo = {");
        sb.AppendLine("      name: 'Теория O(n²·m)',");
        sb.AppendLine("      x: xData,");
        sb.AppendLine("      y: yData,");
        sb.AppendLine("      z: zTheoData,");
        sb.AppendLine("      type: 'surface',");
        sb.AppendLine("      opacity: 0.5,");
        sb.AppendLine("      colorscale: 'Reds',");
        sb.AppendLine("      showscale: false,");
        sb.AppendLine("      visible: true,");
        sb.AppendLine("      hovertemplate: 'n: %{x}<br>m: %{y}<br>T теор: %{z:.3f} мс<extra></extra>'");
        sb.AppendLine("    };");
        sb.AppendLine("");
        sb.AppendLine("    const layout = {");
        sb.AppendLine("      paper_bgcolor: '#ffffff',");
        sb.AppendLine("      plot_bgcolor: '#ffffff',");
        sb.AppendLine("      font: { color: '#1e293b' },");
        sb.AppendLine("      margin: { l: 0, r: 0, b: 0, t: 30 },");
        sb.AppendLine("      scene: {");
        sb.AppendLine("        xaxis: { title: 'n (строки A)', gridcolor: '#e2e8f0', zerolinecolor: '#cbd5e1' },");
        sb.AppendLine("        yaxis: { title: 'm (столбцы A)', gridcolor: '#e2e8f0', zerolinecolor: '#cbd5e1' },");
        sb.AppendLine("        zaxis: { title: 'Время T (мс)', gridcolor: '#e2e8f0', zerolinecolor: '#cbd5e1' },");
        sb.AppendLine("        camera: {");
        sb.AppendLine("          eye: { x: 1.5, y: 1.5, z: 1.1 }");
        sb.AppendLine("        }");
        sb.AppendLine("      },");
        sb.AppendLine("      autosize: true");
        sb.AppendLine("    };");

        sb.AppendLine("    Plotly.newPlot('plot3d', [traceExp, traceTheo], layout, { responsive: true });");

        sb.AppendLine("    let theoryVisible = true;");
        sb.AppendLine("    function toggleTheory() {");
        sb.AppendLine("      theoryVisible = !theoryVisible;");
        sb.AppendLine("      Plotly.restyle('plot3d', { visible: theoryVisible }, [1]);");
        sb.AppendLine("    }");

        sb.AppendLine("    function resetCamera() {");
        sb.AppendLine("      Plotly.relayout('plot3d', { 'scene.camera.eye': { x: 1.5, y: 1.5, z: 1.1 } });");
        sb.AppendLine("    }");
        sb.AppendLine("  </script>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }
}

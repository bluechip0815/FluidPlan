using ScottPlot;
using SkiaSharp;

namespace FluidSimu
{
    public static class ResultVisualizer
    {
        public static void CreateCharts(string csvPath, string outputFileName, SimulationModelDto model)
        {
            Console.WriteLine("Reading results...");
            var data = ReadCsv(csvPath);

            Console.WriteLine("Generating static chart...");
            CreateStaticImage(data, outputFileName, model);
        }

        private static void CreateStaticImage(SimulationData data, string outputPath, SimulationModelDto model)
        {
            const int width = 1200;
            const int mainChartHeight = 600;

            var visibleValves = model.Elements
                .Where(e => e.Visible && e.Type.Equals("valve", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var visiblePressureElements = model.Elements
                .Where(e => e.Visible && !e.Type.Equals("valve", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var mainPlot = new Plot();
            mainPlot.Title("Pressure Distribution");
            mainPlot.YLabel("Pressure [bar]");
            mainPlot.ShowLegend();

            foreach (var element in visiblePressureElements)
            {
                if (data.Series.TryGetValue(element.Name, out var series))
                {
                    var sp = mainPlot.Add.Scatter(data.Time.ToArray(), series.ToArray());
                    sp.LegendText = element.Name;
                    sp.LineWidth = 2;
                }
            }

            if (!visibleValves.Any())
            {
                mainPlot.XLabel("Time [s]");
                mainPlot.SavePng(outputPath, width, mainChartHeight);
            }
            else
            {
                int valveChartHeight = Math.Max(100, visibleValves.Count * 40 + 60); // Base height + per-valve height
                int totalHeight = mainChartHeight + valveChartHeight;

                var valvePlot = new Plot();
                valvePlot.XLabel("Time [s]");

                var valveLabels = new List<Tick>();
                for (int i = 0; i < visibleValves.Count; i++)
                {
                    var valve = visibleValves[i];
                    if (data.Series.TryGetValue(valve.Name, out var series))
                    {
                        double yOffset = i * 1.5;
                        var valveState = series.Select(s => s > 0.5 ? yOffset + 1.0 : yOffset).ToArray();
                        var sp = valvePlot.Add.Scatter(data.Time.ToArray(), valveState);
                        sp.LineWidth = 2;
                    }
                    valveLabels.Add(new Tick(i * 1.5 + 0.5, visibleValves[i].Name));
                }

                valvePlot.Axes.Left.TickGenerator = new ScottPlot.TickGenerators.NumericManual(valveLabels.ToArray());
                valvePlot.Axes.Left.MajorTickStyle.Length = 0;
                valvePlot.Axes.SetLimitsY(-1, visibleValves.Count * 1.5);

                // Align X axes
                valvePlot.Axes.SetLimitsX(mainPlot.Axes.GetLimits());
                mainPlot.Axes.Bottom.TickLabelStyle.IsVisible = false;

                // Render plots to measure axis sizes
                _ = mainPlot.GetImageBytes(width, mainChartHeight);
                _ = valvePlot.GetImageBytes(width, valveChartHeight);
                float mainPlotLeftAxisSize = mainPlot.RenderManager.LastRender.Layout.DataRect.Width;
                float valvePlotLeftAxisSize = valvePlot.RenderManager.LastRender.Layout.DataRect.Width;
                float maxLeftAxisSize = Math.Min(mainPlotLeftAxisSize, valvePlotLeftAxisSize);
                mainPlot.Axes.Left.MinimumSize = width-maxLeftAxisSize;
                valvePlot.Axes.Left.MinimumSize = width-maxLeftAxisSize;

                using var bitmap = new SKBitmap(width, totalHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var canvas = new SKCanvas(bitmap);
                canvas.Clear(SKColors.White);

                using var mainBitmap = SKBitmap.Decode(mainPlot.GetImageBytes(width, mainChartHeight));
                canvas.DrawBitmap(mainBitmap, 0, 0);

                using var valveBitmap = SKBitmap.Decode(valvePlot.GetImageBytes(width, valveChartHeight));
                canvas.DrawBitmap(valveBitmap, 0, mainChartHeight);

                using var image = SKImage.FromBitmap(bitmap);
                using var stream = File.OpenWrite(outputPath);
                image.Encode(SKEncodedImageFormat.Png, 80).SaveTo(stream);
            }

            Console.WriteLine($"Saved: {outputPath}");
        }

        private static SimulationData ReadCsv(string path)
        {
            var lines = File.ReadAllLines(path);
            if (lines.Length < 2) return new SimulationData();

            // Header: "Time[s];Element1_P[bar];Element2_P[bar]..."
            var header = lines[0].Split(';');
            var result = new SimulationData();

            // Initialize Lists
            for (int i = 1; i < header.Length; i++)
            {
                // MODIFIED LINE: Split at the last underscore to get the clean name.
                // This handles both "ElementName_P[bar]" and "ElementName_State".
                var nameParts = header[i].Split('_');
                string name = nameParts[0];
                result.Series[name] = new List<double>();
            }

            // Parse Rows
            // Use CultureInfo.InvariantCulture for "." decimals
            var culture = System.Globalization.CultureInfo.InvariantCulture;

            for (int r = 1; r < lines.Length; r++)
            {
                if (string.IsNullOrWhiteSpace(lines[r])) continue;

                var cols = lines[r].Split(';');
                if (cols.Length != header.Length) continue;

                if (double.TryParse(cols[0], culture, out double t))
                {
                    result.Time.Add(t);
                    for (int i = 1; i < header.Length; i++)
                    {
                        if (double.TryParse(cols[i], culture, out double p))
                        {
                            int pos = header[i].LastIndexOf("_");
                            string key = header[i].Substring(0,pos);
                            result.Series[key].Add(p);
                        }
                    }
                }
            }
            return result;
        }

        private class SimulationData
        {
            public List<double> Time { get; set; } = new();
            public Dictionary<string, List<double>> Series { get; set; } = new();
        }
    }
}
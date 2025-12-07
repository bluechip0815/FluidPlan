using ScottPlot;
using SkiaSharp;
//using ImageMagick; // Required for GIF creation

namespace FluidSimu
{
    public static class ResultVisualizer
    {
        public static void CreateCharts(string csvPath, string outputFileName, SimulationModelDto model)
        {
            Console.WriteLine("Reading results...");
            var data = ReadCsv(csvPath);

            
            // 1. Create Static Image (The whole history)
            Console.WriteLine("Generating static chart...");
            CreateStaticImage(data, outputFileName, model);

            // 2. Create Animated GIF (Replay)
            //Console.WriteLine("Generating animated GIF (this may take time)...");
            //CreateAnimatedGif(data, Path.Combine(outputFolder, "simulation_replay.gif"));
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
                    var sp = mainPlot.Add.Scatter(data.Time, series);
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
                        var sp = valvePlot.Add.Scatter(data.Time, valveState);
                        sp.LineWidth = 2;
                    }
                    valveLabels.Add(new Tick(i * 1.5 + 0.5, visibleValves[i].Name));
                }

                valvePlot.Axes.Left.TickGenerator = new ScottPlot.TickGenerators.NumericManual(valveLabels.ToArray());
                valvePlot.Axes.Left.MajorTickStyle.Length = 0;
                valvePlot.Axes.SetLimitsY(-1, visibleValves.Count * 1.5);

                mainPlot.Axes.Bottom.TickLabelStyle.IsVisible = false;

                using var bitmap = new SKBitmap(width, totalHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var canvas = new SKCanvas(bitmap);
                canvas.Clear(SKColors.White);

                using var mainBitmap = mainPlot.GetBitmap(width, mainChartHeight);
                canvas.DrawBitmap(mainBitmap, 0, 0);

                using var valveBitmap = valvePlot.GetBitmap(width, valveChartHeight);
                canvas.DrawBitmap(valveBitmap, 0, mainChartHeight);

                using var image = SKImage.FromBitmap(bitmap);
                using var stream = File.OpenWrite(outputPath);
                image.Encode(SKEncodedImageFormat.Png, 80).SaveTo(stream);
            }

            Console.WriteLine($"Saved: {outputPath}");
        }

        //private static void CreateAnimatedGif(SimulationData data, string outputPath)
        //{
        //    // Settings
        //    int width = 800;
        //    int height = 600;
        //    int fps = 20; 
        //    int skipSteps = 10; // Don't render every single 0.1ms step, it's too slow.
            
        //    using (var collection = new MagickImageCollection())
        //    {
        //        // We create a "Moving Window" or "Progress Line" effect
        //        for (int i = 0; i < data.Time.Count; i += skipSteps)
        //        {
        //            var plt = new Plot();
        //            plt.Title($"Simulation T = {data.Time[i]:F2} s");
        //            plt.XLabel("Time [s]");
        //            plt.YLabel("Pressure [bar]");

        //            // Fix Axis limits so the chart doesn't jump around
        //            plt.Axes.SetLimits(0, data.Time.Last(), 0, 7.0); // Assuming 0-7 bar range

        //            // Draw the lines up to the current point 'i'
        //            foreach (var element in data.Series)
        //            {
        //                // Take subset of data
        //                double[] currentYs = element.Value.Take(i + 1).ToArray();
        //                double[] currentXs = data.Time.Take(i + 1).ToArray();

        //                var sp = plt.Add.Scatter(currentXs, currentYs);
        //                sp.LegendText = element.Key;
        //                sp.LineWidth = 2;
        //            }

        //            // Render frame to memory
        //            byte[] bytes = plt.GetImageBytes(width, height, ImageFormat.Png);
        //            var image = new MagickImage(bytes);
                    
        //            // Set delay for this frame (100 / fps)
        //            image.AnimationDelay = 100 / fps; 
        //            collection.Add(image);

        //            if (i % 50 == 0) Console.Write(".");
        //        }

        //        // Optimize and Save
        //        Console.WriteLine("\nEncoding GIF...");
        //        collection.Optimize();
        //        collection.Write(outputPath);
        //    }
        //    Console.WriteLine($"Saved: {outputPath}");
        //}

        // --- Helper to parse CSV ---
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
                string name = header[i].Replace("_P[bar]", "");
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
                            string key = header[i].Replace("_P[bar]", "");
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
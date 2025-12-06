using ScottPlot;
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
            var plt = new Plot();

            // Setup basic style
            plt.Title("Pressure Distribution");
            plt.XLabel("Time [s]");
            plt.YLabel("Pressure [bar]");
            plt.ShowLegend();

            // Create a lookup for visibility
            var visibilityLookup = model.Elements
                                        .ToDictionary(e => e.Name, e => e.Visible);

            // Add a line for each VISIBLE element
            foreach (var series in data.Series)
            {
                // Only plot if the element is marked as visible in the model JSON
                if (visibilityLookup.TryGetValue(series.Key, out bool isVisible) && isVisible)
                {
                    var sp = plt.Add.Scatter(data.Time, series.Value);
                    sp.LegendText = series.Key;
                    sp.LineWidth = 2;
                }
            }

            // Save
            plt.SavePng(outputPath, 1200, 800);
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
using System.Text;

namespace FluidSimu
{
    public class SimulationLogger : IDisposable
    {
        private readonly StreamWriter _writer;
        private readonly List<IPneumaticElement> _elements;
        // The minimum time that must pass between log entries.
        private const double MinLogInterval = 0.001; // 1ms
        // Stores the simulation time of the last saved log entry.
        private double _lastLogTime = -1.0;

        public SimulationLogger(string filePath, List<IPneumaticElement> elements)
        {
            _elements = elements.OrderBy(e => e.Id).ToList();
            _writer = new StreamWriter(filePath, append: false, encoding: Encoding.UTF8);
            
            // Write Header
            WriteHeader();
        }

        private void WriteHeader()
        {
            var sb = new StringBuilder();
            sb.Append("Time[s]");
            
            foreach (var el in _elements)
            {
                string unit = (el is ValveElement) ? "_State" : "_P[bar]";
                sb.Append($";{el.Name}{unit}");
            }
            _writer.WriteLine(sb.ToString());
        }

        public void LogStep(double time)
        {
            // This condition ensures we always log the first step (t=0) and then
            // only log subsequent steps if the time has advanced by at least the minimum interval.
            if (_lastLogTime < 0 || (time >= _lastLogTime + MinLogInterval))
            {
                var sb = new StringBuilder();
                // Format time with fixed precision
                sb.Append(time.ToString("F4", System.Globalization.CultureInfo.InvariantCulture));

                foreach (var el in _elements)
                {
                    sb.Append($";{el.LoggableValue.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)}");
                }

                _writer.WriteLine(sb.ToString());

                // Update the time of the last log entry.
                _lastLogTime = time;
            }
        }

        public void Dispose()
        {
            _writer.Flush();
            _writer.Close();
            _writer.Dispose();
        }
    }
}
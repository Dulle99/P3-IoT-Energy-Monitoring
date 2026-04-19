namespace SensorSimulator.Models
{
    public class SimulatorState
    {
        public readonly object _lock = new();

        public bool LoadShedEnabled
        {
            get
            {
                lock (_lock)
                {
                    return loadShedEnabled;

                }
            }
            set
            {
                lock (_lock)
                {
                    loadShedEnabled = value;
                }
            }

        }

        private bool loadShedEnabled;
    }
}

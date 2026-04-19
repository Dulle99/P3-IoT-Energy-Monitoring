namespace SensorSimulator.Models
{
    public class SimulatorState
    {
        public readonly object _lock = new();

        public bool LoadShedEnable
        {
            get
            {
                lock (_lock)
                {
                    return loadShedEnable;

                }
            }
            set
            {
                lock (_lock)
                {
                    loadShedEnable = value;
                }
            }

        }

        private bool loadShedEnable;
    }
}

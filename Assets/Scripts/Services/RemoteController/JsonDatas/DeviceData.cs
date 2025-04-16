using System;
using JetBrains.Annotations;

namespace Services
{
    [Serializable]
    public class DeviceData
    {
        public string serial_number;
        public string model;
        public int charge;
        public int? volume;
        public long free_memory;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OS.AutoScanner.Models
{
    public class OSBluetoothLEDevice
    {

        public OSBluetoothLEDevice(ulong address, string name, short rssi, DateTimeOffset broadcastTime)
        {
            this.Address = address;
            this.BroadcastTime = broadcastTime;
            this.SignalStrengthInDB = rssi;
            this.Name = name;
        }
        public DateTimeOffset BroadcastTime { get; }
        public ulong Address { get; set; }
        public string Name { get; set; }

        public short SignalStrengthInDB { get; }

        public override string ToString()
        {
            return $"{(string.IsNullOrEmpty(Name)?"[No Name]":Name)}{Address}{(SignalStrengthInDB)}";
        }
    }
}

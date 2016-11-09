using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ras_bt_test
{
    class Program
    {
        static void Main(string[] args)
        {
            SerialPort arduino = new SerialPort("COM7");
            // Replace this COM port by the appropriate one on your computer
            Console.WriteLine("Connecting to Arduino (COM7)...");
            arduino.Open();
            if (arduino.IsOpen)
            {
                Console.WriteLine("Connection successful!");
            }
            while (arduino.IsOpen)
            {
                arduino.WriteLine("Ping!");
            }
            Console.WriteLine("Connection to Arduino closed.");
            arduino.Close();
        }
    }
}

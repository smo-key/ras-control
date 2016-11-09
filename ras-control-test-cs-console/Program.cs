using InTheHand.Net.Sockets;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ras_control_test_cs_console
{
    class Program
    {
        enum JoystickButtons
        {
            X = JoystickOffset.Buttons0,
            A = JoystickOffset.Buttons1,
            B = JoystickOffset.Buttons2,
            Y = JoystickOffset.Buttons3,
            LB = JoystickOffset.Buttons4,
            RB = JoystickOffset.Buttons5,
            LT = JoystickOffset.Buttons6,
            RT = JoystickOffset.Buttons7,
            BACK = JoystickOffset.Buttons8,
            START = JoystickOffset.Buttons9,
            LJ = JoystickOffset.Buttons10,
            RJ = JoystickOffset.Buttons11,
            POV = JoystickOffset.PointOfViewControllers0,
            LX = JoystickOffset.X,
            LY = JoystickOffset.Y
        }

        static string parseUpdate(JoystickUpdate update)
        {
            JoystickButtons btn = (JoystickButtons)update.Offset;
            string data = btn.ToString();
            switch (btn)
            {
                case JoystickButtons.X:
                case JoystickButtons.A:
                case JoystickButtons.B:
                case JoystickButtons.Y:
                case JoystickButtons.LB:
                case JoystickButtons.RB:
                case JoystickButtons.LT:
                case JoystickButtons.RT:
                case JoystickButtons.BACK:
                case JoystickButtons.START:
                case JoystickButtons.LJ:
                case JoystickButtons.RJ:
                    data += " " + (update.Value == 128 ? 1 : 0);
                    break;
                case JoystickButtons.POV: //Top, Bottom, Left, Right
                    data += " " + ((update.Value == 0) || (update.Value == 4500) || (update.Value == 31500) ? 1 : 0) + " "
                                + ((update.Value == 18000) || (update.Value == 13500) || (update.Value == 22500) ? 1 : 0) + " "
                                + ((update.Value == 27000) || (update.Value == 22500) || (update.Value == 31500) ? 1 : 0) + " "
                                + ((update.Value == 9000) || (update.Value == 4500) || (update.Value == 13500) ? 1 : 0) + " " + (update.Value / 100).ToString();
                    break;
                case JoystickButtons.LX:
                case JoystickButtons.LY:
                    data += " " + ((((update.Value + 1) / 65536.0)-0.5)*2.0);
                    break;
                default:
                    data = update.ToString();
                    break;
            }
            return data;
        }

        static void Main(string[] args)
        {
            /** BLUETOOTH INIT **/

            //Check to make sure BT is running
            new BluetoothClient();

            /** CONTROLLER INIT **/

            // Initialize DirectInput
            var directInput = new DirectInput();

            // Find a Joystick Guid
            var joystickGuid = Guid.Empty;

            foreach (var deviceInstance in directInput.GetDevices(DeviceType.Gamepad,
                        DeviceEnumerationFlags.AllDevices))
                joystickGuid = deviceInstance.InstanceGuid;

            // If Gamepad not found, look for a Joystick
            if (joystickGuid == Guid.Empty)
                foreach (var deviceInstance in directInput.GetDevices(DeviceType.Joystick,
                        DeviceEnumerationFlags.AllDevices))
                    joystickGuid = deviceInstance.InstanceGuid;

            // If Joystick not found, throws an error
            if (joystickGuid == Guid.Empty)
            {
                Console.WriteLine("No joystick/Gamepad found.");
                Console.ReadKey();
                Environment.Exit(1);
            }

            // Instantiate the joystick
            var joystick = new Joystick(directInput, joystickGuid);
            Console.WriteLine("Found Joystick/Gamepad with GUID: {0}", joystickGuid);

            // Set BufferSize in order to use buffered data.
            joystick.Properties.BufferSize = 128;

            // Acquire the joystick
            joystick.Acquire();

            // Poll events from joystick
            while (true)
            {
                try
                {
                    joystick.Poll();
                    var updates = joystick.GetBufferedData();
                    JoystickState state = joystick.GetCurrentState();
                    foreach (JoystickUpdate update in updates)
                    {
                        Console.WriteLine(parseUpdate(update));
                    }
                } catch (Exception e)
                {
                    Console.WriteLine("Error reading from joystick.");
                    joystick.Unacquire();
                    joystick.Acquire();
                }
            }
        }
    }
}

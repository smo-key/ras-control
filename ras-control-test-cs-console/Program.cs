﻿using InTheHand.Net.Sockets;
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
            LJ = JoystickOffset.Buttons10,                  //Left Joystick button
            RJ = JoystickOffset.Buttons11,                  //Right Joystick button
            POV = JoystickOffset.PointOfViewControllers0,
            LX = JoystickOffset.X,
            LY = JoystickOffset.Y,
            RX = JoystickOffset.Z,
            RY = JoystickOffset.RotationZ
        }

        struct PartialJoystickState
        {
            public double lx, ly, rx, ry;
        }

        struct ParseUpdateResult
        {
            public PartialJoystickState state;
            public string update;

            public ParseUpdateResult(PartialJoystickState state, string update)
            {
                this.state = state;
                this.update = update;
            }
        }

        static double deadband(double value, double minabs)
        {
            return Math.Abs(value) < minabs ? 0 : value;
        }

        static string calcJoystick(double x, double y)
        {
            //x y r theta
            string result = x.ToString("N6") + " " + y.ToString("N6");
            double r = Math.Sqrt((x * x) + (y * y));
            double theta = 90.0 - (Math.Atan2(y, x) / Math.PI * 180.0);
            theta = ((theta > 180.0) && (theta <= 270.0)) ? theta - 360.0 : theta;
            theta = (r == 0) ? 0 : theta;
            r = (r > 1) ? 1 : r;
            result += " " + r.ToString("N6") + " " + theta.ToString("N2");
            return result;
        }

        static ParseUpdateResult parseUpdate(JoystickUpdate update, PartialJoystickState state)
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
                    double val = update.Value / 100;
                    val = ((val > 180.0) && (val < 360.0)) ? val - 360.0 : val;
                    val = (update.Value == -1) ? -1 : val;
                    data += " " + ((update.Value == 0) || (update.Value == 4500) || (update.Value == 31500) ? 1 : 0) + " "
                                + ((update.Value == 18000) || (update.Value == 13500) || (update.Value == 22500) ? 1 : 0) + " "
                                + ((update.Value == 27000) || (update.Value == 22500) || (update.Value == 31500) ? 1 : 0) + " "
                                + ((update.Value == 9000) || (update.Value == 4500) || (update.Value == 13500) ? 1 : 0) + " " + (val).ToString();
                    break;
                case JoystickButtons.LX:
                    state.lx = deadband(((update.Value / 65535.0) - 0.5) * 2.0, 0.05);
                    data = "L " + calcJoystick(state.lx, state.ly);
                    break;
                case JoystickButtons.LY:
                    state.ly = deadband(((update.Value / 65535.0) - 0.5) * -2.0, 0.05);
                    data = "L " + calcJoystick(state.lx, state.ly);
                    break;
                case JoystickButtons.RX:
                    state.rx = deadband(((update.Value / 65535.0) - 0.5) * 2.0, 0.05);
                    data = "R " + calcJoystick(state.rx, state.ry);
                    break;
                case JoystickButtons.RY:
                    state.ry = deadband(((update.Value / 65535.0) - 0.5) * -2.0, 0.05);
                    data = "R " + calcJoystick(state.rx, state.ry);
                    break;
                default:
                    data = update.ToString();
                    break;
            }
            return new ParseUpdateResult(state, data);
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
            PartialJoystickState partialState = new PartialJoystickState();
            partialState.lx = 0.0;
            partialState.ly = 0.0;
            partialState.rx = 0.0;
            partialState.ry = 0.0;
            while (true)
            {
                try
                {
                    joystick.Poll();
                    var updates = joystick.GetBufferedData();
                    //JoystickState state = joystick.GetCurrentState();
                    foreach (JoystickUpdate update in updates)
                    {
                        ParseUpdateResult result = parseUpdate(update, partialState);
                        partialState = result.state;
                        Console.WriteLine(result.update);
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

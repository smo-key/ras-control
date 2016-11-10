using System;
using System.IO.Ports;
using SharpDX.DirectInput;

namespace ras_control_test_cs_console
{
    class Program
    {
        /** MAKE SURE JOYSTICK IS IN "D" MODE **/
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

            public bool isZero()
            {
                return (lx == 0) && (ly == 0) && (rx == 0) && (ry == 0);
            }
        }

        struct ParseUpdateResult
        {
            public PartialJoystickState state;
            public string update;
            public bool isJoystick;
            public bool isJoystickZero;
            public JoystickButtons btn;

            public ParseUpdateResult(PartialJoystickState state, string update, bool isJoystick, bool isJoystickZero, JoystickButtons btn)
            {
                this.state = state;
                this.update = update;
                this.isJoystick = isJoystick;
                this.isJoystickZero = isJoystickZero;
                this.btn = btn;
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
            bool isJoystickUpdate = false;
            bool isJoystickZero = false;
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
                    state.lx = deadband(((update.Value / 65535.0) - 0.5) * 2.0, 0.25);
                    data = "L " + calcJoystick(state.lx, state.ly);
                    isJoystickUpdate = true;
                    isJoystickZero = ((state.lx == 0) && (state.ly == 0));
                    break;
                case JoystickButtons.LY:
                    state.ly = deadband(((update.Value / 65535.0) - 0.5) * -2.0, 0.25);
                    data = "L " + calcJoystick(state.lx, state.ly);
                    isJoystickUpdate = true;
                    isJoystickZero = ((state.lx == 0) && (state.ly == 0));
                    break;
                case JoystickButtons.RX:
                    state.rx = deadband(((update.Value / 65535.0) - 0.5) * 2.0, 0.25);
                    data = "R " + calcJoystick(state.rx, state.ry);
                    isJoystickUpdate = true;
                    isJoystickZero = ((state.rx == 0) && (state.ry == 0));
                    break;
                case JoystickButtons.RY:
                    state.ry = deadband(((update.Value / 65535.0) - 0.5) * -2.0, 0.25);
                    data = "R " + calcJoystick(state.rx, state.ry);
                    isJoystickUpdate = true;
                    isJoystickZero = ((state.rx == 0) && (state.ry == 0));
                    break;
                default:
                    data = update.ToString();
                    break;
            }
            return new ParseUpdateResult(state, data, isJoystickUpdate, isJoystickZero, btn);
        }

        static void Main(string[] args)
        {
            /** BLUETOOTH INIT **/
            // Replace this COM port by the appropriate one on your computer
            SerialPort arduino = new SerialPort("COM7");

            //Open Arduino connection
            while (!arduino.IsOpen)
            {
                Console.WriteLine("Attempting to connect to Arduino (COM 7)...");
                try
                {
                    arduino.Open();
                } catch (Exception)
                {
                    System.Threading.Thread.Sleep(1000);
                }
            }
            Console.WriteLine("Connected to Arduino!");

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

            /** EVENT LOOP **/
            const int ROLL_SIZE = 5;
            const int MAX_TIMEOUT = 50; //ms between first and last push
            int queueIndex = 0;
            DateTime queueStartTime = DateTime.Now;
            PartialJoystickState prevState = new PartialJoystickState();
            while (true)
            {
                try
                {
                    //Poll joystick
                    joystick.Poll();
                    var updates = joystick.GetBufferedData();
                    //JoystickState state = joystick.GetCurrentState();

                    foreach (JoystickUpdate update in updates)
                    {
                        ParseUpdateResult result = parseUpdate(update, partialState);
                        partialState = result.state;
                        Console.WriteLine(result.update);

                        //Add to queue if joystick update
                        bool flush = true; //should write to Arduino?
                        if (result.isJoystick)
                        {
                            if (queueIndex == 0) { queueStartTime = DateTime.Now; }
                            queueIndex++;

                            //Check whether we should flush the state
                            TimeSpan tdiff = DateTime.Now - queueStartTime;
                            //Clear immediately if joystick state is zero
                            if (result.isJoystickZero)
                            {
                                queueIndex = 0;

                                //definitely flush
                                flush = true;
                            }
                            else if ((queueIndex == ROLL_SIZE) || (tdiff.TotalMilliseconds > MAX_TIMEOUT))
                            {
                                //For now, just get last state when flushing

                                //Clear
                                queueIndex = 0;
                                //Flush
                                flush = true;
                            }
                            else
                            {
                                flush = false;
                            }
                            prevState = result.state;
                        }

                        try
                        {
                            if (flush) arduino.WriteLine(result.update);
                        }
                        catch (Exception)
                        {
                            if (!arduino.IsOpen)
                            {
                                do
                                {
                                    Console.WriteLine("Reconnecting to Arduino (COM 7)...");
                                    try
                                    {
                                        arduino.Open();
                                    }
                                    catch (Exception)
                                    {
                                        System.Threading.Thread.Sleep(1000);
                                    }
                                } while (!arduino.IsOpen);
                                Console.WriteLine("Connected to Arduino!");
                            }
                            else
                            {
                                Console.WriteLine("Unknown Bluetooth exception. Restart program.");
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Error reading from joystick.");
                    bool okay = false;
                    while(!okay)
                    {
                        try
                        {
                            joystick.Unacquire();
                            joystick.Acquire();
                            okay = true;
                        }
                        catch (Exception)
                        {

                        }
                    }
                }
            }
        }
    }
}

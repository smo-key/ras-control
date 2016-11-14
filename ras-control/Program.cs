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

        enum JoystickType
        {
            NONE = 0,
            LEFT = 1,
            RIGHT = 2
        }

        enum ControllerUpdateType
        {
            BUTTON = 0,
            JOYSTICK = 1
        }

        class ControllerUpdate
        {
            public ControllerUpdateType updateType { get; protected set; }
        }

        class JoystickControlUpdate : ControllerUpdate
        {
            public JoystickButtons btn { get; private set; }
            public double x { get; set; }
            public double y { get; set; }

            public JoystickControlUpdate()
            {
                btn = JoystickButtons.LX;
                x = 0.0;
                y = 0.0;
                updateType = ControllerUpdateType.JOYSTICK;
            }

            public JoystickControlUpdate(JoystickButtons btn, double x, double y)
            {
                this.btn = btn;
                this.x = x;
                this.y = y;
                updateType = ControllerUpdateType.JOYSTICK;
            }

            public JoystickType getJoystick()
            {
                switch (btn)
                {
                    case JoystickButtons.LX:
                    case JoystickButtons.LY:
                        return JoystickType.LEFT;
                    case JoystickButtons.RX:
                    case JoystickButtons.RY:
                        return JoystickType.RIGHT;
                    default:
                        return JoystickType.NONE;
                }
            }

            public bool isZero()
            {
                return (x == 0) && (y == 0);
            }

            public string ToShortString()
            {
                return btn.ToString().Substring(0, 1) + " " + ((int)(x*100)).ToString() + " " + ((int)(y*100.0)).ToString();
            }

            public string ToBinnedString(int direction)
            {
                
                return (btn.ToString().Substring(0, 1) == "L" ? (direction == 1 ? "R" : "L") : (direction == 1 ? "L" : "R")) + " " + (getBinX()*BIN_STEP).ToString() + " " + (getBinY()*BIN_STEP).ToString();
            }

            public override string ToString()
            {
                return btn.ToString().Substring(0, 1) + " " + x.ToString("N6") + " " + y.ToString("N6");
            }

            public string ToLongString()
            {
                return btn.ToString().Substring(0, 1) + " " + x.ToString("N6") + " " + y.ToString("N6") + " " + r.ToString("N6") + " " + theta.ToString("N2");
            }

            public double theta
            {
                get
                {
                    double r = Math.Sqrt((x * x) + (y * y));
                    double theta = 90.0 - (Math.Atan2(y, x) / Math.PI * 180.0);
                    theta = ((theta > 180.0) && (theta <= 270.0)) ? theta - 360.0 : theta;
                    theta = (r == 0) ? 0 : theta;
                    return theta;
                }
            }

            public double r
            {
                get
                {
                    double r = Math.Sqrt((x * x) + (y * y));
                    r = (r > 1) ? 1 : r;
                    return r;
                }
            }

            private static int BINS = 10;
            private static int BIN_STEP = 100 / BINS;
            public int getBinX()
            {
                return (int)(x * 100.0) / BIN_STEP;
            }
            public int getBinY()
            {
                return (int)(y * 100.0) / BIN_STEP;
            }
        }

        class ButtonUpdate : ControllerUpdate
        {
            public JoystickButtons btn { get; private set; }
            public bool pressed { get; private set; }

            public ButtonUpdate(JoystickButtons btn, bool pressed)
            {
                this.btn = btn;
                this.pressed = pressed;
                updateType = ControllerUpdateType.BUTTON;
            }

            public override string ToString()
            {
                return btn.ToString() + " " + (pressed ? "1" : "0");
            }
        }

        static double deadband(double value, double minabs)
        {
            return Math.Abs(value) < minabs ? 0 : value;
        }
        
        static ControllerUpdate parseUpdate(JoystickUpdate update, JoystickControlUpdate l, JoystickControlUpdate r, double direction)
        {
            JoystickButtons btn = (JoystickButtons)update.Offset;
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
                    return new ButtonUpdate(btn, update.Value == 128);
                //case JoystickButtons.POV: //Top, Bottom, Left, Right
                //    double val = update.Value / 100;
                //    val = ((val > 180.0) && (val < 360.0)) ? val - 360.0 : val;
                //    val = (update.Value == -1) ? -1 : val;
                //    data += " " + ((update.Value == 0) || (update.Value == 4500) || (update.Value == 31500) ? 1 : 0) + " "
                //                + ((update.Value == 18000) || (update.Value == 13500) || (update.Value == 22500) ? 1 : 0) + " "
                //                + ((update.Value == 27000) || (update.Value == 22500) || (update.Value == 31500) ? 1 : 0) + " "
                //                + ((update.Value == 9000) || (update.Value == 4500) || (update.Value == 13500) ? 1 : 0) + " " + (val).ToString();
                //    break;
                case JoystickButtons.LX:
                    return new JoystickControlUpdate(btn, deadband(((update.Value / 65535.0) - 0.5) * -2.0 * direction, 0.25), l.y);
                case JoystickButtons.LY:
                    return new JoystickControlUpdate(btn, l.x, deadband(((update.Value / 65535.0) - 0.5) * 2.0 * direction, 0.25));
                case JoystickButtons.RX:
                    return new JoystickControlUpdate(btn, deadband(((update.Value / 65535.0) - 0.5) * -2.0 * direction, 0.25), r.y);
                case JoystickButtons.RY:
                    return new JoystickControlUpdate(btn, r.x, deadband(((update.Value / 65535.0) - 0.5) * 2.0 * direction, 0.25));
                default:
                    return null;
            }

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
            JoystickControlUpdate joyL = new JoystickControlUpdate();
            JoystickControlUpdate joyR = new JoystickControlUpdate();

            int direction = 1; //direction switch
            bool lastYState = false;

            /** EVENT LOOP **/
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
                        ControllerUpdate result = parseUpdate(update, joyL, joyR, direction);
                        if (result.updateType == ControllerUpdateType.BUTTON)
                        {
                            ButtonUpdate btnUpdate = (ButtonUpdate)result;

                            Console.WriteLine(btnUpdate.ToString());

                            //We have nothing to do with buttons right now

                            if ((btnUpdate.btn == JoystickButtons.Y) && (btnUpdate.pressed == true) && (lastYState == false))
                            {
                                direction = -direction;
                            }
                            if (btnUpdate.btn == JoystickButtons.Y)
                            {
                                lastYState = btnUpdate.pressed;
                            }
                        }
                        else if (result.updateType == ControllerUpdateType.JOYSTICK)
                        {
                            //TODO put everything below in here
                            JoystickControlUpdate joyUpdate = (JoystickControlUpdate)result;

                            //Flush if previous bin is not equal to previous bin
                            bool flush = false;
                            if (joyUpdate.getJoystick() == JoystickType.LEFT) {
                                flush = (joyL.getBinX() != joyUpdate.getBinX()) || (joyL.getBinY() != joyUpdate.getBinY());
                                joyL = joyUpdate;
                            }
                            if (joyUpdate.getJoystick() == JoystickType.RIGHT)
                            {
                                flush = (joyR.getBinX() != joyUpdate.getBinX()) || (joyR.getBinY() != joyUpdate.getBinY());
                                joyR = joyUpdate;
                            }

                            try
                            {
                                Console.WriteLine(joyUpdate.ToBinnedString(direction));
                                if (flush) arduino.WriteLine(joyUpdate.ToBinnedString(direction));
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

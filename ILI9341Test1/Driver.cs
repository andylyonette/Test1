using System;
using System.Text;
using System.Threading;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;

namespace ILI9341Test1
{
    public partial class Driver
    {
        const byte lcdPortraitConfig = 8 | 0x40;  //Change for reversed portrait letters issue
        const byte lcdLandscapeConfig = 44;

        private readonly GpioPin _dataCommandPort;
        private readonly GpioPin _resetPort;
        private readonly GpioPin _backlightPort;
        private readonly SpiDevice _spi;
        private readonly SpiController _spiController;
        private readonly GpioController _gpioController;
        private bool _isLandscape;
        private bool _backlightOn;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool BacklightOn
        {
            get { return _backlightOn; }
            set {
                if (_backlightPort != null)
                {
                    if (value)
                    {
                        _backlightPort.Write(GpioPinValue.High);
                    }
                    else
                    {
                        _backlightPort.Write(GpioPinValue.Low);
                    }
                    _backlightOn = value;
                }
            }
        }

        #region Constructors
        public Driver(bool isLandscape,
                      int lcdChipSelectPin,
                      int dataCommandPin,
                      int resetPin,
                      int backlightPin,
                      string spiBus,
                      uint spiClockFrequency = 20000
            )
        {
            _spiController = SpiController.GetDefault();
            _gpioController = GpioController.GetDefault();
            SpiConnectionSettings _csConnectionSettings = new SpiConnectionSettings(lcdChipSelectPin);
            _spi = SpiDevice.FromId(spiBus, _csConnectionSettings);

            _dataCommandPort = _gpioController.OpenPin(dataCommandPin);
            _dataCommandPort.SetDriveMode(GpioPinDriveMode.Output);

            _resetPort = _gpioController.OpenPin(resetPin);
            _resetPort.SetDriveMode(GpioPinDriveMode.Output);

            _backlightPort = _gpioController.OpenPin(backlightPin);
            _backlightPort.SetDriveMode(GpioPinDriveMode.Output);

            InitializeScreen();
            SetOrientation(isLandscape);
        }

        #endregion Constructors


        #region Communication Methods
        protected virtual void Write(byte[] data)
        {
            _spi.Write(data);
        }

        protected virtual void Write(ushort[] data)
        {
            _spi.Write(data);
        }

        protected virtual void SendCommand(Commands command)
        {
            _dataCommandPort.Write(GpioPinValue.Low);
            Write(new[] { (byte)command });
        }

        protected virtual void SendData(params byte[] data)
        {
            _dataCommandPort.Write(GpioPinValue.High);
            Write(data);
        }

        protected virtual void SendData(params ushort[] data)
        {
            _dataCommandPort.Write(GpioPinValue.High);
            Write(data);
        }

        protected virtual void WriteReset(bool value)
        {
            lock (this)
            {
                if (_resetPort != null)
                {
                    if (value)
                    {
                        _resetPort.Write(GpioPinValue.High);
                    }
                    else
                    {
                        _resetPort.Write(GpioPinValue.Low);
                    }
                }
            }
        }

        #endregion Communication Methods

        #region Public Methods

        public void FillScreen(int left, int right, int top, int bottom, ushort color)
        {
            lock (this)
            {
                SetWindow(left, right, top, bottom);
                var buffer = new ushort[Width];

                if (color != 0)
                {
                    for (var i = 0; i < Width; i++)
                    {
                        buffer[i] = color;
                    }
                }

                for (int y = 0; y < Height; y++)
                {
                    SendData(buffer);
                }
            }
        }

        public void ClearScreen()
        {
            lock (this)
            {
                FillScreen(0, Width - 1, 0, Height - 1, 0);
            }
        }

        public void SetPixel(int x, int y, ushort color)
        {
            lock (this)
            {
                SetWindow(x, x, y, y);
                SendData(color);
            }
        }

        public void SetOrientation(bool isLandscape)
        {
            lock (this)
            {
                _isLandscape = isLandscape;
                SendCommand(Commands.MemoryAccessControl);

                if (isLandscape)
                {
                    SendData(lcdLandscapeConfig);
                    Width = 320;
                    Height = 240;
                }
                else
                {
                    SendData(lcdPortraitConfig);
                    Width = 240;
                    Height = 320;

               }

                SetWindow(0, Width - 1, 0, Height - 1);
            }
        }

        public void ScrollUp(int pixels)
        {
            lock (this)
            {
                SendCommand(Commands.VerticalScrollingStartAddress);
                SendData((ushort)pixels);
                SendCommand(Commands.MemoryWrite);
           }
       }
        #endregion Public Methods

        protected virtual void InitializeScreen()
        {
            lock (this)
            {
                WriteReset(false);
                Thread.Sleep(10);
                WriteReset(true);
                SendCommand(Commands.SoftwareReset);
                Thread.Sleep(10);
                SendCommand(Commands.DisplayOff);

                SendCommand(Commands.MemoryAccessControl);
                SendData(lcdPortraitConfig);

                SendCommand(Commands.PixelFormatSet);
                SendData(0x55);//16-bits per pixel

                SendCommand(Commands.FrameControlNormal);
                SendData(0x00, 0x1B);

                SendCommand(Commands.GammaSet);
                SendData(0x01);

                SendCommand(Commands.ColumnAddressSet); //width of the screen
                SendData(0x00, 0x00, 0x00, 0xEF);

                SendCommand(Commands.PageAddressSet); //height of the screen
                SendData(0x00, 0x00, 0x01, 0x3F);

                SendCommand(Commands.EntryModeSet);
                SendData(0x07);

                SendCommand(Commands.DisplayFunctionControl);
                SendData(0x0A, 0x82, 0x27, 0x00);

                SendCommand(Commands.SleepOut);
                Thread.Sleep(120);

               SendCommand(Commands.DisplayOn);
                Thread.Sleep(100);

                SendCommand(Commands.MemoryWrite);
            }
        }

        public static ushort ColorFromRgb(byte r, byte g, byte b)
        {
            return (ushort)((r << 11) | (g << 5) | b);
        }

        void SetWindow(int left, int right, int top, int bottom)
        {
            lock (this)
            {
                SendCommand(Commands.ColumnAddressSet);
                SendData((byte)((left >> 8) & 0xFF),
                         (byte)(left & 0xFF),
                         (byte)((right >> 8) & 0xFF),
                         (byte)(right & 0xFF));
                SendCommand(Commands.PageAddressSet);
                SendData((byte)((top >> 8) & 0xFF),
                         (byte)(top & 0xFF),
                         (byte)((bottom >> 8) & 0xFF),
                         (byte)(bottom & 0xFF));
                SendCommand(Commands.MemoryWrite);
            }
        }
    }
}

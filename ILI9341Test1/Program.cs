using nanoFramework.Hardware.Esp32;
using System;
using System.Threading;
using Windows.Devices.Gpio;
using Windows.Devices.I2c;
using Windows.Devices.Spi;

namespace ILI9341Test1
{
    public class Program
    {
        public static void Main()
        {
            var tft = new Driver(isLandscape: true,
                                 lcdChipSelectPin: 0,
                                 dataCommandPin: 2,
                                 resetPin: 13,
                                 backlightPin: 21,
                                 spiBus: "SPI14"
                            );


            var font = new StandardFixedWidthFont();

            tft.ClearScreen();

            tft.DrawString(10, 10, "Hello world!", 0xF800, font);

            tft.BacklightOn = true;
            while (true)
            {
                Thread.Sleep(1000);
            }
        }
    }
}


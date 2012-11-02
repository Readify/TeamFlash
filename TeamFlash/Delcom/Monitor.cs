using System;
using System.Linq;
using System.Text;
using System.Threading;

namespace TeamFlash.Delcom
{
    class Monitor
    {
        readonly object _lockObject = new Object();

        public LedColour CurrentColour { get; private set; }

        private void SetLed(byte led, bool turnItOn, bool flashIt, int? flashDurationInSeconds)
        {
            SetLed(led, turnItOn, flashIt, flashDurationInSeconds, false);
        }

        private void SetLed(byte led, bool turnItOn, bool flashIt, int? flashDurationInSeconds = null, bool turnOffAfterFlashing = false)
        {
            var hUsb = GetDelcomDeviceHandle(); // open the device
            if (hUsb == 0) return;
            if (turnItOn)
            {
                if (flashIt)
                {
                    DelcomBuildIndicator.DelcomLEDControl(hUsb, led, DelcomBuildIndicator.LEDFLASH);
                    if (flashDurationInSeconds.HasValue)
                    {
                        Thread.Sleep(flashDurationInSeconds.Value * 1000);
                        var ledStatus = turnOffAfterFlashing ? DelcomBuildIndicator.LEDOFF : DelcomBuildIndicator.LEDON;
                        DelcomBuildIndicator.DelcomLEDControl(hUsb, led, ledStatus);
                    }
                }
                else
                {
                    DelcomBuildIndicator.DelcomLEDControl(hUsb, led, DelcomBuildIndicator.LEDON);
                }
            }
            else
            {
                DelcomBuildIndicator.DelcomLEDControl(hUsb, led, DelcomBuildIndicator.LEDOFF);
            }
            DelcomBuildIndicator.DelcomCloseDevice(hUsb);
        }

        readonly StringBuilder _deviceName = new StringBuilder(DelcomBuildIndicator.MAXDEVICENAMELEN);

        uint GetDelcomDeviceHandle()
        {
            if (String.IsNullOrEmpty(_deviceName.ToString()))
            {
                // Search for the first match USB device, For USB IO Chips use USBIODS
                var devicesFound = DelcomBuildIndicator.DelcomGetNthDevice(0, 0, _deviceName);

                if (devicesFound == 0)
                    Console.WriteLine("Can't find build light device, or it's in use by another program");
            }

            var hUsb = DelcomBuildIndicator.DelcomOpenDevice(_deviceName, 0); // open the device
            return hUsb;
        }

        public void TestLights()
        {
            TurnOnFailLight();
            Thread.Sleep(800);
            TurnOffLights();
            Thread.Sleep(200);
            TurnOnWarningLight();
            Thread.Sleep(800);
            TurnOffLights();
            Thread.Sleep(200);
            TurnOnSuccessLight();
            Thread.Sleep(800);
            TurnOffLights();
            Thread.Sleep(200);
        }

        public void TurnOnSuccessLight()
        {
            ChangeColor(LedColour.Green);
        }

        public void TurnOnWarningLight()
        {
            ChangeColor(LedColour.Yellow);
        }

        public void TurnOnFailLight()
        {
            ChangeColor(LedColour.Red);
        }

        public void TurnOffLights()
        {
            ChangeColor(LedColour.Off);
        }

        public void Blink()
        {
            var oldColour = CurrentColour;
            TurnOffLights();
            Thread.Sleep(100);
            ChangeColor(oldColour);
        }

        public void BlinkThenRevert(LedColour colour, int blinkInterval = 100)
        {
            var oldColour = CurrentColour;
            TurnOffLights();
            Thread.Sleep(blinkInterval);
            ChangeColor(colour);
            Thread.Sleep(blinkInterval);
            ChangeColor(oldColour);
        }

        public void Disco(double intervalInSeconds)
        {
            var until = DateTime.Now.AddSeconds(intervalInSeconds);
            var oldColour = CurrentColour;
            while (DateTime.Now < until)
            {
                foreach (var color in Enum.GetValues(typeof (LedColour)).Cast<LedColour>())
                {
                    ChangeColor(color);
                    Thread.Sleep(100);
                }
            }
            ChangeColor(oldColour);
        }

        private void ChangeColor(LedColour colour)
        {
            lock (_lockObject)
            {
                switch (colour)
                {
                    case LedColour.Red:
                        CurrentColour = LedColour.Red;
                        SetRGB(true, false, false);
                        break;
                    case LedColour.Green:
                        CurrentColour = LedColour.Green;
                        SetRGB(false, true,false);
                        break;
                    case LedColour.Blue:
                        CurrentColour = LedColour.Blue;
                        SetRGB(false, false,true);
                        break;
                    case LedColour.Yellow:
                        CurrentColour = LedColour.Yellow;
                        SetRGB(false, true,true);
                        break;
                    case LedColour.White:
                        CurrentColour = LedColour.White;
                        SetRGB(true, true,true);
                        break;
                    case LedColour.Purple:
                        CurrentColour = LedColour.Purple;
                        SetRGB(true, false,true);
                        break;
                    case LedColour.Off:
                        CurrentColour = LedColour.Off;
                        SetRGB(false, false,false);
                        break;
                }
            }
        }

        private void SetRGB(bool red, bool green, bool blue)
        {
            SetLed(DelcomBuildIndicator.REDLED, red, false);
            SetLed(DelcomBuildIndicator.GREENLED, green, false);
            SetLed(DelcomBuildIndicator.BLUELED, blue, false);
        }
    }

    internal enum LedColour
    {
        Red,
        Blue,
        Green,
        Yellow,
        White,
        Purple,
        Off
    }
}

using System;
using System.Linq;
using System.Text;
using System.Threading;

namespace TeamFlash.Delcom
{
    class Monitor
    {
        private LedColour _currentColour;
        readonly object _lockObject = new Object();

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
            var oldColour = _currentColour;
            TurnOffLights();
            Thread.Sleep(200);
            ChangeColor(oldColour);
        }

        public void Disco(double intervalInSeconds)
        {
            var until = DateTime.Now.AddSeconds(intervalInSeconds);
            var oldColour = _currentColour;
            while (DateTime.Now < until)
            {
                foreach (var color in Enum.GetValues(typeof (LedColour)).Cast<LedColour>())
                {
                    ChangeColor(color);
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
                        _currentColour = LedColour.Red;
                        SetLed(DelcomBuildIndicator.REDLED, true, false);
                        SetLed(DelcomBuildIndicator.GREENLED, false, false);
                        SetLed(DelcomBuildIndicator.BLUELED, false, false);
                        break;
                    case LedColour.Green:
                        _currentColour = LedColour.Red;
                        SetLed(DelcomBuildIndicator.REDLED, false, false);
                        SetLed(DelcomBuildIndicator.GREENLED, true, false);
                        SetLed(DelcomBuildIndicator.BLUELED, false, false);
                        break;
                    case LedColour.Blue:
                        _currentColour = LedColour.Blue;
                        SetLed(DelcomBuildIndicator.REDLED, false, false);
                        SetLed(DelcomBuildIndicator.GREENLED, false, false);
                        SetLed(DelcomBuildIndicator.BLUELED, true, false);
                        break;
                    case LedColour.Yellow:
                        _currentColour = LedColour.Red;
                        SetLed(DelcomBuildIndicator.REDLED, false, false);
                        SetLed(DelcomBuildIndicator.GREENLED, true, false);
                        SetLed(DelcomBuildIndicator.BLUELED, true, false);
                        break;
                    case LedColour.White:
                        _currentColour = LedColour.Red;
                        SetLed(DelcomBuildIndicator.REDLED, true, false);
                        SetLed(DelcomBuildIndicator.GREENLED, true, false);
                        SetLed(DelcomBuildIndicator.BLUELED, true, false);
                        break;
                    case LedColour.Purple:
                        _currentColour = LedColour.Blue;
                        SetLed(DelcomBuildIndicator.REDLED, true, false);
                        SetLed(DelcomBuildIndicator.GREENLED, false, false);
                        SetLed(DelcomBuildIndicator.BLUELED, true, false);
                        break;
                    case LedColour.Off:
                        _currentColour = LedColour.Blue;
                        SetLed(DelcomBuildIndicator.REDLED, false, false);
                        SetLed(DelcomBuildIndicator.GREENLED, false, false);
                        SetLed(DelcomBuildIndicator.BLUELED, false, false);
                        break;
                }
            }
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

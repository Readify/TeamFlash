using System;
using System.Text;
using System.Threading;

namespace TeamFlash.Delcom
{
    class DelcomBuildLight : BuildLightBase, IBuildLight
    {
        readonly object _lockObject = new Object();

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

        private void SetRGB(bool red, bool green, bool blue)
        {
            SetLed(DelcomBuildIndicator.REDLED, red, false);
            SetLed(DelcomBuildIndicator.GREENLED, green, false);
            SetLed(DelcomBuildIndicator.BLUELED, blue, false);
        }

        protected override void ChangeColor(LightColour colour)
        {

            {
                lock (_lockObject)
                {
                    switch (colour)
                    {
                        case LightColour.Red:
                            CurrentColour = LightColour.Red;
                            SetRGB(true, false, false);
                            break;
                        case LightColour.Green:
                            CurrentColour = LightColour.Green;
                            SetRGB(false, true, false);
                            break;
                        case LightColour.Blue:
                            CurrentColour = LightColour.Blue;
                            SetRGB(false, false, true);
                            break;
                        case LightColour.Yellow:
                            CurrentColour = LightColour.Yellow;
                            SetRGB(false, true, true);
                            break;
                        case LightColour.White:
                            CurrentColour = LightColour.White;
                            SetRGB(true, true, true);
                            break;
                        case LightColour.Purple:
                            CurrentColour = LightColour.Purple;
                            SetRGB(true, false, true);
                            break;
                        case LightColour.Off:
                            CurrentColour = LightColour.Off;
                            SetRGB(false, false, false);
                            break;
                    }
                }
            }
        }
    }
}

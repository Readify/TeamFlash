using System;
using System.Text;
using System.Threading;

namespace TeamFlash
{
    class Monitor
    {
        public void SetLed(byte led, bool turnItOn, bool flashIt)
        {
            SetLed(led, turnItOn, flashIt, null, false);
        }

        public void SetLed(byte led, bool turnItOn, bool flashIt, int? flashDurationInSeconds)
        {
            SetLed(led, turnItOn, flashIt, flashDurationInSeconds, false);
        }

        public void SetLed(byte led, bool turnItOn, bool flashIt, int? flashDurationInSeconds, bool turnOffAfterFlashing)
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

        readonly StringBuilder deviceName = new StringBuilder(DelcomBuildIndicator.MAXDEVICENAMELEN);

        uint GetDelcomDeviceHandle()
        {
            if (string.IsNullOrEmpty(deviceName.ToString()))
            {
                // Search for the first match USB device, For USB IO Chips use USBIODS
                var devicesFound = DelcomBuildIndicator.DelcomGetNthDevice(0, 0, deviceName);

                if (devicesFound == 0)
                    Console.WriteLine("Can't find build light device, or it's in use by another program");
            }

            var hUsb = DelcomBuildIndicator.DelcomOpenDevice(deviceName, 0); // open the device
            return hUsb;
        }
    }
}

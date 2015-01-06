using System.Text;
using System.Threading;

namespace TeamFlash
{
    internal class Monitor
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
            uint deviceHandle = GetDelcomDeviceHandle(); // open the device
            if (deviceHandle == 0) return;

            if (turnItOn)
            {
                if (flashIt)
                {
                    DelcomBuildIndicator.DelcomLEDControl(deviceHandle, led, DelcomBuildIndicator.LEDFLASH);
                    if (flashDurationInSeconds.HasValue)
                    {
                        Thread.Sleep(flashDurationInSeconds.Value*1000);
                        var ledStatus = turnOffAfterFlashing ? DelcomBuildIndicator.LEDOFF : DelcomBuildIndicator.LEDON;
                        DelcomBuildIndicator.DelcomLEDControl(deviceHandle, led, ledStatus);
                    }
                }
                else
                {
                    DelcomBuildIndicator.DelcomLEDControl(deviceHandle, led, DelcomBuildIndicator.LEDON);
                }
            }
            else
            {
                DelcomBuildIndicator.DelcomLEDControl(deviceHandle, led, DelcomBuildIndicator.LEDOFF);
            }

            DelcomBuildIndicator.DelcomCloseDevice(deviceHandle);
        }

        private readonly StringBuilder deviceName = new StringBuilder(DelcomBuildIndicator.MAXDEVICENAMELEN);

        private uint GetDelcomDeviceHandle()
        {
            if (string.IsNullOrEmpty(deviceName.ToString()))
            {
                // Search for the first match USB device, For USB IO Chips use USBIODS
                DelcomBuildIndicator.DelcomGetNthDevice(DelcomBuildIndicator.USBDELVI, 0, deviceName);
            }

            var hUsb = DelcomBuildIndicator.DelcomOpenDevice(deviceName, 0); // open the device
            return hUsb;
        }
    }
}
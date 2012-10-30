using System;
using System.Text;
using System.Threading;

namespace TeamFlash
{
    class Monitor
    {
        public void SetLed(byte led, bool turnItOn, bool flashIt, int? flashDurationInSeconds)
        {
            if (flashDurationInSeconds != null)
                SetLed(led, turnItOn, flashIt, flashDurationInMilliSeconds: flashDurationInSeconds.Value * 1000);
            else
                SetLed(led, turnItOn, flashIt);
        }

        public void SetLed(byte led, bool turnItOn, bool flashIt, bool turnOffAfterFlashing = false , float? flashDurationInMilliSeconds = null, float? flashFrequency = null, Func<bool> finishWhen = null)
        {
            try
            {

                var hUsb = GetDelcomDeviceHandle(); // open the device
                DateTime? until = null;
                if (finishWhen == null)
                {
                    finishWhen = () =>
                        {
                            if (!until.HasValue)
                                until = DateTime.Now.AddSeconds(Convert.ToInt32(flashDurationInMilliSeconds/1000));
                            return DateTime.Now < until;
                        };
                }
                if (hUsb == 0) return;
                if (turnItOn)
                {
                    if (flashIt)
                    {
                        if (flashFrequency.HasValue)
                        {
                            var toggle = true;
                            while (finishWhen())
                            {

                                DelcomBuildIndicator.DelcomLEDControl(hUsb, led,
                                                                      toggle
                                                                          ? DelcomBuildIndicator.LEDOFF
                                                                          : DelcomBuildIndicator.LEDON);
                                Thread.Sleep(Convert.ToInt16(flashFrequency.Value));
                                toggle = !toggle;
                                switch (led)
                                {
                                    case DelcomBuildIndicator.GREENLED:
                                        led = DelcomBuildIndicator.BLUELED;
                                        break;
                                    case DelcomBuildIndicator.BLUELED:
                                        led = DelcomBuildIndicator.REDLED;
                                        break;
                                    case DelcomBuildIndicator.REDLED:
                                        led = DelcomBuildIndicator.GREENLED;
                                        break;
                                }
                            }
                            var ledStatus = turnOffAfterFlashing
                                                ? DelcomBuildIndicator.LEDOFF
                                                : DelcomBuildIndicator.LEDON;

                        }
                        else
                        {
                            DelcomBuildIndicator.DelcomLEDControl(hUsb, led, DelcomBuildIndicator.LEDFLASH);
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
            catch(Exception){}
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

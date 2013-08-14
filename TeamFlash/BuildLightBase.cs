using System;
using System.Linq;
using System.Threading;

namespace TeamFlash
{
    abstract class BuildLightBase
    {
        public LightColour CurrentColour { get; protected set; }

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
            ChangeColor(LightColour.Green);
        }

        public void TurnOnWarningLight()
        {
            ChangeColor(LightColour.Yellow);
        }

        public void TurnOnFailLight()
        {
            ChangeColor(LightColour.Red);
        }

        public void TurnOffLights()
        {
            ChangeColor(LightColour.Off);
        }

        public void Blink()
        {
            var oldColour = CurrentColour;
            TurnOffLights();
            Thread.Sleep(100);
            ChangeColor(oldColour);
        }

        public void BlinkThenRevert(LightColour colour, int blinkInterval = 100)
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
                foreach (var color in Enum.GetValues(typeof (LightColour)).Cast<LightColour>())
                {
                    ChangeColor(color);
                    Thread.Sleep(100);
                }
            }
            ChangeColor(oldColour);
        }

        protected abstract void ChangeColor(LightColour colour);
    }
}
namespace TeamFlash
{
    public interface IBuildLight
    {
        LightColour CurrentColour { get; }
        void TestLights();
        void TurnOnSuccessLight();
        void TurnOnWarningLight();
        void TurnOnFailLight();
        void TurnOffLights();
        void Blink();
        void BlinkThenRevert(LightColour colour, int blinkInterval = 100);
        void Disco(double intervalInSeconds);
    }
}
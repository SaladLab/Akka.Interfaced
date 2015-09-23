namespace Akka.Interfaced
{
    // Followings are special traits for InterfacedPoisonPill compared with PoisonPill
    // - It handles async messages of InterfacedMessage
    // - It calls OnPreStop handler before stopping.
    public class InterfacedPoisonPill
    {
        public static InterfacedPoisonPill Instance = new InterfacedPoisonPill();
    }
}

namespace RailArena
{
    /// <summary>A valid click survives render frames until one simulation sample; blocked clicks never queue.</summary>
    public sealed class WeaponClickLatch
    {
        private bool pending;
        public void Sample(bool pressedThisFrame,bool canFire){if(pressedThisFrame&&canFire)pending=true;}
        public bool Consume(){bool shot=pending;pending=false;return shot;}
        public void Reset()=>pending=false;
    }
}

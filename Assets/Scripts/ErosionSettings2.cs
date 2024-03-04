using System;

namespace Assets.Scripts
{
    [Serializable]
    public class ErosionSettings2
    {
        public int particleCount = 50_000;

        public int maxParticleLife = 30;
        public int erosionBlurRadius = 1;
        public float erosionBlurWeight = .5f;
        public float erosionSpeed = .7f;

        public float inertia = .5f;
        public float minSlopeParam = 0.01f;
        public float particleCapacity = .5f;
        public float particleDepositionSpeed = .5f;
        public int particleErosionRadius = 1;
        public float gravity = 4;
        public float particleEvaporation = .25f;
    }
}
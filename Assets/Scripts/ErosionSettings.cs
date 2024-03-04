using System;

namespace Assets.Scripts
{
    [Serializable]
    public class ErosionSettings
    {
        public int dropletCycles = 50000;
        public float dropletMass = 50;
        public float saturationMaxProportion = .25f;
        public float saturationSpeed = 0.05f;
        public float evaporationSpeed = 0.005f;

        public float FRICTION = .5f;
        public float HEIGHT_TO_METERS = 20_000;
        public float BLOCK_DISTANCE = 1_000;
    }
}
namespace Assets.Scripts
{
    public class ErosionHeatMapsDebug
    {
        public float[,] heatmapTime;
        public float[,] heatmapVisits;
        public float[,] heatmapStarts;
        public float[,] heatmapSaturationChanges;

        public ErosionHeatMapsDebug(int width, int height)
        {
            heatmapVisits = new float[width, height];
            heatmapStarts = new float[width, height];
            heatmapTime = new float[width, height];
            heatmapSaturationChanges = new float[width, height];
        }
    }
}
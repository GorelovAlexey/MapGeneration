using UnityEngine;

namespace Assets.Scripts
{
    public static class Util
    {
        public static float[,] NormalizeHeightMap(this float[,] map)
        {
            if (map == null || map.Length <= 0)
                return map;

            var min = map[0, 0];
            var max = min;

            foreach (var v in map)
            {
                if (v < min)
                    min = v;
                if (v > max)
                    max = v;
            }

            for (int x = 0; x < map.GetLength(0); x++)
            {
                for (int y = 0; y < map.GetLength(1); y++)
                {
                    map[x, y] = Mathf.InverseLerp(min, max, map[x, y]);
                }
            }

            return map;
        }

        public static void SumMaps(this float[,] map, params (float[,] map, float coefficient)[] addMaps)
        {
            var width = map.GetLength(0);
            var height = map.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int i = 0; i < addMaps.Length; i++)
                    {
                        map[x, y] += addMaps[i].map[x, y] * addMaps[i].coefficient;
                    }
                }
            }
        }

        public static void SumMaps(this float[,] map, params float[][,] addMaps)
        {
            var width = map.GetLength(0);
            var height = map.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    foreach (var m in addMaps)
                    {
                        map[x, y] += m[x, y];
                    }
                }
            }
        }

    }
}
using UnityEngine;

namespace Assets.Scripts
{
    public static class NoiseGenerator
    {
        public static float[,] GenerateNoise(int width, int height, float scale, int octaves, float persistance, float lacunarity, int seed)
        {
            var result = new float[width, height];
            var rnd = new System.Random(seed);
            var offsets = new Vector2[octaves];
            for (int i = 0; i < octaves; i++)
            {
                offsets[i] = new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble());
            }

            var minValue = float.MaxValue;
            var maxValue = float.MinValue;

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var amp = 1f;
                    var freq = 1f;
                    var noiseHeight = 0f;

                    for (int i = 0; i < octaves; i++)
                    {
                        var sampleX = x * scale * freq + offsets[i].x;
                        var sampleY = y * scale * freq + offsets[i].y;

                        noiseHeight += Mathf.PerlinNoise(sampleX, sampleY) * amp;

                        amp *= persistance;
                        freq *= lacunarity;
                    }

                    result[x, y] = noiseHeight;

                    if (noiseHeight > maxValue) maxValue = noiseHeight;
                    if (noiseHeight < minValue) minValue = noiseHeight;
                }
            }

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    result[x, y] = Mathf.InverseLerp(minValue, maxValue, result[x, y]);
                }
            }

            return result;
        }


        public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset) 
        {
            float[,] noiseMap = new float[mapWidth, mapHeight];

            System.Random prng = new System.Random(seed);
            Vector2[] octaveOffsets = new Vector2[octaves];
            for (int i = 0; i<octaves; i++) {
                float offsetX = prng.Next(-100000, 100000) + offset.x;
                float offsetY = prng.Next(-100000, 100000) + offset.y;
                octaveOffsets[i] = new Vector2(offsetX, offsetY);
            }

            if (scale <= 0) {
                scale = 0.0001f;
            }

            float maxNoiseHeight = float.MinValue;
            float minNoiseHeight = float.MaxValue;

            float halfWidth = mapWidth / 2f;
            float halfHeight = mapHeight / 2f;


            for (int y = 0; y<mapHeight; y++) {
                for (int x = 0; x<mapWidth; x++) {
		
                    float amplitude = 1;
                    float frequency = 1;
                    float noiseHeight = 0;

                    for (int i = 0; i<octaves; i++) {
                        float sampleX = (x - halfWidth) / scale * frequency + octaveOffsets[i].x;
                        float sampleY = (y - halfHeight) / scale * frequency + octaveOffsets[i].y;

                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                        noiseHeight += perlinValue* amplitude;

                        amplitude *= persistance;
                        frequency *= lacunarity;
                    }

                    if (noiseHeight > maxNoiseHeight) {
                        maxNoiseHeight = noiseHeight;
                    } else if (noiseHeight<minNoiseHeight) {
                        minNoiseHeight = noiseHeight;
                    }
                    noiseMap[x, y] = noiseHeight;
                }
            }

            for (int y = 0; y<mapHeight; y++) {
                for (int x = 0; x<mapWidth; x++) {
                    noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
                }
            }

            return noiseMap;
        }
    }
}
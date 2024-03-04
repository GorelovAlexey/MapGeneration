using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts
{
    public class SpriteGenerator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private MeshGenerator meshGenerator;
        [Space] [Header("NoiseSettings")]
        //[Range(1, 128)]
        [SerializeField] private int width = 64;
        //[Range(1, 128)]
        [SerializeField] private int height = 64;
        [SerializeField] private float noiseScale = 20;
        [SerializeField] private int seed = 191258581;
        [Range(1, 32)]
        [SerializeField] private int octaves = 4;
        [SerializeField] private float percistance = .5f;
        [SerializeField] private float lacunarity = 1.2f;

       // [Space] [Header("ErrosionSettings")]
        [SerializeField]
        private ErosionSettings erosionSettings;
        //[Space] [Header("ErrosionSettings2")]
        [SerializeField]
        private ErosionSettings2 erosionSettings2;

        [Space]
        [Header("ColorSettings")]
        [SerializeField] private float[] levelValues;
        [SerializeField] private Color[] levelColors;

        // Start is called before the first frame update
        void Start()
        {
            GenerateBasicMap();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                GenerateBasicMap();
                ApplyErrosion2();
            }
        }

        private float[,] lastNoiseMap;
        private Texture2D lastTexture;
        void SetNoiseMap(float[,] noiseMap)
        {
            lastNoiseMap = noiseMap;

            lastTexture = DrawMap(noiseMap);
            SetSprite(lastTexture);
        }
        float[,] GenerateBasicNoiseMap() => NoiseGenerator.GenerateNoise(width, height, noiseScale, octaves, percistance, lacunarity, seed);
        public void GenerateBasicMap()
        {
            var noiseMap = GenerateBasicNoiseMap();
            noiseMap.NormalizeHeightMap();
            SetNoiseMap(noiseMap);
            Random.InitState(seed);
        }

        public ErosionHeatMapsDebug LastErosionHeatMapsDebug { get; private set; }
        public void ApplyHeatMapTexture(float[,] texture)
        {
            if (texture == null)
            {
                Debug.LogWarning("No debug heatmap, try running erosion simulation");
                return;
            }

            var txt = CreateHeatMap(texture);
            SetSprite(txt);
            meshGenerator.SetTexture(txt);
        }
        public void ApplyErrosion()
        {
            if (lastNoiseMap == null)
            {
                lastNoiseMap = GenerateBasicNoiseMap();
                Random.InitState(seed);
            }

            LastErosionHeatMapsDebug = new ErosionHeatMapsDebug(lastNoiseMap.GetLength(0), lastNoiseMap.GetLength(1));

            var erosionSeed = Random.Range(int.MinValue, int.MaxValue);
            lastNoiseMap = ErosionSimulation.Erode(lastNoiseMap, erosionSeed, erosionSettings, LastErosionHeatMapsDebug);
            lastNoiseMap.NormalizeHeightMap();
            //var blurred = ApplyBlur(noiseMap, 1);
            SetNoiseMap(lastNoiseMap);
        }

        public void ApplyErrosion2()
        {
            if (lastNoiseMap == null)
            {
                lastNoiseMap = GenerateBasicNoiseMap();
                Random.InitState(seed);
            }

            LastErosionHeatMapsDebug = new ErosionHeatMapsDebug(lastNoiseMap.GetLength(0), lastNoiseMap.GetLength(1));

            var erosionSeed = Random.Range(int.MinValue, int.MaxValue);
            lastNoiseMap = ErosionSimulation.Erosion2(lastNoiseMap, erosionSeed, erosionSettings2, LastErosionHeatMapsDebug);
            lastNoiseMap = ErosionSimulation.ApplyBlur(lastNoiseMap, 1);
            lastNoiseMap.NormalizeHeightMap();
            SetNoiseMap(lastNoiseMap);
        }


        Texture2D CreateHeatMap(float[,] heatMap)
        {
            var min = heatMap[0, 0];
            var max = heatMap[0, 0];

            foreach (var v in heatMap)
            {
                if (v > max) max = v;
                if (v < min) min = v;
            }

            var colorBlue = (240 / 360f);
            var colorRed = 0f;

            var texture = new Texture2D(heatMap.GetLength(0), heatMap.GetLength(1), TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point
            };
            for (int x = 0; x < heatMap.GetLength(0); x++)
            {
                for (int y = 0; y < heatMap.GetLength(1); y++)
                {
                    var value = Mathf.InverseLerp(min, max, heatMap[x, y]);
                    var hue = Mathf.Lerp(colorBlue, colorRed, value);
                    var color =  Color.HSVToRGB(hue, 1, 1);
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply(true);
            return texture;
        }


        public void Generate3DMesh(MeshGenerator.MeshVersion version)
        {
            meshGenerator.Generate3DMesh(version, lastNoiseMap, lastTexture);
        }

        #region 2D Sprite
        void SetSprite(Texture2D texture)
        {
            spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 32);
        }
        Texture2D DrawMap(float[,] noiseMap)
        {
            var width = noiseMap.GetLength(0);
            var height = noiseMap.GetLength(1);

            var tex2d = new Texture2D(width, height, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point,
            };

            for (var x = 0; x < tex2d.width; x++)
            {
                for (var y = 0; y < tex2d.height; y++)
                {
                    var noiseVal = noiseMap[x, y];
                    var col = GetColorForHeight(noiseVal);

                    var halfVal = noiseVal / 2;
                    Color.RGBToHSV(col, out var H, out var S, out var V);
                    col = Color.HSVToRGB(H, S - halfVal / 2, V + halfVal);

                    tex2d.SetPixel(x, y, col);
                }
            }

            tex2d.Apply();
            return tex2d;
        }
        public Color GetColorForHeight(float h)
        {
            var col = levelColors[0];
            for (var i = 1; i < levelValues.Length; i++)
            {
                if (h < levelValues[i])
                    break;
                col = levelColors[i];
            }
            return col;
        }

        public (float, float) GetUVBorder(float[,] noiseMap, int x, int y, int x2, int y2)
        {
            if (x2 <= 0 || y2 <= 0)
                return (1f, 0);

            if (x2 >= noiseMap.GetLength(0) || y2 >= noiseMap.GetLength(1))
                return (1f, 0);

            var height = noiseMap[x, y];
            var height2 = noiseMap[x2, y2];
            var heightDistance = Mathf.Abs(height2 - height);

            var col = GetColorForHeight(height);
            var col2 = GetColorForHeight(height2);

            if (col == col2)
                if (height < height2)
                    return (0f, 1f);
                else
                    return (1f, 0f);

            var maxHeight = Mathf.Max(height, height2);
            var minHeight = Mathf.Min(height, height2);
            var levelValue = 0f;
            foreach (var val in levelValues)
            {
                if (val >= maxHeight)
                    break;

                levelValue = val;
            }

            var topBorderDistance = (maxHeight - levelValue) / heightDistance;
            var botBorderDistance = (levelValue - minHeight) / heightDistance;

            return height > height2 ? (topBorderDistance, botBorderDistance) : (botBorderDistance, topBorderDistance);
        }


        #endregion
    }
}

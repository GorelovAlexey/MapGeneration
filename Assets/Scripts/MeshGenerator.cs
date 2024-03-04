using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts
{
    public class MeshGenerator : MonoBehaviour
    {
        [SerializeField] private SpriteGenerator spriteGenerator;
        [Header("3D_Mesh [Y]")]
        [SerializeField] private float unitPerDist = .5f;
        [SerializeField] private float unitPerHeight = 10f;
        [SerializeField] private GameObject generated;
        [SerializeField] private Material material;
        [Header("Chunks")]
        public List<MeshFilter> meshFilters = new List<MeshFilter>();
        public List<MeshRenderer> meshRenderers = new List<MeshRenderer>();

        private readonly Dictionary<MeshVersion, (int size, int overlap)> ChunkSizeDictionary = new Dictionary<MeshVersion, (int, int)>
        {
            {MeshVersion.First, (64, 1)},
            {MeshVersion.FirstUpdated, (64, 0)},
            {MeshVersion.Second, (64, 1)}
        };

        public enum MeshVersion
        {
            First, FirstUpdated, Second
        }

        public void Generate3DMesh(MeshVersion version, float[,] noiseMap, Texture2D texture)
        {
            if (noiseMap == null)
                return;

            var chunkRects = Chunkify(noiseMap, version);
            SetMeshFilterAndRenderesSize(chunkRects.Count);

            for (var i = 0; i < chunkRects.Count; i++)
            {
                var chunk = chunkRects[i];
                var filter = meshFilters[i];
                var renderer = meshRenderers[i];
                filter.gameObject.name = "Chunk " + i;

                switch (version)
                {
                    case MeshVersion.First:
                        filter.sharedMesh = Generate3DMesh(filter.sharedMesh, noiseMap, chunk.x, chunk.y, chunk.width, chunk.height);
                        filter.transform.localPosition = new Vector3(chunk.x * unitPerDist, chunk.y * unitPerDist);
                        break;
                    case MeshVersion.FirstUpdated:
                        filter.sharedMesh = Generate3DMeshUpdated(filter.sharedMesh, noiseMap, chunk.x, chunk.y, chunk.width, chunk.height);
                        filter.transform.localPosition = Vector3.zero;
                        break;
                    case MeshVersion.Second:
                        filter.sharedMesh = Generate3DMesh2(filter.sharedMesh, noiseMap, chunk.x, chunk.y, chunk.width, chunk.height);
                        filter.transform.localPosition = Vector3.zero;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(version), version, null);
                }

                renderer.sharedMaterial = material;
                renderer.sharedMaterial.mainTexture = texture;
            }
        }


        public void SetTexture(Texture2D tex)
        {
            foreach (var mf in meshRenderers)
            {
                mf.sharedMaterial.mainTexture = tex;
            }
        }

        private T[,] GetArrayBlock<T>(T[,] arr, int startX, int startY, int size)
        {
            var sizeX = Mathf.Min(size, arr.GetLength(0) - startX);
            var sizeY = Mathf.Min(size, arr.GetLength(1) - startY);
            var subBlock = new T[sizeX, sizeY];

            for (var x = 0; x < sizeX; x++)
            {
                for (var y = 0; y < sizeY; y++)
                {
                    var xi = x + startX;
                    var yi = y + startY;

                    subBlock[x, y] = arr[xi, yi];
                }
            }

            return subBlock;
        }

        public List<(int x, int y, int width, int height)> Chunkify(float[,] map, MeshGenerator.MeshVersion version)
        {
            var splitChunks = new List<(int x, int y, int width, int height)>();
            var (chunkSize, overlapRadius) = ChunkSizeDictionary[version];
            var width = map.GetLength(0);
            var height = map.GetLength(1);

            var chunksX = width / chunkSize + (width % chunkSize > 0 ? 1 : 0);
            var chunksY = height / chunkSize + (height % chunkSize > 0 ? 1 : 0);

            for (var x = 0; x < chunksX; x++)
            {
                for (int y = 0; y < chunksY; y++)
                {
                    var offsetX = x * chunkSize;
                    var offsetY = y * chunkSize;

                    var borderX = Mathf.Min(offsetX + chunkSize + overlapRadius, width);
                    var borderY = Mathf.Min(offsetY + chunkSize + overlapRadius, height);

                    offsetX = Mathf.Max(0, offsetX - overlapRadius);
                    offsetY = Mathf.Max(0, offsetY - overlapRadius);

                    splitChunks.Add((offsetX, offsetY, borderX - offsetX, borderY - offsetY));
                }
            }

            return splitChunks;
        }

        void SetMeshFilterAndRenderesSize(int chunks)
        {
            var sizeMax = Mathf.Max(chunks, meshFilters.Count, meshRenderers.Count);

            while (meshFilters.Count < sizeMax)
                meshFilters.Add(null);

            while (meshRenderers.Count < sizeMax)
                meshRenderers.Add(null);

            for (int i = 0; i < sizeMax; i++)
            {
                var mf = meshFilters[i];
                var mr = meshRenderers[i];

                GameObject go = null;
                if (mf)
                    go = mf.gameObject;
                if (mr)
                    go = mr.gameObject;

                (mf, mr) = SetupChunk(go, i < chunks);
                meshFilters[i] = mf;
                meshRenderers[i] = mr;
            }

            (MeshFilter, MeshRenderer) SetupChunk(GameObject go, bool active)
            {
                if (!go)
                    go = new GameObject("Chunk");

                go.SetActive(active);
                go.transform.SetParent(generated.transform);
                go.transform.localScale = Vector3.one;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localPosition = Vector3.zero;

                var mf = go.GetComponent<MeshFilter>();
                var mr = go.GetComponent<MeshRenderer>();

                if (!mf)
                    mf = go.AddComponent<MeshFilter>();

                if (!mr)
                    mr = go.AddComponent<MeshRenderer>();

                return (mf, mr);
            }
        }

        (GameObject generated, MeshFilter filter, MeshRenderer renderer) GetOrCreate3DObject()
        {
            var hasGenerated = generated ? true : false;
            if (!hasGenerated)
            {
                generated = new GameObject("GeneratedMesh");
                generated.transform.position = new Vector3(-4, -4, 0);
                generated.transform.localScale = new Vector3(.25f, .25f, .25f);
                generated.transform.Rotate(-60, 180, 45);
            }
            var filter = hasGenerated ? generated.GetComponent<MeshFilter>() : generated.AddComponent<MeshFilter>();
            var renderer = hasGenerated ? generated.GetComponent<MeshRenderer>() : generated.AddComponent<MeshRenderer>();

            return (generated, filter, renderer);
        }

        Mesh Generate3DMesh(Mesh mesh, float[,] noiseMap, int offsetX, int offsetY, int sizeX, int sizeY)
        {
            var uvSizeX = 1f / noiseMap.GetLength(0);
            var uvSizeY = 1f / noiseMap.GetLength(1);

            var vert = new List<Vector3>();
            var traingles = new List<int>();
            var UV = new List<Vector2>();

            Vector3[] CalculateNoiseQuad(int x, int y, float height)
            {
                var offsetX = unitPerDist * x;
                var offsetY = unitPerDist * y;
                var offsetZ = unitPerHeight * height;

                var A = new Vector3(offsetX, offsetY, offsetZ);
                var B = new Vector3(offsetX + unitPerDist, offsetY, offsetZ);
                var C = new Vector3(offsetX + unitPerDist, offsetY + unitPerDist, offsetZ);
                var D = new Vector3(offsetX, offsetY + unitPerDist, offsetZ);

                return new Vector3[] { A, B, C, D };
            }

            int GetVertexIndex(int x, int y)
            {
                return (x - offsetX + (y - offsetY) * sizeX) * 4;
            }

            var maxY = offsetY + sizeY;
            var maxX = offsetX + sizeX;

            // fill top sides
            for (var y = offsetY; y < maxY; y++)
            {
                for (var x = offsetX; x < maxX; x++)
                {
                    var quadVertices = CalculateNoiseQuad(x, y, noiseMap[x, y]);
                    AddQuad(quadVertices, offsetX, offsetY, CalculateUVDistancesForPoint(x, y));
                }
            }

            // Creating bottom and right sides
            for (var x = offsetX; x < maxX; x++)
            {
                for (var y = offsetY; y < maxY; y++)
                {
                    var x1 = x + 1;
                    var y1 = y + 1;
                    //var height0 = noiseMap[x, y];
                    var quad0 = GetVertexIndex(x, y);

                    void debugDist(params int[] vertices)
                    {
                        var vertices3 = new Vector3[vertices.Length];
                        for (int i = 0; i < vertices.Length; i++)
                            vertices3[i] = vert[vertices[i]];

                        var distances = new float[vertices.Length - 1];
                        for (int i = 1; i < vertices.Length; i++)
                        {
                            distances[i - 1] = ((Vector2)vertices3[0] - (Vector2)vertices3[i]).magnitude;
                        }
                        if (distances.Max() > unitPerDist * 10)
                        {
                            //throw new Exception("");
                        }
                    }

                    // right
                    if (x1 < maxX)
                    {
                        //var height1 = noiseMap[x1, y];
                        var quad1 = GetVertexIndex(x1, y);
                        var a = quad0 + 1;
                        var b = quad1;
                        var c = quad1 + 3;
                        var d = quad0 + 2;

                        // 1
                        traingles.Add(a);
                        traingles.Add(b);
                        traingles.Add(c);
                        // 2
                        traingles.Add(a);
                        traingles.Add(c);
                        traingles.Add(d);

                        //debugDist(a, b, c, d);
                        // UV
                        //var quadForUV = height1 > height0 ? CalculateNoiseQuad(x1, y) : CalculateNoiseQuad(x, y);
                        //AddUV(quadForUV[0], quadForUV[1], quadForUV[2], quadForUV[3]);
                    }

                    // bot
                    if (y1 < maxY)
                    {
                        //var height2 = noiseMap[x, y1];
                        var quad2 = GetVertexIndex(x, y1);
                        var a = quad0 + 3;
                        var b = quad0 + 2;
                        var c = quad2 + 1;
                        var d = quad2;
                        // 1
                        traingles.Add(a);
                        traingles.Add(b);
                        traingles.Add(c);
                        // 2
                        traingles.Add(a);
                        traingles.Add(c);
                        traingles.Add(d);

                        // debugDist(a, b, c, d);
                        // UV
                        //var quadForUV = height2 > height0 ? CalculateNoiseQuad(x, y1) : CalculateNoiseQuad(x, y);
                        //AddUV(quadForUV[0], quadForUV[1], quadForUV[2], quadForUV[3]);
                    }
                }
            }

            float[] CalculateUVDistancesForPoint(int x, int y)
            {
                var top = spriteGenerator.GetUVBorder(noiseMap, x, y, x, y - 1);
                var right = spriteGenerator.GetUVBorder(noiseMap, x, y, x + 1, y);
                var bot = spriteGenerator.GetUVBorder(noiseMap, x, y, x, y + 1);
                var left = spriteGenerator.GetUVBorder(noiseMap, x, y, x - 1, y);

                return new[] { top.Item1, right.Item1, bot.Item1, left.Item1 };
            }

            void AddQuad(Vector3[] ABCDquad, int offsetX, int offsetY, float[] uvDistances)
            {
                var i = vert.Count;
                var a = ABCDquad[0];
                var b = ABCDquad[1];
                var c = ABCDquad[2];
                var d = ABCDquad[3];
                var offsetXY = new Vector3(offsetX * unitPerDist, offsetY * unitPerDist, 0);


                a -= offsetXY;
                b -= offsetXY;
                c -= offsetXY;
                d -= offsetXY;


                vert.Add(a);
                vert.Add(b);
                vert.Add(c);
                vert.Add(d);

                // Первый треугольник
                for (var j = 0; j < 3; j++)
                    traingles.Add(i + j);

                // Второй
                traingles.Add(i);
                traingles.Add(i + 2);
                traingles.Add(i + 3);

                a += offsetXY;
                b += offsetXY;
                c += offsetXY;
                d += offsetXY;

                AddUV(a, b, c, d, uvDistances);
            }

            void AddUV(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float[] uvDistances)
            {
                void AddVector(Vector2 v, float x = 0f, float y = 0f)
                {
                    UV.Add(new Vector2((v.x + x) * uvSizeX / unitPerDist, (v.y + y) * uvSizeY / unitPerDist));
                }

                var xAdd = 0.15f;
                var yAdd = 0.15f;

                var top = uvDistances[0] * yAdd;
                var right = -uvDistances[1] * xAdd;
                var bot = -uvDistances[2] * yAdd;
                var left = uvDistances[3] * xAdd;

                AddVector(a, left, top);
                AddVector(b, right, top);
                AddVector(c, right, bot);
                AddVector(d, left, bot);
            }

            if (mesh == null)
            {
                mesh = new Mesh
                {
                    name = "Generated terrain"
                };
            }

            mesh.Clear(false);
            mesh.vertices = vert.ToArray();
            mesh.triangles = traingles.ToArray();
            mesh.uv = UV.ToArray();

            return mesh;
        }
        Mesh Generate3DMeshUpdated(Mesh mesh, float[,] noiseMap, int offsetX, int offsetY, int sizeX, int sizeY)
        {
            var maxX = offsetX + sizeX;
            var maxY = offsetY + sizeY;

            var uvSizeX = 1f / noiseMap.GetLength(0);
            var uvSizeY = 1f / noiseMap.GetLength(1);

            var VERT = new List<Vector3>();
            var TRI = new List<int>();
            var UV = new List<Vector2>();

            void AddTopSide(int x, int y, Vector3 vertexPoisitionOffset)
            {
                var offsetX = unitPerDist * x;
                var offsetY = unitPerDist * y;
                var offsetZ = unitPerHeight * noiseMap[x, y];

                var A = new Vector3(offsetX, offsetY, offsetZ);
                var B = new Vector3(offsetX + unitPerDist, offsetY, offsetZ);
                var C = new Vector3(offsetX + unitPerDist, offsetY + unitPerDist, offsetZ);
                var D = new Vector3(offsetX, offsetY + unitPerDist, offsetZ);

                var i = VERT.Count;

                void AddUv(Vector3 vertPosition)
                {
                    UV.Add(new Vector2(vertPosition.x * uvSizeX / unitPerDist, vertPosition.y * uvSizeY / unitPerDist));
                }

                AddUv(A);
                AddUv(B);
                AddUv(C);
                AddUv(D);

                VERT.Add(A);
                VERT.Add(B);
                VERT.Add(C);
                VERT.Add(D);

                // Первый треугольник
                for (var j = 0; j < 3; j++)
                    TRI.Add(i + j);

                // Второй
                TRI.Add(i);
                TRI.Add(i + 2);
                TRI.Add(i + 3);

            }
            var offsetPos = new Vector3(offsetX, offsetY);
            for (var y = offsetY; y < maxY; y++)
                for (var x = offsetX; x < maxX; x++)
                    AddTopSide(x, y, offsetPos);

            int GetVertexIndex(int x, int y)
            {
                return (x - offsetX + (y - offsetY) * sizeX) * 4;
            }

            (Vector3 A, Vector3 B, Vector3 C, Vector3 D) GetVertices(int x, int y)
            {
                var i = GetVertexIndex(x, y);
                return (VERT[i], VERT[i + 1], VERT[i + 2], VERT[i + 3]);
            }

            for (var y = offsetY - 1; y < maxY; y++)
            {
                for (var x = offsetX - 1; x < maxX; x++)
                {
                    AddBotConnection(x, y);
                    AddRightConnection(x, y);
                }
            }

            void AddBotConnection(int x, int y)
            {
                if (x < offsetX)
                    return;

                var y2 = y + 1;
                Vector3 A, B, C, D;
                Vector3 uv0, uv1, uv2, uv3;

                uv0 = uv1 = uv2 = uv3 = Vector3.zero;
                /* Vertices in quad placed like this
                 * A B
                 * D C
                 */
                if (y < offsetY)
                {
                    (B, A, uv2, uv3) = GetVertices(x, y2);
                    A.z = 0;
                    B.z = 0;
                    (C, D, _, _) = GetVertices(x, y2);

                    uv0 = B;
                    uv1 = A;
                    if (A.z == D.z)
                        return;
                }
                else if (y == maxY - 1)
                {
                    (uv0, uv1, A, B) = GetVertices(x, y);
                    (_, _, D, C) = GetVertices(x, y);
                    uv2 = A;
                    uv3 = B;
                    C.z = 0;
                    D.z = 0;
                    // uv2 = B;
                    //uv3 = A;
                    if (A.z == D.z)
                        return;
                }
                else
                {
                    (uv0, uv1, A, B) = GetVertices(x, y);
                    (C, D, uv3, uv2) = GetVertices(x, y2);

                    var uvDistance = spriteGenerator.GetUVBorder(noiseMap, x, y, x, y2);

                    var dist = (uv0 - B) * uvDistance.Item1;
                    uv0 = B + dist;
                    uv1 = A + dist;

                    var dist2 = (uv2 - C) * uvDistance.Item2;
                    uv2 = C + dist2;
                    uv3 = D + dist2;

                    if (A.z == D.z)
                        return;
                }
                AddQuad(A, B, C, D);
                AddUV(uv0, uv1, uv2, uv3);
            }

            void AddRightConnection(int x, int y)
            {
                if (y < offsetY)
                    return;

                var x2 = x + 1;
                Vector3 A, B, C, D;
                Vector3 uv0, uv1, uv2, uv3;
                uv0 = uv1 = uv2 = uv3 = Vector3.zero;
                /* Vertices in quad placed like this
                 * A B
                 * D C
                 */
                if (x <= offsetX)
                {
                    (A, uv0, uv3, B) = GetVertices(x2, y);
                    (D, _, _, C) = GetVertices(x2, y);
                    A.z = 0;
                    B.z = 0;
                    uv1 = A;
                    uv2 = B;
                    if (A.z == D.z)
                        return;
                }
                else if (x == maxX - 1)
                {
                    (uv0, A, B, uv3) = GetVertices(x, y);
                    (_, D, C, _) = GetVertices(x, y);
                    C.z = 0;
                    D.z = 0;
                    uv1 = A;
                    uv2 = B;
                    if (A.z == D.z)
                        return;
                }
                else
                {
                    (uv0, A, B, uv1) = GetVertices(x, y);
                    (D, uv3, uv2, C) = GetVertices(x2, y);

                    var uvDistance = spriteGenerator.GetUVBorder(noiseMap, x, y, x2, y);
                    var dist = (uv0 - A) * uvDistance.Item1;
                    uv0 = A + dist;
                    uv1 = B + dist;

                    var dist2 = (uv2 - C) * uvDistance.Item2;
                    uv2 = C + dist2;
                    uv3 = D + dist2;

                    if (A.z == D.z)
                        return;
                }
                AddQuad(A, B, C, D);
                AddUV(uv0, uv1, uv2, uv3);
            }

            void AddUV(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
            {
                void AddUv(Vector2 vertPosition)
                {
                    UV.Add(new Vector2(vertPosition.x * uvSizeX / unitPerDist, vertPosition.y * uvSizeY / unitPerDist));
                }
                AddUv(a);
                AddUv(b);
                AddUv(c);
                AddUv(d);
            }

            void AddQuad(Vector3 A, Vector3 B, Vector3 C, Vector3 D)
            {
                var i = VERT.Count;
                VERT.Add(A);
                VERT.Add(B);
                VERT.Add(C);
                VERT.Add(D);

                // Первый треугольник
                //traingles.Add(i);
                // traingles.Add(i + 1);
                //traingles.Add(i + 2);

                TRI.Add(i + 2);
                TRI.Add(i + 1);
                TRI.Add(i);

                // Второй
                //traingles.Add(i);
                //traingles.Add(i + 2);
                //traingles.Add(i + 3);

                TRI.Add(i + 3);
                TRI.Add(i + 2);
                TRI.Add(i);
            }

            if (mesh == null)
            {
                mesh = new Mesh
                {
                    name = "Generated terrain"
                };
            }
            else
                mesh.Clear(false);

            mesh.vertices = VERT.ToArray();
            mesh.triangles = TRI.ToArray();
            mesh.uv = UV.ToArray();
            mesh.RecalculateNormals();

            return mesh;
        }
        Mesh Generate3DMesh2(Mesh mesh, float[,] noiseMap, int offsetX, int offsetY, int sizeX, int sizeY)
        {
            var maxX = offsetX + sizeX;
            var maxY = offsetY + sizeY;

            var uvSizeX = 1f / noiseMap.GetLength(0);
            var uvSizeY = 1f / noiseMap.GetLength(1);

            var Vert = new List<Vector3>();
            var traingles = new List<int>();
            var UV = new List<Vector2>();

            int GetPointIndex(int x, int y) => (x - offsetX) + (y - offsetY) * sizeX;

            for (var y = offsetY; y < maxY; y++)
                for (var x = offsetX; x < maxX; x++)
                {
                    var pos = new Vector3(x * unitPerDist, y * unitPerDist, noiseMap[x, y] * unitPerHeight);
                    Vert.Add(pos);
                    UV.Add(new Vector2((pos.x + unitPerDist / 2) * uvSizeX / unitPerDist, (pos.y + unitPerDist / 2) * uvSizeY / unitPerDist));
                }

            for (var y = offsetY; y < maxY - 1; y++)
                for (var x = offsetX; x < maxX - 1; x++)
                {
                    AddQuadRightBot(x, y);
                }

            void AddQuadRightBot(int x, int y)
            {
                var A = GetPointIndex(x, y);
                var B = GetPointIndex(x + 1, y);
                var C = GetPointIndex(x + 1, y + 1);
                var D = GetPointIndex(x, y + 1);
                traingles.Add(A);
                traingles.Add(B);
                traingles.Add(C);
                traingles.Add(A);
                traingles.Add(C);
                traingles.Add(D);
            }

            if (mesh == null)
            {
                mesh = new Mesh
                {
                    name = "Generated terrain"
                };
            }
            else 
                mesh.Clear(false);

            mesh.vertices = Vert.ToArray();
            mesh.triangles = traingles.ToArray();
            mesh.uv = UV.ToArray();
            mesh.RecalculateNormals();

            return mesh;
        }
    }
}
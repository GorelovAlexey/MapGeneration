using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts
{
    public static class ErosionSimulation
    {
  
        public static float[,] CreateBlurKernel(int radius)
        {
            var sigma = MathF.Max((float) radius / 2, 1f);
            var kernelWidth = 2 * radius + 1;
            var kernel = new float[kernelWidth, kernelWidth];
            var sum = 0f;
            for (var x = -radius; x <= radius; x++)
            {
                for (var y = -radius; y <= radius; y++)
                {
                    var exponentNumerator = (float) (-(x * x + y * y));
                    var exponentDenominator = (2 * sigma * sigma);

                    var eExpression = MathF.Pow(MathF.E, exponentNumerator / exponentDenominator);
                    var kernelValue = (eExpression / (2 * MathF.PI * sigma * sigma));

                    kernel[x + radius, y + radius] = kernelValue;
                    sum += kernelValue;
                }
            }

            for (var x = 0; x < kernelWidth; x++)
            for (var y = 0; y < kernelWidth; y++)
                kernel[x, y] /= sum;

            return kernel;
        }

        public static float[,] CreateBlurKernel2(int radius)
        {
            var kernelWidth = 2 * radius + 1;
            var kernel = new float[kernelWidth, kernelWidth];
            var sum = 0f;
            var maxDist = radius * radius * 2;
            for (var x = -radius; x <= radius; x++)
            {
                for (var y = -radius; y <= radius; y++)
                {
                    var dist = x * x + y * y;
                    var kernelValue = Mathf.InverseLerp(maxDist, 0, dist);
                    kernel[x + radius, y + radius] = kernelValue;
                    sum += kernelValue;
                }
            }

            for (var x = 0; x < kernelWidth; x++)
            for (var y = 0; y < kernelWidth; y++)
                kernel[x, y] /= sum;

            return kernel;
        }

        public static void AddBlurredValue(float[,] map, float[,] kernel, float volume, int x, int y)
        {
            var width = map.GetLength(0);
            var height = map.GetLength(1);
            var radius = kernel.GetLength(0) / 2;

            for (int kernelX = -radius; kernelX <= radius; kernelX++)
            {
                for (int kernelY = -radius; kernelY <= radius; kernelY++)
                {
                    var kernelValue = kernel[kernelX + radius, kernelY + radius];

                    var x1 = x + kernelX;
                    var y1 = y + kernelY;
                    // TBH IDK how it supposed to work for edges 
                    if (x1 < 0 || x1 >= width) continue;
                    if (y1 < 0 || y1 >= height) continue;
                    map[x1, y1] += volume * kernelValue;
                }
            }
        }

        public static float[,] ApplyBlur(float[,] heightMap, int radius)
        {
            var width = heightMap.GetLength(0);
            var height = heightMap.GetLength(1);

            var heightMapResult = new float[width, height];

            var sigma = MathF.Max((float) radius / 2, 1f);
            var kernelWidth = 2 * radius + 1;

            // Initializing the 2D array for the kernel
            var kernel = new float[kernelWidth, kernelWidth];
            var sum = 0f;

            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    var exponentNumerator = (float) (-(x * x + y * y));
                    var exponentDenominator = (2 * sigma * sigma);

                    var eExpression = MathF.Pow(MathF.E, exponentNumerator / exponentDenominator);
                    var kernelValue = (eExpression / (2 * MathF.PI * sigma * sigma));

                    // We add radius to the indices to prevent out of bound issues because x and y can be negative
                    kernel[x + radius, y + radius] = kernelValue;
                    sum += kernelValue;
                }
            }

            for (int x = 0; x < kernelWidth; x++)
                for (int y = 0; y < kernelWidth; y++) 
                    kernel[x, y] /= sum;


            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    heightMapResult[x, y] = CalculateHeightValue(x, y);
                }
            }

            float CalculateHeightValue(int x, int y)
            {
                var mapHeight = 0f;
                for (int kernelX = -radius; kernelX <= radius; kernelX++)
                {
                    for (int kernelY = -radius; kernelY <= radius; kernelY++)
                    {
                        var kernelValue = kernel[kernelX + radius, kernelY + radius];
                        var x1 = x + kernelX;
                        var y1 = y + kernelY;
                        // TBH IDK how it supposed to work for edges 
                        if (x1 < 0 || x1 >= width)
                        {
                            x1 = x;
                        }

                        if (y1 < 0 || y1 >= height)
                        {
                            y1 = y;
                        }
                        var heightMapValue = heightMap[x1, y1];
                        mapHeight += heightMapValue * kernelValue;
                    }
                }

                return mapHeight;
            }

            return heightMapResult;
        }

        public static float[,] Erode(float[,] elevationsMap, int seed = 0, ErosionSettings settings = null,
            ErosionHeatMapsDebug heatMapsDebug = null)
        {
            var width = elevationsMap.GetLength(0);
            var height = elevationsMap.GetLength(1);
            var copy = (float[,]) elevationsMap.Clone();

            var pathList = new List<(int, int)>();

            Random.InitState(seed);
            //var rnd = new System.Random(seed);
            for (int i = 0; i < settings.dropletCycles; i++)
            {
                var x = Random.Range(0, width); //   rnd.Next(width);
                var y = Random.Range(0, height); //   rnd.Next(height);
                SimulateDroplet(copy, x, y, pathList, settings, heatMapsDebug);
                //Debug.Log("Droplet done " + i);
            }

            copy = ApplyBlur(copy, 1);
            copy.NormalizeHeightMap();
            return copy;
        }

        static (int, int) FindNextPosition(float[,] elevationsMap, int x, int y)
        {
            var x2 = x;
            var y2 = y;
            var currentElevation = elevationsMap[x2, y2];

            var width = elevationsMap.GetLength(0);
            var height = elevationsMap.GetLength(1);

            void FindDownwardsPoint(int x, int y)
            {
                if (x < 0 || x >= width || y < 0 || y >= height)
                    return;

                var elevation = elevationsMap[x, y];
                if (elevation < currentElevation)
                {
                    x2 = x;
                    y2 = y;
                }
            }

            FindDownwardsPoint(x + 1, y);
            FindDownwardsPoint(x, y + 1);
            FindDownwardsPoint(x - 1, y);
            FindDownwardsPoint(x, y - 1);

            /*
            FindDownwardsPoint(x + 1, y + 1);
            FindDownwardsPoint(x + 1, y - 1);
            FindDownwardsPoint(x - 1, y + 1);
            FindDownwardsPoint(x - 1, y - 1);
            */

            return (x2, y2);
        }
        static int FillPathList(float[,] elevationsMap, List<(int, int)> pathList, int x, int y)
        {
            var pathLast = 0;
            while (true)
            {
                if (pathLast < pathList.Count)
                    pathList[pathLast] = (x, y);
                else
                    pathList.Add((x, y));
                pathLast++;

                var (x1, y1) = FindNextPosition(elevationsMap, x, y);
                if (x1 == x && y1 == y)
                    break;

                x = x1;
                y = y1;
            }

            return pathLast;
        }

        static void SimulateDroplet(float[,] elevationsMap, int x, int y, List<(int, int)> pathList,
            ErosionSettings settings, ErosionHeatMapsDebug heatMapsDebug)
        {
            float ZERO_TIME_INTERVAL = settings.dropletMass / (settings.evaporationSpeed * 10f);

            var pathCount = FillPathList(elevationsMap, pathList, x, y);
            var pathCurrent = 0;

            var size = settings.dropletMass;
            var saturation = 0f; // насыщение капли воды породой
            var speed = 100;
            var cycles = 0;

            // var kernel = CreateBlurKernel(1);

            var exactPosition = new Vector2(x + .5f, y + .5f);

            if (heatMapsDebug != null)
            {
                heatMapsDebug.heatmapStarts[x, y] += 1;
                heatMapsDebug.heatmapVisits[x, y] += 1;
            }

            // Основной цикл симуляции
            while (size > 0)
            {
                var (xNext, yNext) = (pathCurrent < pathCount)
                    ? pathList[pathCurrent]
                    : FindNextPosition(elevationsMap, x, y);
                pathCurrent++;

                var pathVector = GetPathVector(xNext, yNext);
                var time = CalculateTime(xNext, yNext, pathVector, out var isZeroMovement);

                if (isZeroMovement)
                {
                    xNext = x;
                    yNext = y;
                }

                if (xNext == x && yNext == y)
                    speed = 0;

                var sat1 = elevationsMap[x, y];
                var sat2 = elevationsMap[xNext, yNext];

                var isOnSameSpace = xNext == x && yNext == y;

                if (!isOnSameSpace && false)
                {
                    EvaporationAndSaturation(time * .5f, x, y);
                    EvaporationAndSaturation(time * .5f, xNext, yNext, elevationsMap[x, y]);
                }
                else
                    EvaporationAndSaturation(time, x, y);

                if (heatMapsDebug != null)
                {
                    if (xNext != x || yNext != y)
                        heatMapsDebug.heatmapVisits[xNext, yNext] += 1;

                    heatMapsDebug.heatmapTime[x, y] += time;
                    //heatMapsDebug.heatmapTime[xNext, yNext] += time * .5f;
                    heatMapsDebug.heatmapSaturationChanges[x, y] += elevationsMap[x, y] - sat1;
                    //  heatMapsDebug.heatmapSaturationChanges[xNext, yNext] += elevationsMap[xNext, yNext] - sat2;
                }

                x = xNext;
                y = yNext;
                exactPosition += pathVector;

                cycles += 1;
                if (cycles > 100_000)
                    Debug.LogWarning("ERROR?");
            }


            bool IsCloseToZero(float value)
            {
                return value < Mathf.Epsilon && value > -Mathf.Epsilon;
            }

            void EvaporationAndSaturation(float t, int x, int y, float? maxHeight = null)
            {
                var satProportion = size > 0 ? saturation / size : 0;
                var satPercent = satProportion / settings.saturationMaxProportion;
                var satSpeed = 0.1f * settings.saturationSpeed + .9f * satPercent * settings.saturationSpeed;

                size -= t * settings.evaporationSpeed;
                if (size <= 0)
                {
                    size = 0;
                    elevationsMap[x, y] += saturation;
                    return;
                }

                var availableMatter = elevationsMap[x, y];

                float saturationTarget = saturation + t * satSpeed;
                var maxSaturation = size * settings.saturationMaxProportion;
                if (saturationTarget > maxSaturation)
                    saturationTarget = maxSaturation;
                if (saturationTarget < 0)
                    saturationTarget = 0;

                var saturationTargetChange = saturationTarget - saturation;
                if (saturationTargetChange > 0 && saturationTargetChange > availableMatter)
                    saturationTargetChange = availableMatter;

                if (saturationTargetChange < 0 && maxHeight.HasValue)
                {
                    var maxPossibleMatterDeposit = elevationsMap[x, y] - maxHeight.Value;
                    if (saturationTargetChange < maxPossibleMatterDeposit)
                        saturationTargetChange = maxPossibleMatterDeposit;
                }

                // var kernelBlur = CreateBlurKernel2(1);
                //AddBlurredValue(elevationsMap, kernelBlur, -saturationTargetChange, x, y);
                elevationsMap[x, y] -= saturationTargetChange;

                saturation += saturationTargetChange;

                // if (maxHeight.HasValue && elevationsMap[x, y] > maxHeight.Value) elevationsMap[x, y] = maxHeight.Value;

                if (float.IsNaN(elevationsMap[x, y]))
                {
                    throw new Exception("NaN");
                }
            }

            Vector2 GetPathVector2(int xNext, int yNext) => new Vector2(xNext - x, yNext - y);

            Vector2 GetPathVector(int xNext, int yNext)
            {
                if (x == xNext && yNext == y)
                    return Vector2.zero;

                const float minBorderDist = 0.001f;

                var changeX = xNext - x;
                var changeY = yNext - y;

                Vector2 posEnd = new Vector2(xNext, yNext);
                if (changeX > 0)
                    posEnd.x += minBorderDist;
                else if (changeX < 0)
                    posEnd.x += 1 - minBorderDist;

                if (changeY > 0)
                    posEnd.y += minBorderDist;
                else if (changeY < 0)
                    posEnd.y += 1 - minBorderDist;

                return posEnd - exactPosition;
            }

            float CalculateTime(int xNext, int yNext, Vector2 pathVector, out bool isZeroMovement)
            {
                const float GRAVITY = 9.8f;
                isZeroMovement = true;
                if (xNext == x && yNext == y)
                    return ZERO_TIME_INTERVAL;

                var pathDistance = pathVector.magnitude * settings.BLOCK_DISTANCE;

                var currHeight = elevationsMap[x, y];
                var nextHeight = elevationsMap[xNext, yNext];

                var difference = currHeight - nextHeight;
                var angleRadian = Mathf.Atan(difference * settings.HEIGHT_TO_METERS / settings.BLOCK_DISTANCE);
                var acceleration = GRAVITY * (Mathf.Sin(angleRadian) - settings.FRICTION * Mathf.Cos(angleRadian));

                var isZeroAcceleration = IsCloseToZero(acceleration);
                var isZeroSpeed = IsCloseToZero(speed);

                if (isZeroAcceleration && isZeroSpeed)
                    return ZERO_TIME_INTERVAL;

                isZeroMovement = false;
                if (isZeroAcceleration)
                    return pathDistance / speed;

                // Мы хотим найти время которое нужно капле чтобы преодалеть дистанцию до следующей точки
                // Нет ускорения: S = v * t
                // t = S / v
                // C ускорением: S == v (t) + a/2 * (t*t)
                // приводим к квадратичному уравнению:
                // a/2 (t*t) + v (t) - S = 0
                // t = ( -v +- sqrt(v*v + 2*a*S )) / a
                var discriminant = speed * speed + 2 * acceleration * pathDistance;
                var time = 0f;

                if (discriminant >= 0 && !isZeroAcceleration)
                {
                    var t1 = (-speed + Mathf.Sqrt(discriminant)) / acceleration;
                    var t2 = (-speed - Mathf.Sqrt(discriminant)) / acceleration;

                    if (t1 <= 0 && t2 <= 0)
                        time = ZERO_TIME_INTERVAL;
                    else if (t1 < 0 || t2 < 0)
                        time = Mathf.Max(t1, t2);
                    else
                        time = Mathf.Min(t1, t2);
                }

                return time;
            }
        }

        public static float[,] Erosion2(float[,] elevationsMap, int seed, ErosionSettings2 settings, ErosionHeatMapsDebug debug)
        {
            var width = elevationsMap.GetLength(0);
            var height = elevationsMap.GetLength(1);
            //var initial = (float[,])elevationsMap.Clone(); // debug
            var changesMap = new float[width, height];
            var erosionMask = CreateErosionMask(settings.particleErosionRadius);
            const int blurInterval = 100; // Бутылочное горлышко алгоритма - функция блюра. Для ускорения работы уменьшение  

            for (int p = 0; p < settings.particleCount; p++)
            {
                var posX = Random.Range(0f, width - 1);
                var posY = Random.Range(0f, height - 1);
                SimulateParticle(new Vector2(posX, posY));

                if (p == settings.particleCount - 1 || p % blurInterval == blurInterval - 1)
                {
                    ApplyChangesToElevationsMap(elevationsMap, changesMap, settings.erosionBlurRadius, settings.erosionBlurWeight);
                    Array.Clear(changesMap, 0, changesMap.Length);
                }
            }

            void SimulateParticle(Vector2 position)
            {
                Vector2 curDirection = Vector2.zero;
                float velocity = 1;
                float water = 10;
                float sediment = 0;
                debug.heatmapStarts[(int) position.x, (int) position.y] += 1;
                debug.heatmapVisits[(int)position.x, (int)position.y] += 1;

                var steps = settings.maxParticleLife;
                while (steps-- > 0 && water > 0)
                {
                    var grad = CalculateGradient(position);
                    var dir = CalculateDirection(grad, curDirection, settings.inertia);

                    var posNew = position + dir;
                    if (IsOutside(posNew))
                    {
                        break;
                        // STOP
                    }

                    var heightOld = CalculateHeight(position);
                    var heightNew = CalculateHeight(posNew);
                    var heightDff = heightNew - heightOld ;

                    var isGoingUphill = heightDff > 0;
                    if (isGoingUphill)
                    {
                        var depositValue = Mathf.Min(sediment, heightDff);
                        Deposition(depositValue, new Vector2((int)position.x, (int)position.y));
                        sediment -= depositValue;
                    }
                    else
                    {
                        var allowedCapacity = Mathf.Max(-heightDff, settings.minSlopeParam) * velocity * water * settings.particleCapacity;
                        if (sediment > allowedCapacity)
                        {
                            var surplus = sediment - allowedCapacity;
                            var deposit = surplus * settings.particleDepositionSpeed;
                            Deposition(deposit, position);
                            sediment -= deposit;
                        }
                        else
                        {
                            var canGet = allowedCapacity - sediment;
                            var erosion = Mathf.Min(canGet, -heightDff) * settings.erosionSpeed;
                            Erosion(erosion, (int)position.x, (int)position.y, settings.particleErosionRadius, erosionMask);
                            sediment += erosion;
                        }
                    }

                    velocity = Mathf.Sqrt(velocity * velocity + heightDff * settings.gravity);
                    water = water * (1 - settings.particleEvaporation);

                    position = posNew;

                    debug.heatmapVisits[(int)position.x, (int)position.y] += 1;
                }
            }

            bool IsOutside(Vector2 pos)
            {
                var x = (int)pos.x;
                var y = (int)pos.y;
                return (x < 0 || y < 0 || x >= width || y >= height);
            }

            Vector2 CalculateGradient(Vector2 pos)
            {
                var x = (int) pos.x;
                var y = (int) pos.y;
                var u = pos.x - x;
                var v = pos.y - y;

                var x1 = x + 1;
                var y1 = y + 1;

                if (x1 == width) 
                    x1 = x;
                if (y1 == height) 
                    y1 = y;

                var Pxy = elevationsMap[x, y];
                var Px1y = elevationsMap[x1, y];
                var Px1y1 =  elevationsMap[x1, y1];
                var Pxy1 = elevationsMap[x, y1];

                // var Gxy = new Vector2(Px1y - Pxy, Pxy1 - Pxy);
                // var Gx1y = new Vector2(Px1y - Pxy, Px1y1 - Px1y);
                // var Gxy1 = new Vector2(Px1y1 - Pxy1, Pxy1 - Pxy);
                // var Gx1y1 = new Vector2(Px1y1 - Pxy1, Px1y1 - Px1y);

                var grad = new Vector2(
                    (Px1y - Pxy) * (1 - v) + (Px1y1 - Pxy1) * v,
                    (Pxy1 - Pxy) * (1 - u) + (Px1y1 - Px1y) * u);

                return grad;
            }

            Vector2 CalculateDirection(Vector2 gradient, Vector2 dir, float inertia)
            {
                dir = (dir * inertia - gradient * (1 - inertia));
                if (dir == Vector2.zero)
                    dir = Random.insideUnitCircle;

                return dir.normalized;
            }

            float CalculateHeight(Vector2 pos)
            {
                var x = (int) pos.x;
                var y = (int) pos.y;

                return elevationsMap[x,y] + changesMap[x, y];
            }

            void Deposition(float value, Vector2 pos)
            {
                var x = (int)pos.x;
                var y = (int)pos.y;

                var u = pos.x - x;
                var v = pos.y - y;
                var uRev = 1 - u;
                var vRev = 1 - v;

                var x1 = x + 1;
                var y1 = y + 1;
                var isOutsideX = x1 >= width || u < Mathf.Epsilon;
                var isOutsideY = y1 >= height || v < Mathf.Epsilon;

                if (isOutsideX && isOutsideY)
                {
                    changesMap[x, y] += value;
                    debug.heatmapSaturationChanges[x, y] += value;
                    return;
                }

                if (isOutsideX)
                {
                    var val = value * vRev;
                    var val1 = value * v;

                    changesMap[x, y] += val;
                    changesMap[x, y1] += val1;

                    debug.heatmapSaturationChanges[x, y] += val;
                    debug.heatmapSaturationChanges[x, y1] += val1;
                    return;
                }
                
                if (isOutsideY)
                {
                    var val = value * u;
                    var val1 = value * uRev;

                    changesMap[x, y] += val;
                    changesMap[x1, y] += val1;

                    debug.heatmapSaturationChanges[x, y] += val;
                    debug.heatmapSaturationChanges[x1, y] += val1;
                    return;
                }

                // w01 . w11
                // . . . .
                // . . . .
                // w00 . w10
                var w00 = uRev * vRev * value;
                var w11 = u * v * value;
                var w10 = u * vRev * value;
                var w01 = uRev * v * value;

                changesMap[x, y] += w00;
                changesMap[x1, y] += w10;
                changesMap[x1, y1] += w11;
                changesMap[x, y1] += w01;

                debug.heatmapSaturationChanges[x, y] += w00;
                debug.heatmapSaturationChanges[x1, y] += w10;
                debug.heatmapSaturationChanges[x1, y1] += w11;
                debug.heatmapSaturationChanges[x, y1] += w01;
            }

            float[,] CreateErosionMask(int radius)
            {
                var total = 0f;
                var size = radius * 2 + 1;
                var result = new float[size, size];

                float GetW(int x, int y)
                {
                    var distX = radius - (x < 0 ? -x : x);
                    var distY = radius - (y < 0 ? -y : y);

                    return distX * distX + distY * distY;
                }

                for (var x = -radius; x <= radius; x++)
                {
                    for (var y = -radius; y <= radius; y++)
                    {
                        if (x < 0 || y < 0 || x >= width || y >= width)
                            continue;

                        var w = GetW(x, y);
                        total += w;
                        result[x + radius, y + radius] = w;
                    }
                }

                for (var i = 0; i < size; i++)
                {
                    for (var j = 0; j < size; j++)
                    {
                        result[i, j] /= total;
                    }
                }
                return result;
            }

            void Erosion(float value, int xOrigin, int yOrigin, int radius, float[,] erosionMask)
            {
                value = -value;
                
                for (int ix = -radius; ix <= radius; ix++)
                {
                    for (int iy = -radius; iy <= radius; iy++)
                    {
                        var x = xOrigin + ix;
                        var y = yOrigin + iy;
                        if (x < 0 || y < 0 || x >= width || y >= height)
                            continue;

                        var change = value * erosionMask[ix + radius, iy + radius];

                        changesMap[x, y] += change;
                        debug.heatmapSaturationChanges[x, y] += change;
                    }
                }
            }

            void ApplyChangesToElevationsMap(float[,] targetMap, float[,] changesMap, int blurRadius, float blurredWeight)
            {
                if (blurredWeight <= 0)
                {
                    elevationsMap.SumMaps(changesMap);
                    return;
                }

                blurredWeight = Mathf.Clamp(blurredWeight, 0, 1f);
                float[,] blurredChanges = (float[,]) changesMap.Clone();
                blurredChanges = ApplyBlur(blurredChanges, blurRadius);
                elevationsMap.SumMaps((blurredChanges, blurredWeight), (changesMap, 1 - blurredWeight));
            }

            return elevationsMap;
        }

      
    }
}
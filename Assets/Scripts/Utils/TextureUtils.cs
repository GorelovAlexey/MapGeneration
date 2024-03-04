using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts
{
    static class TextureUtils
    {
        public static Texture2D Clear(this Texture2D t, Color c)
        {
            var pixels = t.GetPixels();
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = c;
            }
            t.SetPixels(pixels);
            return t;
        }

        public static bool IsWithinTexture(this Texture2D texture, int x, int y) => IsWithinTextureX(texture, x) && IsWithinTextureY(texture, y);
        public static bool IsWithinTextureX(this Texture2D texture, int x) => x >= 0 && x < texture.width;
        public static bool IsWithinTextureY(this Texture2D texture, int y) => y >= 0 && y < texture.height;

        public static Texture2D DrawRect(this Texture2D t, RectInt rect, Color c)
        {
            var x = Mathf.Clamp(rect.xMin, 0, t.width - 1);
            var y = Mathf.Clamp(rect.yMin, 0, t.height - 1);
            var width = Mathf.Min(rect.width, t.width - x - 1);
            var height = Mathf.Min(rect.height, t.height - y - 1);
            var pixels = new Color[width * height];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = c;
            }
            t.SetPixels(x, y, width, height, pixels);
            return t;
        }

        public static Texture2D DrawLine(this Texture2D texture, int x0, int y0, int x1, int y1, Color col)
        {
            var sx = x0 < x1 ? 1 : -1;
            var sy = y0 < y1 ? 1 : -1;

            var dx = x0 < x1 ? x1 - x0 : x0 - x1;
            var dy = y0 < y1 ? y1 - y0 : y0 - y1;
            dy = -dy;

            var e = dx + dy;

            while (true)
            {
                DrawPoint(x0, y0);
                if (x0 == x1 && y0 == y1)
                    break;
                var e2 = 2 * e;

                if (e2 >= dy)
                {
                    if (x0 == x1)
                        break;
                    e += dy;
                    x0 += sx;
                }

                if (e2 <= dx)
                {
                    if (y0 == y1)
                        break;

                    e += dx;
                    y0 += sy;
                }
            }
            
            texture.Apply();
            void DrawPoint(int x, int y)
            {
                if (x < 0 || y< 0 || x >= texture.width || y >= texture.height)
                    return;
                texture.SetPixel(x, y, col);
            }
            return texture;
        }

        public static Texture2D DrawTriangle(this Texture2D texture, int x0, int y0, int x1, int y1, int x2, int y2, Color color)
        {
            int width = texture.width;
            int height = texture.height;
            // sort the points vertically
            if (y1 > y2)
            {
                (x1, x2) = (x2, x1);
                (y1, y2) = (y2, y1);
            }
            if (y0 > y1)
            {
                (x0, x1) = (x1, x0);
                (y0, y1) = (y1, y0);
            }
            if (y1 > y2)
            {
                (x1, x2) = (x2, x1);
                (y1, y2) = (y2, y1);
            }

            double dx_far = Convert.ToDouble(x2 - x0) / (y2 - y0 + 1);
            double dx_upper = Convert.ToDouble(x1 - x0) / (y1 - y0 + 1);
            double dx_low = Convert.ToDouble(x2 - x1) / (y2 - y1 + 1);
            double xf = x0;
            double xt = x0 + dx_upper; // if y0 == y1, special case
            var yMax = y2 > height - 1 ? height - 1 : y2;
            for (var y = y0; y <= yMax; y++)
            {
                if (y >= 0)
                {
                    for (int x = (xf > 0 ? Convert.ToInt32(xf) : 0);
                        x <= (xt < width ? xt : width - 1); x++)
                        texture.SetPixel(x, y, color);
                    for (int x = (xf < width ? Convert.ToInt32(xf) : width - 1);
                        x >= (xt > 0 ? xt : 0); x--)
                        texture.SetPixel(x, y, color);
                }
                xf += dx_far;
                if (y < y1)
                    xt += dx_upper;
                else
                    xt += dx_low;
            }

            texture.Apply();
            return texture;
        }

        /*
         * 
         */

        public static Texture2D DrawTriangleSlow(this Texture2D texture, int x0, int y0, int x1, int y1, int x2, int y2, Color color)
        {
            (int x, int y) v0 = (x0, y0);
            (int x, int y) v1 = (x1, y1);
            (int x, int y) v2 = (x2, y2);

            if (!edgeFunction(v0, v1, v2))
            {
                var tmp = v1;
                v1 = v2;
                v2 = tmp;
            }

            bool edgeFunction((int x, int y) a, (int x, int y) b, (int x, int y) c)
            {
                return (c.x - a.x) * (b.y - a.y) - (c.y - a.y) * (b.x - a.x) <= 0;
            }

            bool checkPoint(int x, int y)
            {
                return edgeFunction(v0, v1, (x, y)) && edgeFunction(v1, v2, (x, y)) && edgeFunction(v2, v0, (x, y));
            }

            var maxX = Mathf.Max(v0.x, v1.x, v2.x);
            maxX = Mathf.Clamp(maxX, 0, texture.width);

            var minX = Mathf.Min(v0.x, v1.x, v2.x);
            minX = Mathf.Clamp(minX, 0, texture.width);

            var maxY = Mathf.Max(v0.y, v1.y, v2.y);
            maxY = Mathf.Clamp(maxY, 0, texture.height);

            var minY = Mathf.Min(v0.y, v1.y, v2.y);
            minY = Mathf.Clamp(minY, 0, texture.height);

            for (var x = minX; x <= maxX; x++)
            {
                for (var y = minY; y <= maxY; y++)
                {
                    if (checkPoint(x, y))
                        texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return texture;
        }

        public static Texture2D DrawTriangle2(this Texture2D texture, int x0, int y0, int x1, int y1, int x2, int y2, Color color)
        {
            (int x, int y) v0 = (x0, y0);
            (int x, int y) v1 = (x1, y1);
            (int x, int y) v2 = (x2, y2);

            var sorted = new[] {v0, v1, v2};
            sorted = sorted.OrderByDescending(x => x.y).ThenByDescending(x => x.x).ToArray();
            v0 = sorted[0];
            v1 = sorted[1];
            v2 = sorted[2];

            /* here we know that v1.y <= v2.y <= v3.y */
            /* check for trivial case of bottom-flat triangle */
            if (y1 == y2)
            {
                fillBottomFlatTriangle(v0, v1, v2);
            }
            /* check for trivial case of top-flat triangle */
            else if (y0 == y1)
            {
                fillTopFlatTriangle(v0, v1, v2);
            }
            else
            {
                var v4 = ((int)(v0.x + ((float)(v1.y - v0.y) / (float)(v2.y - v0.y)) * (v2.x - v0.x)), v1.y);
                fillBottomFlatTriangle(v0, v1, v4);
                fillTopFlatTriangle(v1, v4, v2);
            }

            void fillBottomFlatTriangle((int x, int y) v1, (int x, int y) v2, (int x, int y) v3)
            {
                float invslope1 = (v2.x - v1.x) / (float)(v2.y - v1.y);
                float invslope2 = (v3.x - v1.x) / (float)(v3.y - v1.y);

                float curx1 = v1.x;
                float curx2 = v1.x;

                for (int scanlineY = v2.y; scanlineY <= v1.y; scanlineY++)
                {
                    drawLine((int)curx1, (int)curx2, scanlineY);
                    curx1 += invslope1;
                    curx2 += invslope2;
                }
            }

            void fillTopFlatTriangle((int x, int y) v1, (int x, int y) v2, (int x, int y) v3)
            {
                float invslope1 = (v3.x - v1.x) / (float)(v3.y - v1.y);
                float invslope2 = (v3.x - v2.x) / (float)(v3.y - v2.y);

                float curx1 = v3.x;
                float curx2 = v3.x;

                for (int scanlineY = v1.y; scanlineY > v3.y; scanlineY--)
                {
                    drawLine((int)curx1, (int)curx2, scanlineY);
                    curx1 -= invslope1;
                    curx2 -= invslope2;
                }
            }

            void drawLine(int x1, int x2, int y)
            {
                if (x1 > x2)
                    (x1, x2) = (x2, x1);

                for (int x = x1; x <= x2; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return texture;
        }

    }
}

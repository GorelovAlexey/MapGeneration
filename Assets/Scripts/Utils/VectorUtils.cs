using UnityEngine;

namespace Assets.Scripts.Utils
{
    static class VectorUtils
    {
        public static bool GetCircumference(Vector2 p0, Vector2 p1, Vector2 p2, out Vector2 center)
        {
            var dA = p0.x * p0.x + p0.y * p0.y;
            var dB = p1.x * p1.x + p1.y * p1.y;
            var dC = p2.x * p2.x + p2.y * p2.y;

            var aux1 = (dA * (p2.y - p1.y) + dB * (p0.y - p2.y) + dC * (p1.y - p0.y));
            var aux2 = -(dA * (p2.x - p1.x) + dB * (p0.x - p2.x) + dC * (p1.x - p0.x));
            var div = (2 * (p0.x * (p2.y - p1.y) + p1.x * (p0.y - p2.y) + p2.x * (p1.y - p0.y)));

            if (div == 0)
            {
                center = Vector2.zero;
                return false;
            }

            center = new Vector2(aux1 / div, aux2 / div);
            return true;
        }

        // From 0 (closest from left side) to 2PI (closest from right side (clockwise))
        public static float AngleBetweenTowardsLeft(this Vector2 a, Vector2 b)
        {
            var res = AngleBetween(a, b);
            return res >= 0 ? res : 2 * Mathf.PI + res;
        }

        // from PI (180 degrees to the left) to -PI (180 degrees to the right (clockwise))
        public static float AngleBetween(this Vector2 a, Vector2 b)
        {
            var dot = a.x * b.x + a.y * b.y;
            var det = a.x * b.y - a.y * b.x;
            return Mathf.Atan2(det, dot);
        }

        public static Vector2 Rotate90(this Vector2 a, bool rotateRight)
        {
            if (rotateRight) 
                return new Vector2(a.y, -a.x);

            return new Vector2(-a.y, a.x);
        }

        // left <0 
        // right >0
        // collinear =0
        public static float GetCollinearity(Vector2 A0, Vector2 A1, Vector2 P)
        {
            return GetCollinearity(A0.x, A0.y, A1.x, A1.y, P.x, P.y);
        }

        /*
        public static float GetCollinearity(Vector2 A0, Vector2 A1, Vector2 P)
        {
            return (P.x - A0.x) * (A1.y - A0.y) - (P.y - A0.y) * (A1.x - A0.x);
        }*/

        public static float GetCollinearity(float x0, float y0, float x1, float y1, float pX, float pY)
        {

            return (pX - x0) * (y1 - y0) - (pY - y0) * (x1 - x0);
        }
    }
}

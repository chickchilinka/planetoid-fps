using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Base.EffectSystem.Utils
{
    public static class EffectsExtensions
    {
        public static Vector3 GetLocalFromGlobalPosition(Transform parent, Vector3 position, bool zZeroize)
        {
            var localPosition = parent.transform.InverseTransformPoint(position);
            if (zZeroize) localPosition.z = 0;

            return localPosition;
        }

        public static Vector3[] GetKeyPointsForFlying(Vector2 startPos, Vector2 endPos)
        {
            var middleTempPos = (startPos + endPos) / 2f;

            var middleStartPos = startPos;
            middleStartPos.y = middleTempPos.y;

            var middleEndPos = endPos;
            middleEndPos.y = (endPos.y + middleTempPos.y) / 2f;

            var keyPoints = new Vector3[]
            {
                startPos, middleStartPos, middleEndPos, endPos
            };

            return keyPoints;
        }

        public static Vector3[] GetKeyPointsForCoins(Vector2 startPos, Vector2 endPos)
        {
            var startPos2 = startPos;
            startPos2.y -= Random.Range(2f, 4f);
            startPos2.x += Random.Range(1f, 4f);

            var middleTempPos = (startPos2 + endPos) / 2f;

            var middleStartPos = startPos2;
            middleStartPos.y = middleTempPos.y;

            var middleEndPos = endPos;
            middleEndPos.y = (endPos.y + middleTempPos.y) / 2f;

            var keyPoints = new Vector3[]
            {
                startPos2, middleStartPos, middleEndPos, endPos
            };

            return keyPoints;
        }

        public static Vector3[] GetKeyPointsForShuffle(Vector2 startPos, Vector2 endPos, Vector2 center)
        {
            var radius = Vector2.Distance(startPos, Vector2.zero);
            var angle = Mathf.Atan2(startPos.y, startPos.x);

            var maxPoints = 10;
            var keyPoints = new List<Vector3>();

            keyPoints.Add(startPos);

            for (var i = 1; i <= maxPoints; i++)
            {
                var point = (GetPositionOnCircle((radius + (i / 2) * 7) / ((i / 2) + 1) , angle + i * 45 * Mathf.Deg2Rad) + Vector2.zero) / 2f;
                point += center;
                keyPoints.Add(point);
            }

            keyPoints.Add(endPos);

            return keyPoints.Select(point => { point.z = -5; return point; }).ToArray();
        }

        private static Vector2 GetPositionOnCircle(float radius, float angle)
        {
            return new Vector2(radius * Mathf.Cos(angle), radius * Mathf.Sin(angle));
        }

        public static Vector3[] GetBernsteinPath(int needPoints, Vector3[] keyPoints)
        {
            var j = 0;
            var step = 1f / needPoints;
            var result = new Vector3[needPoints + 1];

            for (var t = 0f; t < 1; t += step)
            {
                var x = 0f;
                var y = 0f;
                var z = 0f;

                for (var i = 0; i < keyPoints.Length; i++)
                {
                    var b = Polinom(i, keyPoints.Length - 1, t);
                    x += keyPoints[i].x * b;
                    y += keyPoints[i].y * b;
                    z += keyPoints[i].z * b;
                }

                result[j] = new Vector3(x, y, z);
                j++;
            }

            return result;
        }

        // Bernstein polynomial
        private static float Polinom(int i, int n, float t)
        {
            return Factorial(n) / (float)(Factorial(i) * Factorial(n - i)) * Mathf.Pow(t, i) * Mathf.Pow(1 - t, n - i);
        }

        private static int Factorial(int arg)
        {
            var result = 1;
            for (var i = 1; i <= arg; i++)
                result *= i;
            return result;
        }

    }
}
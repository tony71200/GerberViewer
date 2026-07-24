using System;
using GerberViewer.Stitching.Transforms;

namespace GerberStitching.Tests.Matching
{
    public static class TransformAssert
    {
        public static void AreEqual(double[,] expected, double[,] actual, double tolerance, string message)
        {
            if (expected == null) throw new ArgumentNullException("expected");
            if (actual == null) throw new ArgumentNullException("actual");
            if (expected.GetLength(0) != 3 || expected.GetLength(1) != 3 || actual.GetLength(0) != 3 || actual.GetLength(1) != 3)
                throw new InvalidOperationException(message + " Matrices must be double[3,3].");
            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 3; col++)
                    if (Math.Abs(expected[row, col] - actual[row, col]) > tolerance)
                        throw new InvalidOperationException(message + " Mismatch at [" + row + "," + col + "]: expected " + expected[row, col] + ", actual " + actual[row, col]);
        }

        public static void IsIdentity(Transform2D transform, double tolerance, string message)
        {
            AreEqual(Transform2D.Identity.ToArray(), transform.ToArray(), tolerance, message);
        }
    }
}

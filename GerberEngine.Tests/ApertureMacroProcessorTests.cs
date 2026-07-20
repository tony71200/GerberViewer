using System;
using System.Collections.Generic;
using System.Drawing;
using GerberEngine;

namespace GerberEngine.Tests
{
    internal static class ApertureMacroProcessorTests
    {
        private const double Tolerance = 0.0001;

        private static int Main()
        {
            var tests = new Action[]
            {
                VariableAssignmentBuildsPrimitive,
                ArithmeticBuildsExpectedPrimitive,
                UnarySignsBuildExpectedPrimitive,
                EmptyNumericTokenWarnsAndPipelineContinues,
                UnsupportedExponentNotationWarnsAndPipelineContinues
            };

            foreach (Action test in tests)
            {
                test();
                Console.WriteLine("PASS " + test.Method.Name);
            }

            return 0;
        }

        private static void VariableAssignmentBuildsPrimitive()
        {
            var warnings = new List<string>();
            var shapes = Build("$2=$1x2", "1,1,$2,0,0", new[] { 0.25 }, warnings);

            AssertEqual(1, shapes.Count, "Variable assignment should allow later primitives to build.");
            AssertEqual(0, warnings.Count, "Valid variable assignment should not warn.");
            AssertBounds(shapes[0].Path.GetBounds(), -0.25f, -0.25f, 0.5f, 0.5f);
        }

        private static void ArithmeticBuildsExpectedPrimitive()
        {
            var warnings = new List<string>();
            var shapes = Build("1,1,1+2x3,0,0", warnings);

            AssertEqual(1, shapes.Count, "Arithmetic expression should build one primitive.");
            AssertEqual(0, warnings.Count, "Supported arithmetic should not warn.");
            AssertBounds(shapes[0].Path.GetBounds(), -3.5f, -3.5f, 7.0f, 7.0f);
        }

        private static void UnarySignsBuildExpectedPrimitive()
        {
            var warnings = new List<string>();
            var shapes = Build("1,1,1,+0.5x-+2,0", warnings);

            AssertEqual(1, shapes.Count, "Unary signs should build one primitive.");
            AssertEqual(0, warnings.Count, "Supported unary signs should not warn.");
            AssertBounds(shapes[0].Path.GetBounds(), -1.5f, -0.5f, 1.0f, 1.0f);
        }

        private static void EmptyNumericTokenWarnsAndPipelineContinues()
        {
            var warnings = new List<string>();
            var shapes = Build("1,1,,0,0", "1,1,0.25,0,0", warnings);

            AssertEqual(1, shapes.Count, "Bad macro block should not stop later primitive.");
            AssertEqual(1, warnings.Count, "Empty numeric token should produce one warning.");
            AssertContains(warnings[0], "empty numeric token", "Warning should document empty numeric token.");
        }

        private static void UnsupportedExponentNotationWarnsAndPipelineContinues()
        {
            var warnings = new List<string>();
            var shapes = Build("1,1,1e3,0,0", "1,1,0.25,0,0", warnings);

            AssertEqual(1, shapes.Count, "Unsupported expression syntax should not stop later primitive.");
            AssertEqual(1, warnings.Count, "Unsupported exponent notation should produce one warning.");
            AssertContains(warnings[0], "unsupported expression syntax", "Warning should document unsupported expression syntax.");
        }

        private static List<MacroShape> Build(string block, List<string> warnings)
        {
            return Build(new[] { block }, new double[0], warnings);
        }

        private static List<MacroShape> Build(string block1, string block2, List<string> warnings)
        {
            return Build(new[] { block1, block2 }, new double[0], warnings);
        }

        private static List<MacroShape> Build(string block, double[] args, List<string> warnings)
        {
            return Build(new[] { block }, args, warnings);
        }

        private static List<MacroShape> Build(string assignment, string primitive, double[] args, List<string> warnings)
        {
            return Build(new[] { assignment, primitive }, args, warnings);
        }

        private static List<MacroShape> Build(IEnumerable<string> blocks, double[] args, List<string> warnings)
        {
            var macro = new ApertureMacro { Name = "TEST" };
            macro.Blocks.AddRange(blocks);
            return ApertureMacroProcessor.Build(macro, args, 1.0, warnings);
        }

        private static void AssertBounds(RectangleF actual, float x, float y, float width, float height)
        {
            AssertNear(x, actual.X, "Bounds X mismatch.");
            AssertNear(y, actual.Y, "Bounds Y mismatch.");
            AssertNear(width, actual.Width, "Bounds width mismatch.");
            AssertNear(height, actual.Height, "Bounds height mismatch.");
        }

        private static void AssertNear(float expected, float actual, string message)
        {
            if (Math.Abs(expected - actual) > Tolerance)
                throw new InvalidOperationException(message + " Expected " + expected + ", got " + actual + ".");
        }

        private static void AssertEqual(int expected, int actual, string message)
        {
            if (expected != actual)
                throw new InvalidOperationException(message + " Expected " + expected + ", got " + actual + ".");
        }

        private static void AssertContains(string actual, string expectedSubstring, string message)
        {
            if (actual.IndexOf(expectedSubstring, StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(message + " Expected substring '" + expectedSubstring + "' in '" + actual + "'.");
        }
    }
}

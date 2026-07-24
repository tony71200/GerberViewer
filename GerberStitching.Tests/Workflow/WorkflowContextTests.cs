using System;
using System.Reflection;
using System.Windows.Forms;
using GerberViewer.Stitching.Alignment;
using GerberViewer.Stitching.Models;
using GerberViewer.Views;
using GerberViewer.Workflow.Models;

namespace GerberStitching.Tests.Workflow
{
    public static class WorkflowContextTests
    {
        public static void RunAll()
        {
            FirstContextAssignmentSharesCanonicalConfig();
            ReplacingContextUnsubscribesOldAndSubscribesNew();
            StateSemanticsAreExplicit();
        }

        private static void FirstContextAssignmentSharesCanonicalConfig()
        {
            var context = new WorkflowContext();
            context.ManifestPath = @"C:\baseline\sample_manifest.json";
            context.OutputDirectory = @"C:\baseline\out";
            context.AlignStitchConfig.NccMinScore = 0.91;

            using (var control = new AlignStitchingControl())
            {
                control.WorkflowContext = context;

                AssertSame(context.AlignStitchConfig, PrivateField(control, "_config"), "Control must use the first assigned context config instance.");
                AssertSame(context.AlignStitchConfig, PrivateControl<PropertyGrid>(control, "alignConfigGrid").SelectedObject, "PropertyGrid must bind to the canonical context config.");
                AssertEqual(@"C:\baseline\sample_manifest.json", PrivateControl<TextBox>(control, "txtManifestPath").Text, "First context assignment must refresh manifest UI.");
                AssertEqual(@"C:\baseline\out", PrivateControl<TextBox>(control, "txtOutputFolder").Text, "First context assignment must refresh output UI.");
            }
        }

        private static void ReplacingContextUnsubscribesOldAndSubscribesNew()
        {
            var oldContext = new WorkflowContext();
            oldContext.ManifestPath = @"C:\old\sample_manifest.json";
            oldContext.OutputDirectory = @"C:\old\out";
            var newContext = new WorkflowContext();
            newContext.ManifestPath = @"C:\new\sample_manifest.json";
            newContext.OutputDirectory = @"C:\new\out";

            using (var control = new AlignStitchingControl())
            {
                control.WorkflowContext = oldContext;
                control.WorkflowContext = newContext;

                oldContext.ManifestPath = @"C:\old\changed.json";
                oldContext.OutputDirectory = @"C:\old\changed-out";
                oldContext.NotifyChanged();

                AssertEqual(@"C:\new\sample_manifest.json", PrivateControl<TextBox>(control, "txtManifestPath").Text, "Old context notification must not update control after replacement.");
                AssertEqual(@"C:\new\out", PrivateControl<TextBox>(control, "txtOutputFolder").Text, "Old context output notification must be unsubscribed after replacement.");

                newContext.ManifestPath = @"C:\new\changed.json";
                newContext.OutputDirectory = @"C:\new\changed-out";
                newContext.NotifyChanged();

                AssertEqual(@"C:\new\changed.json", PrivateControl<TextBox>(control, "txtManifestPath").Text, "New context notification must refresh manifest UI.");
                AssertEqual(@"C:\new\changed-out", PrivateControl<TextBox>(control, "txtOutputFolder").Text, "New context notification must refresh output UI.");
                AssertSame(newContext.AlignStitchConfig, PrivateControl<PropertyGrid>(control, "alignConfigGrid").SelectedObject, "Replacement context config must be rebound.");
            }
        }

        private static void StateSemanticsAreExplicit()
        {
            var captured = new CapturedImageInfo { OrderIndex = 7, Row = 1, Column = 2 };
            var pose = Homography.Identity();

            var solved = TileWorkflowState.From(captured, pose, PoseSource.SampleAlignment, null, null);
            AssertTrue(solved.HasValidPose, "Solved pose must retain HasValidPose compatibility.");
            AssertTrue(solved.AlignmentSucceeded, "SampleAlignment pose must mark AlignmentSucceeded.");
            AssertTrue(solved.IsStitchable, "SampleAlignment pose must be stitchable.");
            AssertFalse(solved.IsFallbackPose, "SampleAlignment pose must not be fallback.");

            var fallback = TileWorkflowState.From(captured, pose, PoseSource.ExpectedGridOffset, null, "fallback");
            AssertTrue(fallback.HasValidPose, "Fallback pose may still have a finite pose.");
            AssertFalse(fallback.AlignmentSucceeded, "ExpectedGridOffset must not be alignment success.");
            AssertFalse(fallback.IsStitchable, "ExpectedGridOffset must not be stitchable by default.");
            AssertTrue(fallback.IsFallbackPose, "ExpectedGridOffset must be marked fallback.");

            var failed = TileWorkflowState.From(captured, null, PoseSource.Failed, null, "failed");
            AssertFalse(failed.HasValidPose, "Failed state with null pose must not have valid pose.");
            AssertFalse(failed.AlignmentSucceeded, "Failed state must not be alignment success.");
            AssertFalse(failed.IsStitchable, "Failed state must not be stitchable.");
            AssertFalse(failed.IsFallbackPose, "Failed state must not be fallback pose.");
        }

        private static object PrivateField(object owner, string name)
        {
            var field = owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new InvalidOperationException("Missing private field: " + name);
            return field.GetValue(owner);
        }

        private static T PrivateControl<T>(object owner, string name) where T : class
        {
            var value = PrivateField(owner, name) as T;
            if (value == null) throw new InvalidOperationException("Private field is not " + typeof(T).Name + ": " + name);
            return value;
        }

        private static void AssertSame(object expected, object actual, string message)
        {
            if (!object.ReferenceEquals(expected, actual)) throw new InvalidOperationException(message);
        }

        private static void AssertEqual(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal)) throw new InvalidOperationException(message + " Expected: " + expected + "; Actual: " + actual);
        }

        private static void AssertTrue(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static void AssertFalse(bool value, string message)
        {
            if (value) throw new InvalidOperationException(message);
        }
    }
}

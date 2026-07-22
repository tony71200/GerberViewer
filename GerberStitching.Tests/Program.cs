using System;
using GerberStitching.Tests.Comparison;
using GerberStitching.Tests.Imaging;
using GerberStitching.Tests.Matching;
using GerberStitching.Tests.UI;
using GerberStitching.Tests.Stitching;
using GerberStitching.Tests.Workflow;

namespace GerberStitching.Tests
{
    internal static class Program
    {
        [STAThread]
        private static int Main()
        {
            try
            {
                WorkflowContextTests.RunAll();
                PathCanvasControlTests.RunAll();
                GlobalTransformStitcherTests.RunAll();
                SampleComparisonServiceTests.RunAll();
                ImageInteropTests.RunAll();
                PharseCorrMatcherTests.RunAll();
                EccMatcherTests.RunAll();
                NCC_HalconMatcherTests.RunAll();
                DirectMatcherPipelineTests.RunAll();
                NeighborRecoveryTests.RunAll();
                Console.WriteLine("All GerberStitching tests passed.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
        }
    }
}

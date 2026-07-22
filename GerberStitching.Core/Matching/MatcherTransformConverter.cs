using GerberViewer.Stitching.Transforms;
using OpenCvSharp;

namespace GerberViewer.Stitching.Matching
{
    public static class MatcherTransformConverter
    {
        public static Transform2D FromTranslation(double movingToReferenceX, double movingToReferenceY)
        {
            return Transform2D.Translation(movingToReferenceX, movingToReferenceY);
        }

        public static Mat ToOpenCvMatCopy(Transform2D transform)
        {
            return transform.ToMatCv64FCopy();
        }

        public static Transform2D FromOpenCvMat(Mat transform)
        {
            return Transform2D.FromMatCv64F(transform);
        }
    }
}

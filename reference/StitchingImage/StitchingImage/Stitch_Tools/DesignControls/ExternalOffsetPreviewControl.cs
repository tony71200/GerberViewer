using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using StitchingImage.Stitch_Tools.Matcher;
using StitchingImage.Stitch_Tools.Utils;

namespace StitchingImage.Stitch_Tools.DesignControls
{
    public partial class ExternalOffsetPreviewControl : UserControl
    {
        private const double PreviewMegapix = 2.0;
        private readonly ComboBox _cmbMode = new ComboBox();
        private readonly TextBox _txtHFrom = new TextBox();
        private readonly TextBox _txtHTo = new TextBox();
        private readonly TextBox _txtVFrom = new TextBox();
        private readonly TextBox _txtVTo = new TextBox();

        private readonly TextBox _txtHTx = new TextBox();
        private readonly TextBox _txtHTy = new TextBox();
        private readonly TextBox _txtHTheta = new TextBox();
        private readonly TextBox _txtVTx = new TextBox();
        private readonly TextBox _txtVTy = new TextBox();
        private readonly TextBox _txtVTheta = new TextBox();

        private readonly NumericUpDown _nudHTx = new NumericUpDown();
        private readonly NumericUpDown _nudHTy = new NumericUpDown();
        private readonly NumericUpDown _nudVTx = new NumericUpDown();
        private readonly NumericUpDown _nudVTy = new NumericUpDown();

        private readonly TextBox _txtOverlapH = new TextBox();
        private readonly TextBox _txtOverlapV = new TextBox();
        private readonly TextBox _txtTimeH = new TextBox();
        private readonly TextBox _txtTimeV = new TextBox();

        private readonly ImagePreviewControl _previewH = new ImagePreviewControl();
        private readonly ImagePreviewControl _previewV = new ImagePreviewControl();
        private readonly StitchingConfig _matchConfig = new StitchingConfig();

        private readonly Button _btnRun = new Button();
        private readonly Button _btnApply = new Button();

        public ExternalOffsetPreviewControl()
        {
            Dock = DockStyle.Fill;
            // [Codex] [Change time: 260324] [Move UI-design initialization into ExternalOffsetPreviewControl.Design.cs for easier form layout editing.]
//            BuildLayout();
            InitializeDesignLayout();
        }

        private void UpdateMode()
        {
            var manual = _cmbMode.SelectedIndex == 1;
            _nudHTx.Enabled = manual;
            _nudHTy.Enabled = manual;
            _nudVTx.Enabled = manual;
            _nudVTy.Enabled = manual;
        }

        private static void PickFile(TextBox txt)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image files|*.bmp;*.png;*.jpg;*.jpeg;*.tif;*.tiff|All files|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                    txt.Text = ofd.FileName;
            }
        }

        private void Run(bool manual)
        {
            manual = manual || _cmbMode.SelectedIndex == 1;
            Render(_txtHFrom.Text, _txtHTo.Text, manual, (double)_nudHTx.Value, (double)_nudHTy.Value, true, _previewH, _txtHTx, _txtHTy, _txtHTheta, _txtOverlapH, _txtTimeH);
            Render(_txtVFrom.Text, _txtVTo.Text, manual, (double)_nudVTx.Value, (double)_nudVTy.Value, false, _previewV, _txtVTx, _txtVTy, _txtVTheta, _txtOverlapV, _txtTimeV);
        }

        // [Codex] [Change time: 260324] [Add missing transfer actions to copy computed offsets into manual controls quickly.]
        private void CopyResultToManual(bool horizontal)
        {
            if (horizontal)
            {
                if (decimal.TryParse(_txtHTx.Text, out var tx))
                    _nudHTx.Value = Math.Max(_nudHTx.Minimum, Math.Min(_nudHTx.Maximum, tx));
                if (decimal.TryParse(_txtHTy.Text, out var ty))
                    _nudHTy.Value = Math.Max(_nudHTy.Minimum, Math.Min(_nudHTy.Maximum, ty));
            }
            else
            {
                if (decimal.TryParse(_txtVTx.Text, out var tx))
                    _nudVTx.Value = Math.Max(_nudVTx.Minimum, Math.Min(_nudVTx.Maximum, tx));
                if (decimal.TryParse(_txtVTy.Text, out var ty))
                    _nudVTy.Value = Math.Max(_nudVTy.Minimum, Math.Min(_nudVTy.Maximum, ty));
            }

            _cmbMode.SelectedIndex = 1;
            UpdateMode();
        }

        // [Codex] [Change time: 260323] [Bring matching/preview behavior closer to OffsetPreviewControl and support Unicode image paths via ImageRead]
        private void Render(string aPath, string bPath, bool manual, double manualTx, double manualTy, bool isHorizontal, ImagePreviewControl preview, TextBox txBox, TextBox tyBox, TextBox thetaBox, TextBox overlap, TextBox matchTime)
        {
            if (string.IsNullOrWhiteSpace(aPath) || string.IsNullOrWhiteSpace(bPath))
                return;

            var start = DateTime.UtcNow;
            using (var imgARaw = LoadPreviewMat(aPath, PreviewMegapix, out var scaleA))
            using (var imgBRaw = LoadPreviewMat(bPath, PreviewMegapix, out var scaleB))
            using (var imgA = ApplyConvertScale(imgARaw, _matchConfig))
            using (var imgB = ApplyConvertScale(imgBRaw, _matchConfig))
            {
                if (imgA.Empty() || imgB.Empty())
                    return;

                double tx = manualTx;
                double ty = manualTy;
                double thetaRad = 0;
                double overlapPercent = 0;

                using (var rigidFull = manual
                    ? CreateTranslationMat(manualTx, manualTy, 0)
                    : BuildAutoTransform(aPath, bPath, imgA, imgB, scaleA, isHorizontal, out tx, out ty, out thetaRad, out overlapPercent))
                using (var rigidPreview = ScaleRigidTransform(rigidFull, scaleA, scaleB))
                using (var merged = ComposePreview(imgA, imgB, rigidPreview))
                {
                    preview.SetImage(BitmapConverter.ToBitmap(merged), preserveView: false);
                    preview.ManualScaleToFull = scaleA > 0 ? 1.0 / scaleA : 1.0;

                    if (manual)
                    {
                        tx = manualTx;
                        ty = manualTy;
                        overlapPercent = EstimateOverlapPercent(imgA, imgB, rigidPreview);
                    }
                }

                txBox.Text = tx.ToString("0.###");
                tyBox.Text = ty.ToString("0.###");
                thetaBox.Text = (thetaRad * 180.0 / Math.PI).ToString("0.###");
                overlap.Text = overlapPercent > 0 ? overlapPercent.ToString("0.##") : "N/A";
                matchTime.Text = (DateTime.UtcNow - start).TotalSeconds.ToString("0.###");
            }
        }

        private Mat BuildAutoTransform(string aPath, string bPath, Mat imgA, Mat imgB, double scaleA, bool isHorizontal, out double tx, out double ty, out double thetaRad, out double overlapPercent)
        {
            tx = 0;
            ty = 0;
            thetaRad = 0;
            overlapPercent = 0;

            var direction = isHorizontal ? Direction.Horizontal : Direction.Vertical;
            var estimateDistX = imgA.Width / Math.Max(scaleA, 1e-9);
            var estimateDistY = imgA.Height / Math.Max(scaleA, 1e-9);
            var dxRobot = isHorizontal ? estimateDistX : 0.0;
            var dyRobot = isHorizontal ? 0.0 : estimateDistY;

            using (var matcher = PairMatching.CreateMatcher(_matchConfig.Clone()))
            {
                var pr = matcher.MatchPair(aPath, bPath, dxRobot, dyRobot, estimateDistX, estimateDistY, direction);
                if (pr?.Eval == null || pr.MRigidBToA == null || !pr.Eval.IsMatch)
                {
                    tx = isHorizontal ? estimateDistX : 0.0;
                    ty = isHorizontal ? 0.0 : estimateDistY;
                    overlapPercent = 0.0;
                    pr?.HFullBToA?.Dispose();
                    pr?.MRigidBToA?.Dispose();
                    return CreateTranslationMat(tx, ty, 0);
                }

                tx = pr.Tx;
                ty = pr.Ty;
                thetaRad = pr.DThetaRad;
                overlapPercent = (pr.Eval?.OverlapRatio ?? 0) * 100.0;
                var rigid = pr.MRigidBToA.Clone();
                pr.HFullBToA?.Dispose();
                pr.MRigidBToA?.Dispose();
                return rigid;
            }
        }

        private static Mat ApplyConvertScale(Mat src, StitchingConfig cfg)
        {
            if (src == null || src.Empty())
                return src?.Clone() ?? new Mat();

            var dst = new Mat();
            Cv2.ConvertScaleAbs(src, dst, cfg?.ConvertAlpha ?? 1.0, cfg?.ConvertBeta ?? 0.0);
            return dst;
        }

        private static Mat LoadPreviewMat(string path, double maxMegapix, out double scale)
        {
            var img = ImageRead.ReadImage(path, ImreadModes.Color);
            if (img.Empty())
            {
                scale = 1;
                return img;
            }

            double megapix = img.Width * img.Height / 1_000_000.0;
            if (megapix <= maxMegapix)
            {
                scale = 1;
                return img;
            }

            scale = Math.Sqrt(maxMegapix / Math.Max(1e-9, megapix));
            var newSize = new OpenCvSharp.Size(
                Math.Max(1, (int)(img.Width * scale)),
                Math.Max(1, (int)(img.Height * scale)));

            var resized = new Mat();
            Cv2.Resize(img, resized, newSize, 0, 0, InterpolationFlags.Area);
            img.Dispose();
            return resized;
        }

        private static Mat CreateTranslationMat(double tx, double ty, double thetaRad)
        {
            var cos = Math.Cos(thetaRad);
            var sin = Math.Sin(thetaRad);
            var mat = new Mat(2, 3, MatType.CV_64F);
            mat.Set(0, 0, cos);
            mat.Set(0, 1, -sin);
            mat.Set(1, 0, sin);
            mat.Set(1, 1, cos);
            mat.Set(0, 2, tx);
            mat.Set(1, 2, ty);
            return mat;
        }

        private static Mat ScaleRigidTransform(Mat rigidFull, double scaleA, double scaleB)
        {
            var rigid3 = Mat.Eye(3, 3, MatType.CV_64F).ToMat();
            for (int r = 0; r < 2; r++)
            {
                for (int c = 0; c < 3; c++)
                    rigid3.Set(r, c, rigidFull.At<double>(r, c));
            }

            var sA = Mat.Eye(3, 3, MatType.CV_64F).ToMat();
            sA.Set(0, 0, scaleA);
            sA.Set(1, 1, scaleA);

            var sBInv = Mat.Eye(3, 3, MatType.CV_64F).ToMat();
            sBInv.Set(0, 0, 1.0 / Math.Max(1e-9, scaleB));
            sBInv.Set(1, 1, 1.0 / Math.Max(1e-9, scaleB));

            using (var temp = sA * rigid3)
            using (var scaled = temp * sBInv)
            {
                var scaledMat = scaled.ToMat();
                var outMat = new Mat(2, 3, MatType.CV_64F);
                for (int r = 0; r < 2; r++)
                {
                    for (int c = 0; c < 3; c++)
                        outMat.Set(r, c, scaledMat.At<double>(r, c));
                }
                scaledMat.Dispose();
                return outMat;
            }
        }

        private static Mat ComposePreview(Mat imgA, Mat imgB, Mat rigidPreview)
        {
            var cornersB = new[]
            {
                new Point2f(0, 0),
                new Point2f(imgB.Width, 0),
                new Point2f(imgB.Width, imgB.Height),
                new Point2f(0, imgB.Height)
            };

            var cornersBTrans = cornersB.Select(p => ApplyTransform(rigidPreview, p)).ToArray();
            var allX = cornersBTrans.Select(p => p.X).Concat(new[] { 0f, (float)imgA.Width });
            var allY = cornersBTrans.Select(p => p.Y).Concat(new[] { 0f, (float)imgA.Height });

            var minX = allX.Min();
            var minY = allY.Min();
            var maxX = allX.Max();
            var maxY = allY.Max();

            var shiftX = minX < 0 ? -minX : 0;
            var shiftY = minY < 0 ? -minY : 0;

            var canvasW = Math.Max((int)Math.Ceiling(maxX + shiftX), imgA.Width + (int)shiftX);
            var canvasH = Math.Max((int)Math.Ceiling(maxY + shiftY), imgA.Height + (int)shiftY);

            var shiftMat = rigidPreview.Clone();
            shiftMat.Set(0, 2, shiftMat.At<double>(0, 2) + shiftX);
            shiftMat.Set(1, 2, shiftMat.At<double>(1, 2) + shiftY);

            var canvasB = new Mat(new OpenCvSharp.Size(canvasW, canvasH), MatType.CV_8UC3, Scalar.Black);
            Cv2.WarpAffine(imgB, canvasB, shiftMat, new OpenCvSharp.Size(canvasW, canvasH), InterpolationFlags.Linear, BorderTypes.Constant, Scalar.Black);

            var canvasA = new Mat(new OpenCvSharp.Size(canvasW, canvasH), MatType.CV_8UC3, Scalar.Black);
            using (var target = new Mat(canvasA, new Rect((int)Math.Round(shiftX), (int)Math.Round(shiftY), imgA.Width, imgA.Height)))
                imgA.CopyTo(target);

            var blended = new Mat();
            Cv2.AddWeighted(canvasA, 0.5, canvasB, 0.5, 0, blended);
            canvasA.Dispose();
            canvasB.Dispose();
            shiftMat.Dispose();
            return blended;
        }

        private static Point2f ApplyTransform(Mat mat, Point2f pt)
        {
            var x = mat.At<double>(0, 0) * pt.X + mat.At<double>(0, 1) * pt.Y + mat.At<double>(0, 2);
            var y = mat.At<double>(1, 0) * pt.X + mat.At<double>(1, 1) * pt.Y + mat.At<double>(1, 2);
            return new Point2f((float)x, (float)y);
        }

        private static double EstimateOverlapPercent(Mat imgA, Mat imgB, Mat rigidPreview)
        {
            var cornersB = new[]
            {
                new Point2f(0, 0),
                new Point2f(imgB.Width, 0),
                new Point2f(imgB.Width, imgB.Height),
                new Point2f(0, imgB.Height)
            };

            var cornersBTrans = cornersB.Select(p => ApplyTransform(rigidPreview, p)).ToArray();
            var interLeft = Math.Max(0f, cornersBTrans.Min(p => p.X));
            var interRight = Math.Min((float)imgA.Width, cornersBTrans.Max(p => p.X));
            var interTop = Math.Max(0f, cornersBTrans.Min(p => p.Y));
            var interBottom = Math.Min((float)imgA.Height, cornersBTrans.Max(p => p.Y));
            var interW = Math.Max(0, interRight - interLeft);
            var interH = Math.Max(0, interBottom - interTop);
            var baseArea = imgA.Width * imgA.Height;
            if (baseArea <= 0) return 0;
            return interW * interH / baseArea * 100.0;
        }
    }
}

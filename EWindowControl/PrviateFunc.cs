using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Data.Common;
using System.Runtime.InteropServices;
using System.Reflection;
using static System.Windows.Forms.MonthCalendar;
using static System.Net.Mime.MediaTypeNames;
using System.Text.RegularExpressions;

namespace EWindowControl
{
    public partial class EWindowControl
    {
        /// <summary>
        /// Halcon-supported colors [hex]
        /// </summary>
        private readonly string[] HColor =
        {
            //75% alpha
            "#000000c0",
            "#ffffffc0",
            "#ff0000c0",
            "#00ff00c0",
            "#0000ffc0",
            "#696969c0",
            "#bebebec0",
            "#d3d3d3c0",
            "#00ffffc0",
            "#ff00ffc0",
            "#ffff00c0",
            "#7b68eec0",
            "#ff7f50c0",
            "#6a5acdc0",
            "#00ff7fc0",
            "#ff4500c0",
            "#556b2fc0",
            "#ffc0cbc0",
            "#5f9ea0c0",
            "#daa520c0",
            "#ffa500c0",
            "#ffd700c0",
            "#228b22c0",
            "#6495edc0",
            "#000080c0",
            "#40e0d0c0",
            "#483d8bc0",
            "#add8e6c0",
            "#cd5c5cc0",
            "#d02090c0",
            "#b0c4dec0",
            "#f0e68cc0",
            "#ee82eec0",
            "#b22222c0",
            "#191970c0",

            //50% alpha
            "#00000080",
            "#ffffff80",
            "#ff000080",
            "#00ff0080",
            "#0000ff80",
            "#69696980",
            "#bebebe80",
            "#d3d3d380",
            "#00ffff80",
            "#ff00ff80",
            "#ffff0080",
            "#7b68ee80",
            "#ff7f5080",
            "#6a5acd80",
            "#00ff7f80",
            "#ff450080",
            "#556b2f80",
            "#ffc0cb80",
            "#5f9ea080",
            "#daa52080",
            "#ffa50080",
            "#ffd70080",
            "#228b2280",
            "#6495ed80",
            "#00008080",
            "#40e0d080",
            "#483d8b80",
            "#add8e680",
            "#cd5c5c80",
            "#d0209080",
            "#b0c4de80",
            "#f0e68c80",
            "#ee82ee80",
            "#b2222280",
            "#19197080",

            //25% alpha
            "#00000040",
            "#ffffff40",
            "#ff000040",
            "#00ff0040",
            "#0000ff40",
            "#69696940",
            "#bebebe40",
            "#d3d3d340",
            "#00ffff40",
            "#ff00ff40",
            "#ffff0040",
            "#7b68ee40",
            "#ff7f5040",
            "#6a5acd40",
            "#00ff7f40",
            "#ff450040",
            "#556b2f40",
            "#ffc0cb40",
            "#5f9ea040",
            "#daa52040",
            "#ffa50040",
            "#ffd70040",
            "#228b2240",
            "#6495ed40",
            "#00008040",
            "#40e0d040",
            "#483d8b40",
            "#add8e640",
            "#cd5c5c40",
            "#d0209040",
            "#b0c4de40",
            "#f0e68c40",
            "#ee82ee40",
            "#b2222240",
            "#19197040",

            //0% alpha
            "#000000",
            "#ffffff",
            "#ff0000",
            "#00ff00",
            "#0000ff",
            "#696969",
            "#bebebe",
            "#d3d3d3",
            "#00ffff",
            "#ff00ff",
            "#ffff00",
            "#7b68ee",
            "#ff7f50",
            "#6a5acd",
            "#00ff7f",
            "#ff4500",
            "#556b2f",
            "#ffc0cb",
            "#5f9ea0",
            "#daa520",
            "#ffa500",
            "#ffd700",
            "#228b22",
            "#6495ed",
            "#000080",
            "#40e0d0",
            "#483d8b",
            "#add8e6",
            "#cd5c5c",
            "#d02090",
            "#b0c4de",
            "#f0e68c",
            "#ee82ee",
            "#b22222",
            "#191970"
        };

        #region HotKey

        private void CreateHotKeyTable()
        {
            HotKeyTable = new Dictionary<int, Tuple<uint, Keys>>();

            ImportSystemDLL.KeyModifiers keyModifiers = ImportSystemDLL.KeyModifiers.Control;
            // copy ROI
            HotKeyTable.Add(6000, new Tuple<uint, Keys>((uint)keyModifiers, Keys.C));
            // paste ROI
            HotKeyTable.Add(6001, new Tuple<uint, Keys>((uint)keyModifiers, Keys.V));


            keyModifiers = ImportSystemDLL.KeyModifiers.None;
            // cancel ROI
            HotKeyTable.Add(6002, new Tuple<uint, Keys>((uint)keyModifiers, Keys.Back));
            // delete ROI
            HotKeyTable.Add(6003, new Tuple<uint, Keys>((uint)keyModifiers, Keys.Delete));
        }
        /// <summary>
        /// register shortcut keys
        /// </summary>
        private void RegisterHotKey()
        {
            // initialize
            //enableCopyROI = false;
            //enablePasteROI = false;
            //CopyROIParm = null;
            foreach (var item in HotKeyTable)
            {
                ImportSystemDLL.RegisterHotKey(Handle, item.Key, item.Value.Item1, item.Value.Item2); //register hotkey as
            }

            //int HotKeyId = 6000;
            //ImportSystemDLL.KeyModifiers keyModifiers = ImportSystemDLL.KeyModifiers.Control;

            //// copy ROI
            //ImportSystemDLL.RegisterHotKey(Handle, HotKeyId, (uint)keyModifiers, Keys.C); //register hotkey as
            //HotKeyId++;
            //ImportSystemDLL.RegisterHotKey(Handle, HotKeyId, (uint)keyModifiers, Keys.V); //register hotkey as
            //keyModifiers = ImportSystemDLL.KeyModifiers.None;
            //HotKeyId++;
            //ImportSystemDLL.RegisterHotKey(Handle, HotKeyId, (uint)keyModifiers, Keys.Escape); //register hotkey as
        }
        /// <summary>
        /// unregister shortcut keys
        /// </summary>
        private void UnRegisterHotKey()
        {
            // initialize
            //enableCopyROI=false;
            //enablePasteROI=false;
            //CopyROIParm = null;

            foreach (var item in HotKeyTable)
            {
                ImportSystemDLL.UnregisterHotKey(Handle, item.Key); //unregister shortcut key
            }

            //int HotKeyId = 6000;
            //ImportSystemDLL.UnregisterHotKey(Handle, HotKeyId); //unregister shortcut key
            //HotKeyId++;
            //ImportSystemDLL.UnregisterHotKey(Handle, HotKeyId); //unregister shortcut key
            //HotKeyId++;
            //ImportSystemDLL.UnregisterHotKey(Handle, HotKeyId); //unregister shortcut key
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m) // override the WndProc function
        {
            const int WM_HOTKEY = 0x0312; //hotkey-detection flag
            const int WM_KEYDOWN = 0x100;

            const int WM_SYSKEYDOWN = 0x104;

            const int WM_KEYUP = 0X101;
            const int WM_SYSCHAR = 0X106;
            const int WM_SYSKEYUP = 0X105;

            const int WM_CHAR = 0X102;

            try
            {
                //ProcessHotkeySetBinCode(m);
                switch (m.Msg)
                {
                    case WM_HOTKEY:
                        ProcessHotkeySetBinCode(m);
                        break;

                }
            }
            catch (Exception ex)
            {

            }

            base.WndProc(ref m);
        }

        private void ProcessHotkeySetBinCode(Message m)
        {
            IntPtr id = m.WParam; //IntPtr is the platform-specific type used to represent a pointer or handle

            string sid = id.ToString();

            int keyId = Convert.ToInt32(sid);

            switch (keyId)
            {
                case 6000:
                    CopyRoI();
                    if (EHotkeyEvent != null)
                        EHotkeyEvent(CmdHotKey.Copy);
                    break;
                case 6001:
                    PasteRoI();
                    if (EHotkeyEvent != null)
                        EHotkeyEvent(CmdHotKey.Paste);
                    break;
                case 6002:
                    CancelROI_Action();
                    if (EHotkeyEvent != null)
                        EHotkeyEvent(CmdHotKey.Cancel);
                    break;
                case 6003:
                    DeleteROI();
                    if (EHotkeyEvent != null)
                        EHotkeyEvent(CmdHotKey.Delete);
                    break;
            }
        }
        #endregion

        #region private functions
        /// <summary>
        /// initialize flags
        /// </summary>
        private void InitialFlag()
        {
            flag_AutoDrawingDefaultROI = false;
            flag_MoveImage = false;
            flag_CopyROI = false;
            flag_PasteROI = false;
            flag_DrawingROI_FirstMouseDown = false;
            flag_DrawingROI_NextPosIsClick = false;
            flag_EditROI = false;
            flag_Magnifier = false;
            flag_Erase = false;
            flag_Erase_MouseIsDown = false;
            flag_Erase_Recovery = false;
            flag_MagicWand = false;
            flag_DrawingROI = false;
            flag_SelectedROI=false;
            flag_showRegion = false;
            flag_measure = false;
            flag_ZoomRoi = false;
            flag_ZoomRoi_Batch = false;

            if (CopyROIParm != null)
            {
                flag_CopyROI = true;
            }
        }
        /// <summary>
        /// pan off, zoom off
        /// </summary>
        private void StopWinZoomInOutAndMove()
        {
            flag_Erase = false;
            flag_MoveImage = false;
            enableZoomInOut = 0;
            flag_Magnifier = false;
            hSmartWindowControl1.HMoveContent = flag_MoveImage;
            hSmartWindowControl1.Cursor = Cursors.Default;
            winOperate = 0;
            drawingType = RoiType.None;
        }
        /// <summary>
        /// initialize
        /// </summary>
        private void InitialImage()
        {
            // Determine format
            HTuple Channel = new HTuple();
            HOperatorSet.CountChannels(ho_Source, out Channel);
            if (Channel.Length == 0)
            {
                ho_Source = null;
                return;
            }

            if (Channel == 3)
                _isColor = true;
            else
                _isColor = false;

            image_W = new HTuple();
            image_H = new HTuple();
            HOperatorSet.GetImageSize(ho_Source, out image_W, out image_H);
            //hWindow.SetPart(0, 0, -1, -1);
            hWindow.ClearWindow();
            //hWindow.DispObj(ho_Source);

            if (ho_Source_bak != null && ho_Source_bak.IsInitialized())
            {
                ho_Source_bak.Dispose();
                ho_Source_bak = null;
            }

            BrushEraseRegion = null;

            DispSoureImage();
            //DispSoureImage();

        }
        /// <summary>
        /// color image HObject -> HImage3
        /// </summary>
        private HObject ConverImage(Bitmap bmp)
        {
            if (ho_Source == null)
                ho_Source = new HObject();

            switch (bmp.PixelFormat)
            {
                case PixelFormat.Format24bppRgb:
                    Bitmap2HObjectBpp24(bmp, out ho_Source);
                    break;
                case PixelFormat.Format8bppIndexed:
                    Bitmap2HObjectBpp8(bmp, out ho_Source);
                    break;
                case PixelFormat.Format32bppArgb:
                    Bitmap2HObjectBpp32(bmp, out ho_Source);
                    break;
                case PixelFormat.Format32bppRgb:
                    Bitmap2HObjectBpp32(bmp, out ho_Source);
                    break;
                case PixelFormat.Format16bppRgb565:
                    Bitmap2HObjectBpp32_565(bmp, out ho_Source);
                    break;
                default:
                    bmp = null;
                    return null;
            }
            return ho_Source;
        }

        /// <summary>
        /// received empty image
        /// </summary>
        private void NullSource()
        {
            // this must be added; otherwise the designer form will error
            if (!DesignMode)
            {
                //ho_Source=null;
                //if (ho_Source != null)
                //    ho_Source.Dispose();
                if (ho_Source_bak != null)
                    ho_Source_bak.Dispose();
                if (_showRegion != null && _showRegion.IsInitialized())
                    _showRegion.Dispose();

                lb_Info.Text = "---";
                if (hWindow != null)
                {
                    hWindow.ClearWindow();
                    ClearSoureImage();
                }
                return;
            }
        }
        /// <summary>
        /// zoom in/out
        /// </summary>
        /// <param name="point"></param>
        /// <param name="ratio"></param>
        private void ZoomInOut(Point point, int ratio)
        {
            try
            {
                HOperatorSet.HomMat2dIdentity(out var homMat2DIdentity);
                hWindow.ConvertCoordinatesWindowToImage((double)point.Y, (double)point.X, out double rowImage, out double columnImage);
                double num = ((ratio < 0) ? Math.Sqrt(2.0) : (1.0 / Math.Sqrt(2.0)));
                num = 1.0 / num;
                for (int num2 = Math.Abs(ratio) / 120; num2 > 1; num2--)
                {
                    num *= ((ratio < 0) ? Math.Sqrt(2.0) : (1.0 / Math.Sqrt(2.0)));
                }
                HOperatorSet.HomMat2dScale(homMat2DIdentity, num, num, columnImage, rowImage, out var homMat2DScale);
                GetFloatPart(hWindow, out var l, out var c, out var l2, out var c2);
                HOperatorSet.AffineTransPoint2d(homMat2DScale, c, l, out var qx, out var qy);
                HOperatorSet.AffineTransPoint2d(homMat2DScale, c2, l2, out var qx2, out var qy2);

                try
                {
                    hWindow.SetPart(qy.D, qx.D, qy2.D, qx2.D);
                }
                catch (Exception)
                {
                    hWindow.SetPart(l, c, l2, c2);
                }
                //CrossLine();
                if (EWinldowShowChanged != null)
                {
                    EWinldowShowChanged();
                }
            }
            catch (HalconException he)
            {

            }

        }
        /// <summary>
        /// numeric format conversion
        /// </summary>
        /// <param name="window"></param>
        /// <param name="l1"></param>
        /// <param name="c1"></param>
        /// <param name="l2"></param>
        /// <param name="c2"></param>
        private void GetFloatPart(HWindow window, out double l1, out double c1, out double l2, out double c2)
        {
            window.GetPart(out HTuple row, out HTuple column, out HTuple row2, out HTuple column2);
            l1 = row;
            c1 = column;
            l2 = row2;
            c2 = column2;
        }
        /// <summary>
        /// set mouse cursor image
        /// </summary>
        /// <param name="cursor">image</param>
        private void SetCursor(Bitmap cursor)
        {
            Bitmap myNewCursor = new Bitmap(cursor.Width * 2, cursor.Height * 2);
            Graphics graphics = Graphics.FromImage(myNewCursor);
            graphics.Clear(Color.FromArgb(0, 0, 0, 0));
            graphics.DrawImage(cursor, cursor.Width, cursor.Height, cursor.Width, cursor.Height);

            if (manualCursor == null)
                hSmartWindowControl1.Cursor = new Cursor(myNewCursor.GetHicon());

            graphics.Dispose();
            myNewCursor.Dispose();
        }
        /// <summary>
        /// set mouse cursor image
        /// </summary>
        /// <param name="cursor"></param>
        /// <param name="hotPoint"></param>
        private void SetCursor(Bitmap cursor, Point hotPoint)
        {
            int hotX = hotPoint.X;
            int hotY = hotPoint.Y;
            Bitmap myNewCursor = new Bitmap(cursor.Width * 2 - hotX, cursor.Height * 2 - hotY);
            Graphics graphics = Graphics.FromImage(myNewCursor);
            graphics.Clear(Color.FromArgb(0, 0, 0, 0));
            graphics.DrawImage(cursor, cursor.Width - hotX, cursor.Height - hotY, cursor.Width, cursor.Height);
            hSmartWindowControl1.Cursor = new Cursor(myNewCursor.GetHicon());

            graphics.Dispose();
            myNewCursor.Dispose();
        }

        /// <summary>
        ///  ZoomInOut image [EnableZoomInOutmust be enabled]
        /// </summary>
        /// <param name="pt">anchor point [non-image coordinate system]</param>
        private void ZoomInOutImage(Point pt)
        {
            int leftBorder = hSmartWindowControl1.Location.X;
            int rightBorder = hSmartWindowControl1.Location.X + hSmartWindowControl1.Size.Width;
            int topBorder = hSmartWindowControl1.Location.Y;
            int bottomBorder = hSmartWindowControl1.Location.Y + hSmartWindowControl1.Size.Height;

            switch (enableZoomInOut)
            {
                case 1:
                    HOperatorSet.SetSystem("int_zooming", "false");//it is best to set this parameter to false before image scaling.
                    if (pt.X > leftBorder && pt.X < rightBorder && pt.Y > topBorder && pt.Y < bottomBorder)
                        ZoomInOut(pt, -1 * zoomRatio);
                    HOperatorSet.SetSystem("int_zooming", "true");//it is best to set this parameter to false before image scaling.
                    break;
                case 2:
                    HOperatorSet.SetSystem("int_zooming", "false");//it is best to set this parameter to false before image scaling.

                    if (pt.X > leftBorder && pt.X < rightBorder && pt.Y > topBorder && pt.Y < bottomBorder)
                        ZoomInOut(pt, 1 * zoomRatio);
                    HOperatorSet.SetSystem("int_zooming", "true");//it is best to set this parameter to false before image scaling.
                    break;
            }
        }
        /// <summary>
        /// reset window operations
        /// </summary>
        private void RestWinOperator()
        {
            // magnifier
            if (flag_Magnifier)
            {
                flag_Magnifier = false;
                drawingType = RoiType.None;
                if (drawingObject != null)
                    drawingObject.ClearDrawingObject();
            }
        }

        /// <summary>
        /// circular eraser
        /// </summary>
        /// <param name="mouse"></param>
        private void EraseCircle(Point mouse)
        {
            // ClearWindow must be added
            hWindow.ClearWindow();
            ShowSourceImageAndROI();

            HObject ho_Circle = new HObject();
            HObject ho_RegionBorder = new HObject();
            //HObject ho_ImageReduced = new HObject();

            HOperatorSet.GenEmptyObj(out ho_Circle);
            HOperatorSet.GenEmptyObj(out ho_RegionBorder);
            //HOperatorSet.GenEmptyObj(out ho_ImageReduced);

            HTuple EraseColor = new HTuple();
            HTuple hv_Row = new HTuple();
            HTuple hv_Column = new HTuple();

            // draw border range
            if (_isColor)
            {
                EraseColor.Append(255);
                EraseColor.Append(255);
                EraseColor.Append(255);
            }
            else
            {
                EraseColor.Append(255);
            }

            hv_Row = mouse.Y;
            hv_Column = mouse.X;
            HOperatorSet.GenCircle(out ho_Circle, hv_Row, hv_Column, eraseSize);
            ho_RegionBorder.Dispose();
            // to display the border
            HObject BorderCir = new HObject();
            HOperatorSet.Boundary(ho_Circle, out ho_RegionBorder, "inner");

            hWindow.SetRgb(DefaultColor[0], DefaultColor[1], DefaultColor[2]);

            //hWindow.DispObj(ho_Circle);
            if (flag_Erase_MouseIsDown)
            {
                if (flag_Erase_Recovery)
                {
                    //HOperatorSet.ReduceDomain(ho_Source_bak, ho_Circle, out ho_ImageReduced);
                    //HOperatorSet.OverpaintGray(ho_Source, ho_ImageReduced);

                    // [old version]
                    //if (EraseRegion == null)
                    //    HOperatorSet.GenEmptyObj(out EraseRegion);
                    //HOperatorSet.Union2(EraseRegion, ho_Circle, out EraseRegion);

                    // [new version]
                    if (BrushEraseRegion == null)
                        HOperatorSet.GenEmptyObj(out BrushEraseRegion);
                    HOperatorSet.Difference(BrushEraseRegion, ho_Circle, out BrushEraseRegion);
                }
                else
                {
                    //HOperatorSet.OverpaintRegion(ho_Source, ho_Circle, EraseColor, "fill");

                    // [old version]
                    //if (BrushRegion == null)
                    //    HOperatorSet.GenEmptyObj(out BrushRegion);
                    //HOperatorSet.Union2(BrushRegion, ho_Circle, out BrushRegion);

                    // [new version]
                    if (BrushEraseRegion == null)
                        HOperatorSet.GenEmptyObj(out BrushEraseRegion);
                    HOperatorSet.Union2(BrushEraseRegion, ho_Circle, out BrushEraseRegion);
                }

                if (EBrushOrEraseDrawingDone != null)
                {
                    //HObject region = GetBrushRegion();
                    EBrushOrEraseDrawingDone(null);
                }
            }
            // DispObj must be placed here to display the border
            hWindow.DispObj(ho_RegionBorder);
            ho_Circle.Dispose();
            ho_RegionBorder.Dispose();
            //ho_ImageReduced.Dispose();
        }
        /// <summary>
        /// rectangular eraser
        /// </summary>
        /// <param name="mouse"></param>
        private void EraseRectangle(Point mouse)
        {
            // ClearWindow must be added
            hWindow.ClearWindow();
            ShowSourceImageAndROI();
            HObject ho_Rect = new HObject();
            HObject ho_RegionBorder = new HObject();
            //HObject ho_ImageReduced = new HObject();

            HOperatorSet.GenEmptyObj(out ho_Rect);
            HOperatorSet.GenEmptyObj(out ho_RegionBorder);
            //HOperatorSet.GenEmptyObj(out ho_ImageReduced);

            HTuple EraseColor = new HTuple();
            HTuple hv_Row1 = new HTuple();
            HTuple hv_Column1 = new HTuple();
            HTuple hv_Row2 = new HTuple();
            HTuple hv_Column2 = new HTuple();
            // draw border range
            if (_isColor)
            {
                EraseColor.Append(255);
                EraseColor.Append(255);
                EraseColor.Append(255);
            }
            else
            {
                EraseColor.Append(255);
            }

            hv_Row1 = mouse.Y - (eraseSize / 2);
            hv_Column1 = mouse.X - (eraseSize / 2);
            hv_Row2 = hv_Row1 + eraseSize;
            hv_Column2 = hv_Column1 + eraseSize;

            HOperatorSet.GenRectangle1(out ho_Rect, hv_Row1, hv_Column1, hv_Row2, hv_Column2);
            ho_RegionBorder.Dispose();
            // for better appearance
            HObject BorderRect = new HObject();
            //HOperatorSet.DilationRectangle1(ho_Rect, out BorderRect, 5, 5);
            // to display the border
            HOperatorSet.Boundary(ho_Rect, out ho_RegionBorder, "inner");

            hWindow.SetRgb(DefaultColor[0], DefaultColor[1], DefaultColor[2]);

            if (flag_Erase_MouseIsDown)
            {
                if (flag_Erase_Recovery)
                {
                    //HOperatorSet.ReduceDomain(ho_Source_bak, ho_Rect, out ho_ImageReduced);
                    //HOperatorSet.OverpaintGray(ho_Source, ho_ImageReduced);

                    // [old version]
                    //if (EraseRegion == null)
                    //    HOperatorSet.GenEmptyObj(out EraseRegion);
                    //HOperatorSet.Union2(EraseRegion, ho_Rect, out EraseRegion);

                    if (BrushEraseRegion == null)
                        HOperatorSet.GenEmptyObj(out BrushEraseRegion);
                    HOperatorSet.Difference(BrushEraseRegion, ho_Rect, out BrushEraseRegion);
                }
                else
                {
                    //HOperatorSet.OverpaintRegion(ho_Source, ho_Rect, EraseColor, "fill");

                    // [old version]
                    //if (BrushRegion == null)
                    //    HOperatorSet.GenEmptyObj(out BrushRegion);
                    //HOperatorSet.Union2(BrushRegion, ho_Rect, out BrushRegion);

                    // [new version]
                    if (BrushEraseRegion == null)
                        HOperatorSet.GenEmptyObj(out BrushEraseRegion);
                    HOperatorSet.Union2(BrushEraseRegion, ho_Rect, out BrushEraseRegion);
                }
                if (EBrushOrEraseDrawingDone != null)
                {
                    //HObject region = GetBrushRegion();
                    EBrushOrEraseDrawingDone(null);
                }
                //hWindow.DispObj(ho_RegionBorder);
            }
            // DispObj must be placed here to display the border
            hWindow.DispObj(ho_RegionBorder);
            ho_Rect.Dispose();
            ho_RegionBorder.Dispose();
            //ho_ImageReduced.Dispose();
        }

        /// <summary>
        /// display image
        /// </summary>
        private void DispSoureImage()
        {
            if (ho_Source == null)
                return;

            if (!ho_Source.IsInitialized())
                return;

            //if (flag_viewImage)
            //{
            //    CrossLine();
            //    return;
            //}

            // must be written this way; otherwise memory cannot be released
            //HImage hImage;
            //if (_isColor)
            //    hImage = HObject2HImage3(ho_Source);
            //else
            //    hImage = HObject2HImage1(ho_Source);

            //hWindow.AttachBackgroundToWindow(hImage);

            //hImage.Dispose();

            HImage hImage;
            if (_isColor)
                hImage = HObject2HImage3(ho_Source);
            else
                hImage = HObject2HImage1(ho_Source);

            ho_img_bak = hImage;
            flag_showBaseImage = true;
            hWindow.AttachBackgroundToWindow(hImage);

            CrossLine();
        }

        /// <summary>
        /// display image()
        /// </summary>
        private void DispBaseHImage()
        {
            if (ho_img_bak == null)
                return;

            if (!ho_img_bak.IsInitialized())
                return;

            if (flag_showBaseImage)
                hWindow.AttachBackgroundToWindow(ho_img_bak);
            else
                hWindow.DetachBackgroundFromWindow();
            CrossLine();
        }
        private void ClearSoureImage()
        {
            hWindow.DetachBackgroundFromWindow();
            //flag_viewImage = false;
        }
        /// <summary>
        /// center line
        /// </summary>
        private void CrossLine()
        {
            //if (enableCeterLine)
            //{
            //    //btn_CrossH.Location = new Point(0, 0);
            //    //btn_CrossV.Location = new Point(0, 0);

            //    //int btn_CrossH_OffsetX = (hSmartWindowControl1.Width - btn_CrossH.Width) / 2;
            //    //int PanelY = pl_hsmart.Height / 2;

            //    //int PanelX = pl_hsmart.Width / 2;
            //    //int btn_CrossV_OffsetY = (hSmartWindowControl1.Height - btn_CrossV.Height) / 2;

            //    //btn_CrossH.Location = new Point(btn_CrossH_OffsetX, PanelY);
            //    //btn_CrossV.Location = new Point(PanelX, btn_CrossV_OffsetY);

            //    //btn_CrossH.Visible = true;
            //    //btn_CrossV.Visible=true;             
            //}
            //else
            //{
            //    //btn_CrossH.Visible = false;
            //    //btn_CrossV.Visible = false;
            //}


            if (enableCeterLine)
            {
                hWindow.ClearWindow();
                HTuple Cx = image_W / 2;
                HTuple Cy = image_H / 2;

                double CrossSize = image_W > image_H ? image_W : image_H;
                CrossSize = CrossSize * 0.1;

                hWindow.SetLineWidth(2);
                hWindow.SetColor("magenta");
                hWindow.DispCross(Cy, Cx, CrossSize, 0);
            }
            else
            {
                hWindow.ClearWindow();
            }
        }
        #endregion

        #region ROI
        /// <summary>
        /// initialize drawing
        /// </summary>
        /// <param name="type"></param>
        /// <param name="_color"></param>
        private void InitialDrawing(RoiType type, int[] _color = null)
        {
            if (_color != null)
            {
                if (_color.Length >= 3)
                {
                    curRoiColor = new int[4];
                    curRoiColor[0] = _color[0];
                    curRoiColor[1] = _color[1];
                    curRoiColor[2] = _color[2];
                    if (_color.Length == 3)
                        curRoiColor[3] = DefaultColor[3];
                    else
                        curRoiColor[3] = _color[3];
                }
            }
            else
            {
                curRoiColor = new int[4];
                curRoiColor[0] = DefaultColor[0];
                curRoiColor[1] = DefaultColor[1];
                curRoiColor[2] = DefaultColor[2];
                curRoiColor[3] = DefaultColor[3];
            }

            // set color
            if (curRoiColor == null)
                curRoiColor = DefaultColor;
            hWindow.SetRgba(curRoiColor[0], curRoiColor[1], curRoiColor[2], curRoiColor[3]);
            drawingObject = new HDrawingObject();
            drawingType = type;
        }

        /// <summary>
        /// reset the draw-ROI flag
        /// </summary>
        private void RestDrawingFlag()
        {
            if (!flag_Magnifier)
                hSmartWindowControl1.Cursor = Cursors.Default;

            if (!flag_PasteROI)
            {
                flag_DrawingROI_NextPosIsClick = false;
                flag_DrawingROI_FirstMouseDown = true;
            }

            // set color
            if (curRoiColor == null)
                curRoiColor = DefaultColor;
            hWindow.SetRgba(curRoiColor[0], curRoiColor[1], curRoiColor[2], curRoiColor[3]);

            drawingObject = new HDrawingObject();
        }

        /// <summary>
        ///  ROI setup completed
        /// </summary>
        /// <param name="parm">ROI parameters</param>
        private void ConfirmNewROI(ROIParm parm = null)
        {
            if (parm != null)
            {
                eRoiList.AddROI(parm);
            }
            else
            {
                if (!flag_measure)
                    AddRoIParms(drawingType);
            }

            ShowDrawingROI(eRoiList);
            drawingType = RoiType.None;
            InitialFlag();

            if (EROI_Finish != null && eRoiList.Count != 0)
                EROI_Finish(eRoiList[eRoiList.Count - 1]);
        }


        /// <summary>
        /// update edited ROI settings
        /// </summary>
        private void ConfirmEditROI()
        {
            LastEditeDrawingRebion = null;
            UpdateEditRoIParms();

            //Console.WriteLine("ConfirmEditROI-EROI_Finish");
            if (EROI_Finish != null && editROIParm != null)
                EROI_Finish(editROIParm);

            editROIParm = null;
            if (eRoiList.Count > 0)
                ShowDrawingROI(eRoiList);

            drawingType = RoiType.None;
            InitialFlag();
        }


        /// <summary>
        /// update edited ROI parameters
        /// </summary>
        private void UpdateEditRoIParms()
        {
            ROIParm Find = eRoiList.Find_FirstOrDefault(editROIParm);

            HTuple parmName, parmValue, parmValue2;
            HObject hRegion = new HObject();
            HOperatorSet.GenEmptyObj(out hRegion);
            switch (drawingType)
            {
                case RoiType.Rectangle:
                    parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
                    parmValue = drawingObject.GetDrawingObjectParams(parmName);
                    HOperatorSet.GenRectangle1(out hRegion, parmValue[0], parmValue[1], parmValue[2], parmValue[3]);
                    break;
                case RoiType.Line:
                    parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
                    parmValue = drawingObject.GetDrawingObjectParams(parmName);
                    HOperatorSet.GenRegionLine(out hRegion, parmValue[0], parmValue[1], parmValue[2], parmValue[3]);
                    break;
                case RoiType.Circle:
                    parmName = new HTuple(new string[] { "row", "column", "radius", "row" });
                    parmValue = drawingObject.GetDrawingObjectParams(parmName);
                    HOperatorSet.GenCircle(out hRegion, parmValue[0], parmValue[1], parmValue[2]);
                    break;
                case RoiType.Cursor:
                    return;
                case RoiType.Polygon:
                    parmName = new HTuple(new string[] { "row" });
                    parmValue = drawingObject.GetDrawingObjectParams(parmName);
                    parmName = new HTuple(new string[] { "column" });
                    parmValue2 = drawingObject.GetDrawingObjectParams(parmName);
                    HOperatorSet.GenRegionPolygonFilled(out hRegion, parmValue, parmValue2);
                    break;
                case RoiType.Ring:
                    return;
                case RoiType.None:
                    return;
                default:
                    return;
            }
            Find.Region = hRegion;
            //RegionToCorrdinate(parmValue, Find._type, ref Find);
        }

        /// <summary>
        /// update copied ROI settings
        /// </summary>
        private void ConfirmPasteROI()
        {
            AddRoIParms(drawingType);

            if (EROI_Finish != null && eRoiList.Count != 0)
                EROI_Finish(eRoiList[eRoiList.Count - 1]);

            if (eRoiList.Count > 0)
                ShowDrawingROI(eRoiList);

            InitialFlag();

            // can paste repeatedly
            flag_CopyROI = true;
            flag_PasteROI = false;
        }

        /// <summary>
        /// add ROI list
        /// </summary>
        /// <param name="drawingType">type</param>
        private void AddRoIParms(RoiType drawingType)
        {
            ROIParm roi = new ROIParm();
            HObject hRegion = new HObject();
            //HTuple parmName, parmValue;
            //HOperatorSet.GenEmptyObj(out hRegion);
            //switch (drawingType)
            //{
            //    default:
            //        return;
            //    case RoiType.Rectangle:
            //        parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
            //        parmValue = drawingObject.GetDrawingObjectParams(parmName);
            //        HOperatorSet.GenRectangle1(out hRegion, parmValue[0], parmValue[1], parmValue[2], parmValue[3]);
            //        break;
            //    case RoiType.Line:
            //        parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
            //        parmValue = drawingObject.GetDrawingObjectParams(parmName);
            //        HOperatorSet.GenRegionLine(out hRegion, parmValue[0], parmValue[1], parmValue[2], parmValue[3]);
            //        break;
            //    case RoiType.Circle:
            //        parmName = new HTuple(new string[] { "row", "column", "radius", "row" });
            //        parmValue = drawingObject.GetDrawingObjectParams(parmName);
            //        HOperatorSet.GenCircle(out hRegion, parmValue[0], parmValue[1], parmValue[2]);
            //        break;
            //    case RoiType.Polygon:
            //        HOperatorSet.GenRegionPolygonFilled(out hRegion, Polygon_y.ToArray(), Polygon_x.ToArray());
            //        break;
            //}
            hRegion = Trans_drawingObjectToRegion();
            roi.VisibleROIText = visibleROIText;
            roi._color = (int[])curRoiColor.Clone();
            roi.Region = hRegion;
            roi._type = drawingType;
            roi.VisableROI = visibleROI;

            //RegionToCorrdinate(parmValue, roi._type, ref roi);

            eRoiList.AddROI(roi);
        }

        /// <summary>
        /// convert drawingObject to region
        /// </summary>
        /// <returns></returns>
        private HObject Trans_drawingObjectToRegion()
        {
            HTuple parmName, parmValue;
            HObject hRegion = new HObject();
            HOperatorSet.GenEmptyObj(out hRegion);
            if (!drawingObject.IsInitialized())
                return null;
            switch (drawingType)
            {
                default:
                    return null;
                case RoiType.Rectangle:
                    parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
                    parmValue = drawingObject.GetDrawingObjectParams(parmName);
                    HOperatorSet.GenRectangle1(out hRegion, parmValue[0], parmValue[1], parmValue[2], parmValue[3]);
                    break;
                case RoiType.Line:
                    parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
                    parmValue = drawingObject.GetDrawingObjectParams(parmName);
                    HOperatorSet.GenRegionLine(out hRegion, parmValue[0], parmValue[1], parmValue[2], parmValue[3]);
                    break;
                case RoiType.Circle:
                    parmName = new HTuple(new string[] { "row", "column", "radius", "row" });
                    parmValue = drawingObject.GetDrawingObjectParams(parmName);
                    HOperatorSet.GenCircle(out hRegion, parmValue[0], parmValue[1], parmValue[2]);
                    break;
                case RoiType.Polygon:
                    HOperatorSet.GenRegionPolygonFilled(out hRegion, Polygon_y.ToArray(), Polygon_x.ToArray());
                    break;
            }
            return hRegion;
        }

        /// <summary>
        /// determine how many ROIs are in the selected region
        /// </summary>
        /// <param name="region">selected region</param>
        private ERoiList GetSelectedRoiList(HObject region)
        {

            RectangleF rectangleF = new RectangleF();
            HTuple hv_Value = new HTuple();
            HOperatorSet.RegionFeatures(region, (((new HTuple("row1")).TupleConcat("column1")).TupleConcat("row2")).TupleConcat("column2"), out hv_Value);

            rectangleF.Y = (float)hv_Value.DArr[0];
            rectangleF.X = (float)hv_Value.DArr[1];
            rectangleF.Width = (float)hv_Value.DArr[3] - (float)hv_Value.DArr[1];
            rectangleF.Height = (float)hv_Value.DArr[2] - (float)hv_Value.DArr[0];

            ERoiList selected = new ERoiList();
            HObject rectObj = new HObject();

            RectangleF rectangleF2 = new RectangleF();
            for (int i = 0; i < eRoiList.Count; i++)
            {
                if (!eRoiList[i].VisableROI)
                    continue;

                rectangleF2 = new RectangleF();
                HObject regionRect = new HObject();
             
                HOperatorSet.ShapeTrans(eRoiList[i].Region,out regionRect, "rectangle1");

                HOperatorSet.RegionFeatures(regionRect, (((new HTuple("row1")).TupleConcat("column1")).TupleConcat("row2")).TupleConcat("column2"), out hv_Value);
                if (hv_Value.Length == 0)
                    continue;
                rectangleF2.Y = (float)hv_Value.DArr[0];
                rectangleF2.X = (float)hv_Value.DArr[1];
                rectangleF2.Width = (float)hv_Value.DArr[3] - (float)hv_Value.DArr[1];
                rectangleF2.Height = (float)hv_Value.DArr[2] - (float)hv_Value.DArr[0];

                if (rectangleF.Contains(rectangleF2))
                {
                    selected.AddROI(eRoiList[i], false);
                }
            }


            return selected;
        }

        private void RegionToCorrdinate(HTuple parmValue, RoiType roiType, ref ROIParm parm)
        {
            //parm.regionSize.LeftTop=new Point(0,0);
            //parm.regionSize.RightBottom = new Point(0, 0);
            //parm.regionSize.Center = new Point(0, 0);
            //switch (roiType)
            //{
            //    case RoiType.Rectangle:
            //        switch (parmValue.Type)
            //        {
            //            case HTupleType.DOUBLE:
            //                parm.regionSize.LeftTop = new Point((int)parmValue.DArr[1], (int)parmValue.DArr[0]);
            //                parm.regionSize.RightBottom = new Point((int)parmValue.DArr[3], (int)parmValue.DArr[2]);
            //                break;
            //            case HTupleType.LONG:
            //                parm.regionSize.LeftTop = new Point((int)parmValue.LArr[1], (int)parmValue.LArr[0]);
            //                parm.regionSize.RightBottom = new Point((int)parmValue.LArr[3], (int)parmValue.LArr[2]);
            //                break;
            //            case HTupleType.INTEGER:
            //                parm.regionSize.LeftTop = new Point(parmValue.IArr[1], parmValue.IArr[0]);
            //                parm.regionSize.RightBottom = new Point(parmValue.IArr[3], parmValue.IArr[2]);
            //                break;
            //        }
            //        break;
            //    case RoiType.Circle:
            //        switch (parmValue.Type)
            //        {
            //            case HTupleType.DOUBLE:
            //                parm.regionSize.Center = new Point((int)parmValue.DArr[1], (int)parmValue.DArr[0]);
            //                parm.regionSize.Radius = parmValue.DArr[3];
            //                break;
            //            case HTupleType.LONG:
            //                parm.regionSize.Center = new Point((int)parmValue.LArr[1], (int)parmValue.LArr[0]);
            //                parm.regionSize.Radius = parmValue.LArr[3];
            //                break;
            //            case HTupleType.INTEGER:
            //                parm.regionSize.Center = new Point(parmValue.IArr[1], parmValue.IArr[0]);
            //                parm.regionSize.Radius = parmValue.IArr[3];
            //                break;
            //        }
            //        break;
            //}

        }

        /// <summary>
        /// add ROI list
        /// </summary>
        /// <param name="Roiparm">parameters</param>
        private void AddRoIParms(ROIParm Roiparm)
        {

            Roiparm.VisibleROIText = visibleROIText;
            if (Roiparm._color == null)
                Roiparm._color = (int[])DefaultColor.Clone();
            //Roiparm._color = CheckColorIsSupport2(Roiparm._color);
            Roiparm._type = drawingType;
            Roiparm.VisableROI = visibleROI;

            //CheckRoiIndex();
            //Roiparm.ID = ROI_ID;
            //RoiList.Add(Roiparm);
            //ROI_ID++;
            eRoiList.AddROI(Roiparm);
        }


        /// <summary>
        /// display drawn ROI
        /// </summary>
        /// <param name="eROIs">ROI list</param>
        /// <param name="ClearDrawing">whether to clear drawingObject</param>
        private void ShowDrawingROI(ERoiList eROIs,bool ClearDrawing=true)
        {
            if (eROIs == null)
                return;
            HObject hObject;
            HTuple hv_BoxColor = new HTuple();
            hv_BoxColor.Dispose();
            hv_BoxColor = "#0000ff77";
            hWindow.ClearWindow();
            hWindow.SetWindowParam("flush","false");
            if (ClearDrawing)
            {
                if (drawingObject != null)
                    drawingObject.ClearDrawingObject();
            }

            HTuple parmName, parm;
            HTuple H_Area = new HTuple();
            HTuple H_cx = new HTuple();
            HTuple H_cy = new HTuple();

            // [for acceleration]batch drawing
            Dictionary<int[], HObject> table = new Dictionary<int[], HObject>();

            for (int i = 0; i < eROIs.Count; i++)
            {
                HOperatorSet.GenEmptyObj(out hObject);
                ROIParm ROI = eROIs[i];

                if (ROI == editROIParm)
                    continue;

                if (ROI == null)
                {
                    eROIs.RemoveList_ByIdx(i);
                    break;
                }
                RoiType drawingType = ROI._type;
                if (ROI.VisableROI == false)
                    continue;

                if (ROI._color == null)
                {
                    ROI._color = new int[3];
                    ROI._color = DefaultColor;
                }
                //ROI._color = CheckColorIsSupport2(ROI._color);
                hWindow.SetRgba(ROI._color[0], ROI._color[1], ROI._color[2], ROI._color[3]);

                switch (drawingType)
                {
                    case RoiType.Line:
                        parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
                        HOperatorSet.RegionFeatures(ROI.Region, parmName, out parm);
                        if (ROI.VisibleROIText)
                            HOperatorSet.DispText(hWindow, ROI.ID.ToString(), "image",
                                parm[2], parm[3], "white", "box_color", hv_BoxColor);
                        break;
                    case RoiType.Rectangle:
                        parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
                        HOperatorSet.RegionFeatures(ROI.Region, parmName, out parm);
                        if (ROI.VisibleROIText)
                            HOperatorSet.DispText(hWindow, ROI.ID.ToString(), "image",
                                parm[2], parm[3], "white", "box_color", hv_BoxColor);
                        break;
                    case RoiType.Circle:
                        parmName = new HTuple(new string[] { "row", "column", "radius" });
                        HOperatorSet.RegionFeatures(ROI.Region, parmName, out parm);
                        if (ROI.VisibleROIText)
                            HOperatorSet.DispText(hWindow, ROI.ID.ToString(), "image",
                                parm[0], parm[1], "white", "box_color", hv_BoxColor);
                        break;
                    case RoiType.Polygon:
                        parmName = new HTuple(new string[] { "row", "column" });

                        HOperatorSet.AreaCenter(ROI.Region, out H_Area, out H_cy, out H_cx);
                        if (ROI.VisibleROIText)
                            HOperatorSet.DispText(hWindow, ROI.ID.ToString(), "image",
                                H_cy, H_cx, "white", "box_color", hv_BoxColor);
                        break;
                    case RoiType.Region:
                        //parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
                        //HOperatorSet.RegionFeatures(ROI.Region, parmName, out parm);

                        parmName = new HTuple(new string[] { "row", "column" });
                        HOperatorSet.AreaCenter(ROI.Region, out H_Area, out H_cy, out H_cx);
                        if (ROI.VisibleROIText)
                            HOperatorSet.DispText(hWindow, ROI.ID.ToString(), "image",
                                 H_cy, H_cx, "white", "box_color", hv_BoxColor);
                        break;
                }
                hObject = ROI.Region;

                //// batch drawing
                //if (table.ContainsKey(ROI._color))
                //{
                //    HObject unionObj = new HObject();
                //    unionObj = table[ROI._color];
                //    HOperatorSet.Union2(unionObj, ROI.Region, out unionObj);
                //    table[ROI._color] = ROI.Region;
                //}
                //else
                //{
                //    table.Add(ROI._color, ROI.Region);
                //}

                // because of color constraints, only single drawing is possible; batch drawing is not possible
                if (hObject != null && hObject.IsInitialized())
                {
                    hWindow.DispObj(hObject);
                }
            }

            //// batch drawing
            //foreach (var item in table)
            //{
            //    hWindow.SetRgba(item.Key[0], item.Key[1], item.Key[2], item.Key[3]);
            //    hWindow.DispObj(item.Value);
            //}




            if (_showRegion != null && _showRegion.IsInitialized())
            {
                if (curRoiColor == null)
                    curRoiColor = DefaultColor;
                hWindow.SetRgba(curRoiColor[0], curRoiColor[1], curRoiColor[2], curRoiColor[3]);
                hWindow.DispObj(_showRegion);
            }
            if (enableCeterLine)
            {
                if (image_W != 0)
                {
                    HTuple Cx = image_W / 2;
                    HTuple Cy = image_H / 2;
                    double CrossSize = image_W > image_H ? image_W : image_H;
                    CrossSize = CrossSize * 0.1;
                    hWindow.SetLineWidth(2);
                    hWindow.SetColor("magenta");
                    hWindow.DispCross(Cy, Cx, CrossSize, 0);
                }
            }

            hWindow.FlushBuffer();
            hWindow.SetWindowParam("flush", "true");
        }

        /// <summary>
        /// display original image and ROI
        /// </summary>
        private void ShowSourceImageAndROI()
        {
            if (ho_Source == null) return;
            //hWindow.DispObj(ho_Source);
            if (eRoiList.Count >= 0)
                ShowDrawingROI(eRoiList);
            else
            {
                if (_showRegion != null && _showRegion.IsInitialized())
                {
                    hWindow.SetRgba(curRoiColor[0], curRoiColor[1], curRoiColor[2], curRoiColor[3]);
                    hWindow.DispObj(_showRegion);
                }
            }
        }
        /// <summary>
        /// check whether the clicked position has an ROI
        /// </summary>
        /// <param name="SelectPt">position</param>
        /// <returns></returns>
        private bool CheckClickPosInROI(Point SelectPt)
        {
            HTuple IsNull = new HTuple();
            HObject FindRegion = new HObject();
            HObject NullRegion = new HObject();
            HOperatorSet.GenEmptyObj(out NullRegion);
            EditeRoi_ID = -1;

            for (int i = 0; i < eRoiList.Count; i++)
            {
                if (!eRoiList[i].VisableROI)
                    continue;

                //if (eRoiList[i]._type == RoiType.Rectangle || eRoiList[i]._type == RoiType.Circle
                //    || eRoiList[i]._type == RoiType.Polygon)
                //{
                //    HOperatorSet.GenEmptyObj(out FindRegion);
                //    HOperatorSet.SelectRegionPoint(eRoiList[i].Region, out FindRegion, SelectPt.Y, SelectPt.X);
                //    HOperatorSet.TestEqualObj(NullRegion, FindRegion, out IsNull);
                //    if (IsNull.I == 0)
                //    {
                //        EditeRoi_ID = eRoiList[i].ID;
                //        return true;
                //    }
                //}
                HOperatorSet.GenEmptyObj(out FindRegion);
                HOperatorSet.SelectRegionPoint(eRoiList[i].Region, out FindRegion, SelectPt.Y, SelectPt.X);
                HOperatorSet.TestEqualObj(NullRegion, FindRegion, out IsNull);
                if (IsNull.I == 0)
                {
                    EditeRoi_ID = eRoiList[i].ID;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// edit ROI
        /// </summary>
        private void EditRoi(Point pt)
        {
            flag_EditROI = true;
            flag_CopyROI = false;
            flag_PasteROI = false;
            CopyROIParm = null;
            HTuple parmName, parmValue;
            if (EditeRoi_ID == -1) return;
            editROIParm = eRoiList.Find_FirstOrDefaultById(EditeRoi_ID);
            if (editROIParm.Lock)
                return;
            //EditROIParm._color = curRoiColor;
            // then update the screen again

            ShowDrawingROI(eRoiList);
            drawingType = editROIParm._type;
            DrawRoiRegionAndOutterRectangleBorder(editROIParm.Region, pt.X, pt.Y);
            tempRegion= editROIParm.Region;
            hWindow.DispObj(tempRegion);


            GetCurROIParms_OutterRect(editROIParm.ID, editROIParm._type, editROIParm._color, editROIParm.VisableROI, editROIParm.VisibleROIText, new Point(0,0));

            //if (editROIParm._type != RoiType.Region)
            //{
            //    ShowDrawingROI(eRoiList);

            //    // edit ROI
            //    Point NextPos = new Point(0,0);
            //    if (editROIParm._type == RoiType.Rectangle)
            //    {
            //        parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
            //        HOperatorSet.RegionFeatures(editROIParm.Region, parmName, out parmValue);
            //        InitialDrawing(editROIParm._type, editROIParm._color);
            //        DrawingROI_First_x1 = parmValue[1];
            //        DrawingROI_First_y1 = parmValue[0];
            //        NextPos = new Point((int)parmValue[3].D, (int)parmValue[2].D);
            //        DrawingROIRegion_Edite(NextPos);
            //    }


            //    if (editROIParm._type == RoiType.Circle)
            //    {
            //        parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
            //        HOperatorSet.RegionFeatures(editROIParm.Region, parmName, out parmValue);
            //        InitialDrawing(editROIParm._type, editROIParm._color);
            //        DrawingROI_First_x1 = parmValue[1];
            //        DrawingROI_First_y1 = parmValue[0];
            //        NextPos = new Point((int)parmValue[3].D, (int)parmValue[2].D);
            //        DrawingROIRegion_Edite(NextPos);

            //        //parmName = new HTuple(new string[] { "row", "column", "radius" });
            //        //HOperatorSet.RegionFeatures(editROIParm.Region, parmName, out parmValue);
            //        //HOperatorSet.GenCircle(out EditeROI_Region, parmValue[0], parmValue[1], parmValue[2]);
            //        //DrawingROI_First_x1 = parmValue[1].D;
            //        //DrawingROI_First_y1 = parmValue[0].D;
            //        //NextPos = new Point((int)DrawingROI_First_x1 + (int)parmValue[2].D, (int)DrawingROI_First_y1 + (int)parmValue[2].D);
            //        //DrawingROIRegion(NextPos);
            //    }

            //    if (editROIParm._type == RoiType.Polygon)
            //    {
            //        HTuple parmValue2;
            //        HOperatorSet.GetRegionPolygon(editROIParm.Region, 1, out parmValue, out parmValue2);
            //        drawingObject.CreateDrawingObjectXld(parmValue, parmValue2);

            //        if (drawingObject.ID != 0)
            //        {
            //            //associate the drawing object with the Halcon window
            //            hWindow.AttachDrawingObjectToWindow(drawingObject);
            //        }
            //    }

            //    if (EFirstClickEditeROI != null)
            //        EFirstClickEditeROI(editROIParm);
            //}
            //else
            //{
            //    flag_EditROI = true;
            //    TempROIParm = CloneRoiParm(editROIParm);

            //    //if (editROIParm._color[3]==255)
            //    //    TempROIParm._color[3] = 20;
            //    //else
            //    //    TempROIParm._color[3] = 255;
            //    //int ChangColor=255- editROIParm._color[3];
            //    //ChangColor += 50;
            //    //if (editROIParm._color[3] >= 125)
            //    //    TempROIParm._color[3] =100;
            //    //else
            //    //    TempROIParm._color[3] = 255;
            //    TempROIParm._color[3] = 255;

            //    editROIParm.VisableROI = false;
            //    eRoiList.UpdateROI(editROIParm);
            //    AddRoi(TempROIParm);
            //    if (ERegion_Edite != null)
            //        ERegion_Edite(TempROIParm);
            //}


        }


        /// <summary>
        /// edit ROI
        /// </summary>
        private void LockEditeRoiScale(Point pt)
        {
            flag_EditROI = true;
            HTuple parmName, parmValue;
            if (EditeRoi_ID == -1) return;
            editROIParm = eRoiList.Find_FirstOrDefaultById(EditeRoi_ID);
            if (editROIParm.Lock)
                return;

            ShowDrawingROI(eRoiList);
            drawingType = editROIParm._type;
            DrawRoiRegionAndOutterRectangleBorder(editROIParm.Region, pt.X, pt.Y);
            tempRegion = editROIParm.Region;
            hWindow.DispObj(tempRegion);
           
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="drawid"></param>
        /// <param name="window"></param>
        /// <param name="type"></param>
        public void HDrawingObject_OnResizeCallback(HDrawingObject drawid, HWindow window, string type)
        {
            HTuple parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2"});
            HTuple parmValue = new HTuple();
            HObject drawingOutterRect = new HObject();
            ROIParm TempROI = new ROIParm();
            parmValue = drawid.GetDrawingObjectParams(parmName);

            HTuple CurBorderSize = new HTuple();
            HTuple zoomFactor=new HTuple();

            switch (type) 
            {
                default:
                    return;
                case "on_attach":
                    break;
                case "on_detach":

                    break;
                case "on_resize":
                    if (editROIParm != null)
                    {
                        HTuple ParmValueR = new HTuple();
                        HTuple h_cx = new HTuple();
                        HTuple h_cy = new HTuple();
                        HTuple h_cx2 = new HTuple();
                        HTuple h_cy2 = new HTuple();
                        HTuple h_area = new HTuple();

                        HTuple h_ccx = new HTuple();
                        HTuple h_ccy = new HTuple();


                        if (lockRoiScale)
                        {                       
                            double new_RoiW = parmValue[3] - parmValue[1];
                            double new_RoiH = parmValue[2] - parmValue[0];
                            double Rx = Fix_RoiW - new_RoiW;
                            double Ry = Fix_RoiH - new_RoiH;

                            if (Math.Abs(Rx) > Math.Abs(Ry))
                            {
                                Fix_RoiH -= Rx;
                                Fix_RoiW -= Rx;
                            }
                            else
                            {
                                Fix_RoiH -= Ry;
                                Fix_RoiW -= Ry;
                            }
                            parmValue[2] = parmValue[0] + Fix_RoiH;
                            parmValue[3] = parmValue[1] + Fix_RoiW;
                            if (parmValue[2] <= parmValue[0])
                                return;
                            if (parmValue[3] <= parmValue[1])
                                return;
                            drawid.SetDrawingObjectParams(parmName, parmValue);

                            HOperatorSet.GetDrawingObjectIconic(out drawingOutterRect, drawid);
                            parmName = new HTuple(new string[] { "circularity", "rectangularity" });
                            HTuple hv_value = new HTuple();
                            HOperatorSet.RegionFeatures(tempRegion, parmName, out hv_value);

                            if (hv_value[0] > hv_value[1] && hv_value[0] > 0.9)
                            {
                                HOperatorSet.ShapeTrans(drawingOutterRect, out tempRegion, "inner_circle");
                            }
                            else
                            {
                                tempRegion = drawingOutterRect;
                            }
                            //ShowDrawingROI(eRoiList, false);
                            hWindow.ClearWindow();
                            hWindow.DispObj(tempRegion);
                            if (EFixScaleROI != null)
                                EFixScaleROI(parmValue, type);
                            return;
                        }

                        CurBorderSize.Append(parmValue[3] - parmValue[1]);
                        CurBorderSize.Append(parmValue[2] - parmValue[0]);

                        ParmValueR = LastParmValue - parmValue;

                        if ((parmValue[2] - parmValue[0]) <= 0)
                        {
                            return;
                        }
          

                        if ((parmValue[3] - parmValue[1]) <= 0)
                        {
                            return;
                        }

                        HTuple h_countour_x = new HTuple();
                        HTuple h_countour_y = new HTuple();

                        if (tempRegion != null && tempRegion.IsInitialized())
                        {
                            HOperatorSet.AreaCenter(tempRegion, out h_area, out h_cy, out h_cx);
                            HOperatorSet.GetRegionContour(tempRegion, out h_countour_y, out h_countour_x);
                        }
                        else
                        {
                            HOperatorSet.AreaCenter(editROIParm.Region, out h_area, out h_cy, out h_cx);
                            HOperatorSet.GetRegionContour(editROIParm.Region, out h_countour_y, out h_countour_x);
                        }

                        #region [fast and smooth]
                        HTuple hv_HomMat2D = new HTuple();
                        hv_HomMat2D.Dispose();

                        HOperatorSet.TupleReal(LastParmValue, out LastParmValue);
                        HOperatorSet.TupleReal(parmValue, out parmValue);

                        HTuple initialX = new HTuple();
                        HTuple initialY = new HTuple();

                        initialX.Append(LastParmValue[1]);  //upper left
                        initialX.Append(LastParmValue[1]);  //lower left
                        initialX.Append(LastParmValue[3]);  //lower right
                        initialX.Append(LastParmValue[3]);  //upper right

                        initialY.Append(LastParmValue[0]);  //upper left
                        initialY.Append(LastParmValue[2]);  //lower left
                        initialY.Append(LastParmValue[2]);  //lower right
                        initialY.Append(LastParmValue[0]);  //upper right

                        HTuple NewX = new HTuple();
                        HTuple NewY = new HTuple();

                        NewX.Append(parmValue[1]);  //upper left
                        NewX.Append(parmValue[1]);  //lower left
                        NewX.Append(parmValue[3]);  //lower right
                        NewX.Append(parmValue[3]);  //upper right

                        NewY.Append(parmValue[0]);  //upper left
                        NewY.Append(parmValue[2]);  //lower left
                        NewY.Append(parmValue[2]);  //lower right
                        NewY.Append(parmValue[0]);  //upper right

                        // VectorToHomMat2d requires at least 3 points, so use the four bounding-box points
                        HOperatorSet.VectorToHomMat2d(initialY, initialX, NewY, NewX, out hv_HomMat2D);
                        HOperatorSet.AffineTransRegion(tempRegion, out TranslateTempRegion, hv_HomMat2D, "nearest_neighbor");
                        #endregion

                        if (TranslateTempRegion != null && TranslateTempRegion.IsInitialized())
                        {
                            hWindow.ClearWindow();
                            hWindow.DispObj(TranslateTempRegion);
                        }

                        drawid.SetDrawingObjectParams(parmName, parmValue);

                        TempROI.Region = TranslateTempRegion;
                        TempROI.ID = editROIParm.ID;
                        TempROI._type = editROIParm._type;
                        TempROI._color = editROIParm._color;
                        TempROI.VisableROI = editROIParm.VisableROI;
                        TempROI.VisibleROIText = editROIParm.VisibleROIText;
                        TempROI.RegionBorder = GenerateRegionBorder(TranslateTempRegion);
                        if (EAllowEditROI != null)
                            EAllowEditROI(TempROI);
                    }                         
                    break;
                case "on_drag":
                    if (editROIParm != null)
                    {
                        if (TranslateTempRegion != null && TranslateTempRegion.IsInitialized())
                        {
                            tempRegion = TranslateTempRegion;
                        }
                        if (lockRoiScale)
                        {
                            parmName = new HTuple(new string[] { "circularity", "rectangularity" });
                            HTuple hv_value = new HTuple();
                            HOperatorSet.RegionFeatures(tempRegion, parmName, out hv_value);
                            HOperatorSet.GetDrawingObjectIconic(out drawingOutterRect, drawid);

                            if (hv_value[0] > hv_value[1] && hv_value[0] > 0.9)
                            {
                                HOperatorSet.ShapeTrans(drawingOutterRect, out tempRegion, "inner_circle");
                            }
                            else
                            {
                                tempRegion = drawingOutterRect;
                            }
                            ShowDrawingROI(eRoiList, false);
                            hWindow.DispObj(tempRegion);

                            if (EFixScaleROI != null)
                                EFixScaleROI(parmValue, type);
                            return;
                        }

                        HObject region = new HObject();

                        HTuple h_cx = new HTuple();
                        HTuple h_cy = new HTuple();
                        HTuple h_cx2 = new HTuple();
                        HTuple h_cy2 = new HTuple();
                        HTuple h_area = new HTuple();
                
                        
                        //HOperatorSet.GetDrawingObjectIconic(out region, drawid);
                        if (tempRegion!=null && tempRegion.IsInitialized())
                            HOperatorSet.AreaCenter(tempRegion, out h_area, out h_cy, out h_cx);
                        else
                        {
                            HOperatorSet.AreaCenter(editROIParm.Region, out h_area, out h_cy, out h_cx);
                            tempRegion = editROIParm.Region;
                        }
            

                        h_cx2 = (parmValue[1] + parmValue[3]) / 2;
                        h_cy2 = (parmValue[0] + parmValue[2]) / 2;

                        HTuple h_Movedcx2 = new HTuple();
                        HTuple h_Movedcy2 = new HTuple();
                        h_Movedcx2 = h_cx2 - h_cx;
                        h_Movedcy2 = h_cy2 - h_cy;


                        HOperatorSet.MoveRegion(tempRegion, out tempRegion, h_Movedcy2, h_Movedcx2);

                        LastParmValue = parmValue;
                        ShowDrawingROI(eRoiList, false);
                        hWindow.DispObj(tempRegion);

                        TempROI.Region = tempRegion;
                        TempROI.ID = editROIParm.ID;
                        TempROI._type = editROIParm._type;
                        TempROI._color = editROIParm._color;
                        TempROI.VisableROI = editROIParm.VisableROI;
                        TempROI.VisibleROIText = editROIParm.VisibleROIText;
                        TempROI.RegionBorder = GenerateRegionBorder(tempRegion);
                        if (EAllowEditROI != null)
                            EAllowEditROI(TempROI);
                    }                    
                    break;
            }

            //if (EFixScaleROI != null)
            //    EFixScaleROI(parmValue, type);  
        }

        private ROIParm CloneEditeRoiParm(ROIParm parm)
        {
            ROIParm NewParm = new ROIParm();
            NewParm.Region = editROIParm.Region;
            NewParm.VisibleROIText = editROIParm.VisibleROIText;
            NewParm.VisableROI = editROIParm.VisableROI;
            NewParm._type = RoiType.Region;
            NewParm.ID = editROIParm.ID;
            NewParm.Lock = editROIParm.Lock;
            NewParm._color = new int[editROIParm._color.Length];
            for (int i = 0; i < editROIParm._color.Length; i++)
            {
                NewParm._color[i] = editROIParm._color[i];
            }
            return NewParm;
        }


        private ROIParm CloneRoiParm(ROIParm parm)
        {
            ROIParm NewParm = new ROIParm();
            NewParm.Region = editROIParm.Region;
            NewParm.VisibleROIText = editROIParm.VisibleROIText;
            NewParm.VisableROI = editROIParm.VisableROI;
            NewParm._type = editROIParm._type;
            NewParm.ID = editROIParm.ID;
            NewParm.Lock = editROIParm.Lock;
            NewParm._color = new int[editROIParm._color.Length];
            for (int i = 0; i < editROIParm._color.Length; i++)
            {
                NewParm._color[i] = editROIParm._color[i];
            }
            return NewParm;
        }
        /// <summary>
        /// check whether the configured color is supported; use red if unsupported
        /// </summary>
        /// <param name="_color">configured color</param>
        /// <returns></returns>
        private string CheckColorIsSupport(int[] _color)
        {
            Color _color2 = Color.FromArgb(_color[0], _color[1], _color[2]);
            string HexColor = System.Drawing.ColorTranslator.ToHtml(_color2);
            string FindColor = HColor.FirstOrDefault(x => x == HexColor.ToLower());
            FindColor = HColor.FirstOrDefault(x => x == _color2.Name);
            if (FindColor == null)
                FindColor = "#ff0000";

            return FindColor;
        }
        
        /// <summary>
        /// draw an editable ROI
        /// </summary>
        /// <param name="point">second point position</param>
        private void DrawingROIRegion(Point point,double CircleRadius=0)
        {
            switch (drawingType)
            {
                case RoiType.Line:
                    if ((point.Y - DrawingROI_First_y1) < 1 && (point.X - DrawingROI_First_x1) < 1)
                    {
                        // do not draw if the value is too small
                        return;
                    }
                    drawingObject.CreateDrawingObjectLine(point.Y, point.X, DrawingROI_First_y1, DrawingROI_First_x1);
     
                    break;
                case RoiType.Rectangle:
                    if ((point.Y - DrawingROI_First_y1) > 2 && (point.X - DrawingROI_First_x1) > 2)
                    {
                        drawingObject.CreateDrawingObjectRectangle1(DrawingROI_First_y1, DrawingROI_First_x1, point.Y, point.X);                      
                    }
                    break;
                case RoiType.Circle:

                    if ((point.Y - DrawingROI_First_y1) > 2 && (point.X - DrawingROI_First_x1) > 2)
                    {
                        drawingObject.CreateDrawingObjectRectangle1(DrawingROI_First_y1, DrawingROI_First_x1, point.Y, point.X);
                    }

                    //if (Math.Abs(point.Y - DrawingROI_First_y1) > 2)
                    //{
                    //    //drawingObject.CreateDrawingObjectCircle(DrawingROI_First_y1, DrawingROI_First_x1, Math.Abs(point.Y - DrawingROI_First_y1));

                    //    if (CircleRadius != 0)
                    //        drawingObject.CreateDrawingObjectCircle(DrawingROI_First_y1, DrawingROI_First_x1, CircleRadius);
                    //    else
                    //        drawingObject.CreateDrawingObjectCircle(DrawingROI_First_y1, DrawingROI_First_x1, Math.Abs(point.Y - DrawingROI_First_y1));
                    //}
                    break;
                case RoiType.Region:
                    
                    break;
                case RoiType.Polygon:
                    HTuple distance = new HTuple();
                    double x1 = Polygon_x[Polygon_x.Count - 1];
                    double y1 = Polygon_y[Polygon_y.Count - 1];

                    double x2 = point.X;
                    double y2 = point.Y;
                    HOperatorSet.DistancePp(y1, x1, y2, x2, out distance);

                    if (distance.D > 50)
                    {
                        // to avoid making the small boxes too dense
                        Polygon_x.Add(point.X);
                        Polygon_y.Add(point.Y);
                        drawingObject.CreateDrawingObjectXld(Polygon_y.ToArray(), Polygon_x.ToArray());
                    }
                    else
                    {
                        return;
                    }
                    break;
                default:
                    return;
            }
            if (drawingObject.ID != 0)
            {
                //associate the drawing object with the Halcon window

                // color can only be set here because the region must exist first
                if (flag_SelectedROI)
                    drawingObject.SetDrawingObjectParams("color", "blue");
                else
                    drawingObject.SetDrawingObjectParams("color", "red");

                hWindow.AttachDrawingObjectToWindow(drawingObject);
            }
        }

        /// <summary>
        /// [version 2]draw an editable ROI
        /// </summary>
        /// <param name="point_LT">upper left</param>
        /// <param name="point_RB">lower right</param>
        private void DrawingROIRegion2(Point point_LT, Point point_RB)
        {
            drawingObject.CreateDrawingObjectRectangle1(point_LT.Y, point_LT.X, point_RB.Y, point_RB.X);
  

            if (drawingObject.ID != 0)
            {
                HTuple parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2"});

                LastParmValue = drawingObject.GetDrawingObjectParams(parmName);

                if (lockRoiScale)
                {
                    Fix_RoiW = LastParmValue[3] - LastParmValue[1];
                    Fix_RoiH= LastParmValue[2] - LastParmValue[0];
                }

                TranslateTempRegion = new HObject();
                //associate the drawing object with the Halcon window
                // color can only be set here because the region must exist first
                drawingObject.SetDrawingObjectParams("color", "blue");
                drawingObject.OnResize(HDrawingObject_OnResizeCallback);
                drawingObject.OnDrag(HDrawingObject_OnResizeCallback);
                drawingObject.OnAttach(HDrawingObject_OnResizeCallback);
                drawingObject.OnDetach(HDrawingObject_OnResizeCallback);
                //hWindow.SetShape("convex");
                hWindow.AttachDrawingObjectToWindow(drawingObject);
            }
        }
       
        /// <summary>
        /// draw an editable ROI
        /// </summary>
        /// <param name="point">second point position</param>
        private void DrawingROIRegion_Edite(Point point, double CircleRadius = 0)
        {
            switch (drawingType)
            {
                case RoiType.Line:
                    drawingObject.CreateDrawingObjectLine(point.Y, point.X, DrawingROI_First_y1, DrawingROI_First_x1);
                    break;
                case RoiType.Rectangle:
                    if ((point.Y - DrawingROI_First_y1) > 2 && (point.X - DrawingROI_First_x1) > 2)
                    {
                        drawingObject.CreateDrawingObjectRectangle1(DrawingROI_First_y1, DrawingROI_First_x1, point.Y, point.X);
                    }
                    break;
                case RoiType.Circle:

                    if ((point.Y - DrawingROI_First_y1) > 2 && (point.X - DrawingROI_First_x1) > 2)
                    {
                        drawingObject.CreateDrawingObjectRectangle1(DrawingROI_First_y1, DrawingROI_First_x1, point.Y, point.X);
                    }

                    //if (Math.Abs(point.Y - DrawingROI_First_y1) > 2)
                    //{
                    //    //drawingObject.CreateDrawingObjectCircle(DrawingROI_First_y1, DrawingROI_First_x1, Math.Abs(point.Y - DrawingROI_First_y1));

                    //    if (CircleRadius != 0)
                    //        drawingObject.CreateDrawingObjectCircle(DrawingROI_First_y1, DrawingROI_First_x1, CircleRadius);
                    //    else
                    //        drawingObject.CreateDrawingObjectCircle(DrawingROI_First_y1, DrawingROI_First_x1, Math.Abs(point.Y - DrawingROI_First_y1));
                    //}
                    break;
                case RoiType.Region:

                    break;
                case RoiType.Polygon:
                    HTuple distance = new HTuple();
                    double x1 = Polygon_x[Polygon_x.Count - 1];
                    double y1 = Polygon_y[Polygon_y.Count - 1];

                    double x2 = point.X;
                    double y2 = point.Y;
                    HOperatorSet.DistancePp(y1, x1, y2, x2, out distance);

                    if (distance.D > 50)
                    {
                        // to avoid making the small boxes too dense
                        Polygon_x.Add(point.X);
                        Polygon_y.Add(point.Y);
                        drawingObject.CreateDrawingObjectXld(Polygon_y.ToArray(), Polygon_x.ToArray());
                    }
                    else
                    {
                        return;
                    }
                    break;
                default:
                    return;
            }
            if (drawingObject.ID != 0)
            {
                //associate the drawing object with the Halcon window

                // color can only be set here because the region must exist first
                if (flag_SelectedROI)
                    drawingObject.SetDrawingObjectParams("color", "blue");
                else
                    drawingObject.SetDrawingObjectParams("color", "red");

                hWindow.AttachDrawingObjectToWindow(drawingObject);
            }
        }

        /// <summary>
        /// magic wand [irregular shape]
        /// </summary>
        /// <param name="PosX"></param>
        /// <param name="PosY"></param>
        /// <returns></returns>
        private HObject MagicWand(int PosX, int PosY)
        {
            HObject GrayHobj = new HObject();
            HObject Region = new HObject();
            HObject ConnectRegion = new HObject();
            HObject RegionInMouse = new HObject();
            HTuple IsNull = new HTuple();
            HObject FindRegion = new HObject();
            HObject NullRegion = new HObject();
            HOperatorSet.GenEmptyObj(out NullRegion);


            HOperatorSet.GenEmptyObj(out GrayHobj);
            HOperatorSet.GenEmptyObj(out Region);
            HOperatorSet.GenEmptyObj(out ConnectRegion);
            HOperatorSet.GenEmptyObj(out FindRegion);

            HOperatorSet.Rgb1ToGray(ho_Source, out GrayHobj);

            HTuple H_Gray = new HTuple();
            HTuple H_ImagePosY = new HTuple(PosY);
            HTuple H_ImagePosX = new HTuple(PosX);
            HTuple circularity=new HTuple();
            HTuple rectangularity=new HTuple();
            HTuple parmName=new HTuple();
            HTuple parm = new HTuple();

            HOperatorSet.GetGrayval(GrayHobj, H_ImagePosY, H_ImagePosX, out H_Gray);
            //if (H_Gray == 255)
            //    H_Gray = H_Gray - 1;

            //HOperatorSet.Threshold(GrayHobj, out Region, H_Gray, 255);
            //HOperatorSet.SmoothImage(GrayHobj, out GrayHobj, "gauss", 0.5);
            HOperatorSet.Threshold(GrayHobj, out Region, H_Gray- tol_MagicWand, H_Gray+ tol_MagicWand);
            HOperatorSet.Connection(Region, out ConnectRegion);


            HOperatorSet.SelectRegionPoint(ConnectRegion, out RegionInMouse, PosY, PosX);
            HOperatorSet.TestEqualObj(NullRegion, RegionInMouse, out IsNull);
            if (IsNull.I == 0)
            {
                magicWandType = RoiType.Region;

                //HOperatorSet.FillUp(RegionInMouse, out RegionInMouse);
                FindRegion = RegionInMouse;


                //HOperatorSet.RegionFeatures(RegionInMouse, "circularity", out circularity);
                //HOperatorSet.RegionFeatures(RegionInMouse, "rectangularity", out rectangularity);

                //double _Max = rectangularity.D > circularity.D ? rectangularity.D : circularity.D;
                //string _MaxShape = rectangularity.D > circularity.D ? "rectangularity" : "circularity";
                //if (_Max > 0.6)
                //{
                //    if (_MaxShape == "rectangularity")
                //    {
                //        parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
                //        HOperatorSet.RegionFeatures(RegionInMouse, parmName, out parm);
                //        HOperatorSet.GenRectangle1(out FindRegion, parm[0], parm[1], parm[2], parm[3]);
                //        magicWandType = RoiType.Rectangle;
                //    }
                //    else
                //    {
                //        parmName = new HTuple(new string[] { "row", "column", "radius" });
                //        HOperatorSet.RegionFeatures(RegionInMouse, parmName, out parm);
                //        HOperatorSet.GenCircle(out FindRegion, parm[0], parm[1], parm[2]);
                //        magicWandType = RoiType.Circle;
                //    }
                //}
                //else
                //{
                //    HOperatorSet.SelectRegionPoint(RegionInMouse, out FindRegion, H_ImagePosY, H_ImagePosX);
                //}
            }
            else
            {
                return null;
            }




           
            ConnectRegion.Dispose();
            Region.Dispose();
            GrayHobj.Dispose();

            return FindRegion;
        }
        #endregion

        #region BMP HOBJECT Transfer
        private void HObject2Bitmap8(HObject image, out Bitmap res)
        {
            try
            {
                HTuple hpoint, type, width, height;
                const int Alpha = 255;
                HOperatorSet.GetImagePointer1(image, out hpoint, out type, out width, out height);
                res = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
                ColorPalette pal = res.Palette;
                for (int i = 0; i <= 255; i++)
                {
                    pal.Entries[i] = Color.FromArgb(Alpha, i, i, i);
                }

                res.Palette = pal; Rectangle rect = new Rectangle(0, 0, width, height);
                BitmapData bitmapData = res.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
                int PixelSize = Bitmap.GetPixelFormatSize(bitmapData.PixelFormat) / 8;
                IntPtr ptr1 = bitmapData.Scan0;
                IntPtr ptr2 = hpoint; int bytes = width * height;
                byte[] rgbvalues = new byte[bytes];
                System.Runtime.InteropServices.Marshal.Copy(ptr2, rgbvalues, 0, bytes);
                System.Runtime.InteropServices.Marshal.Copy(rgbvalues, 0, ptr1, bytes);
                res.UnlockBits(bitmapData);
            }
            catch (Exception ex)
            {
                res = null;
            }
        }

        private void Hobject2Bitmap24(HObject hObject, out Bitmap res)
        {
            try
            {//get image size
                HTuple width0 = new HTuple();
                HTuple height0 = new HTuple();
                HTuple Pointer = new HTuple();
                HTuple type = new HTuple();
                HTuple width = new HTuple();
                HTuple height = new HTuple();
                HObject InterImage = new HObject();
                HOperatorSet.GetImageSize(hObject, out width0, out height0);
                //create interleaved-format image
                HOperatorSet.InterleaveChannels(hObject, out InterImage, "rgb", 4 * width0, 0);
                //get interleaved-format image pointer
                HOperatorSet.GetImagePointer1(InterImage, out Pointer, out type, out width, out height);
                IntPtr ptr = Pointer;
                //construct new Bitmap image
                res = new Bitmap(width / 4, height, width, PixelFormat.Format24bppRgb, ptr);
            }
            catch (Exception ex)
            {
                res = null;
            }
        }


        /// <summary>
        /// grayscale image8 bit Bitmap -> HObject
        /// </summary>
        /// <param name="bmp">8 bit Bitmap</param>
        /// <param name="image">HObject</param>
        private void Bitmap2HObjectBpp8(Bitmap bmp, out HObject image)
        {
            try
            {
                //Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);

                //BitmapData srcBmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);

                //HOperatorSet.GenImage1(out image, "byte", bmp.Width, bmp.Height, srcBmpData.Scan0);
                //bmp.UnlockBits(srcBmpData);


                HObject Hobj;
                HOperatorSet.GenEmptyObj(out Hobj);

                Point po = new Point(0, 0);
                Size so = new Size(bmp.Width, bmp.Height);//template.Width, template.Height
                Rectangle ro = new Rectangle(po, so);

                Bitmap DstImage = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format8bppIndexed);
                DstImage = bmp.Clone(ro, PixelFormat.Format8bppIndexed);

                int width = DstImage.Width;
                int height = DstImage.Height;

                Rectangle rect = new Rectangle(0, 0, width, height);
                System.Drawing.Imaging.BitmapData dstBmpData =
                    DstImage.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);//pImage.PixelFormat
                int PixelSize = Bitmap.GetPixelFormatSize(dstBmpData.PixelFormat) / 8;
                int stride = dstBmpData.Stride;

                //the key point is here
                unsafe
                {
                    int count = height * width;
                    byte[] data = new byte[count];
                    byte* bptr = (byte*)dstBmpData.Scan0;
                    fixed (byte* pData = data)
                    {
                        for (int i = 0; i < height; i++)
                            for (int j = 0; j < width; j++)
                            {
                                data[i * width + j] = bptr[i * stride + j];
                            }
                        HOperatorSet.GenImage1(out image, "byte", width, height, new IntPtr(pData));
                    }
                }

                DstImage.UnlockBits(dstBmpData);

            }

            catch (Exception ex)
            {
                image = null;
            }
        }

        /// <summary>
        /// color image32 bit Bitmap -> HObject
        /// </summary>
        /// <param name="bmp">32 bit Bitmap</param>
        /// <param name="image">HObject</param>
        private  void Bitmap2HObjectBpp32_565(Bitmap bmp, out HObject image)  //90ms
        {

            try
            {
                Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);

                BitmapData srcBmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);
                HOperatorSet.GenImageInterleaved(out image, srcBmpData.Scan0, "bgr565", bmp.Width, bmp.Height, 0, "byte", 0, 0, 0, 0, -1, 0);
                bmp.UnlockBits(srcBmpData);

            }
            catch (Exception ex)
            {
                image = null;
            }
        }

        /// <summary>
        /// color image32 bit Bitmap -> HObject
        /// </summary>
        /// <param name="bmp">32 bit Bitmap</param>
        /// <param name="image">HObject</param>
        private  void Bitmap2HObjectBpp32(Bitmap bmp, out HObject image)  //90ms
        {

            try
            {
                Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);

                BitmapData srcBmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);
                HOperatorSet.GenImageInterleaved(out image, srcBmpData.Scan0, "bgrx", bmp.Width, bmp.Height, 0, "byte", 0, 0, 0, 0, -1, 0);
                bmp.UnlockBits(srcBmpData);

            }
            catch (Exception ex)
            {
                image = null;
            }
        }
        /// <summary>
        /// color image24 bit Bitmap -> HObject
        /// </summary>
        /// <param name="bmp">24 bit Bitmap</param>
        /// <param name="image">HObject</param>
        private  void Bitmap2HObjectBpp24(Bitmap bmp, out HObject image)  //90ms
        {
            try
            {
                Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);

                BitmapData bmp_data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                byte[] arrayR = new byte[bmp_data.Width * bmp_data.Height];//red array
                byte[] arrayG = new byte[bmp_data.Width * bmp_data.Height];//green array
                byte[] arrayB = new byte[bmp_data.Width * bmp_data.Height];//blue array
                unsafe
                {

                    byte* pBmp = (byte*)bmp_data.Scan0;//Bitmap header pointer
                                                       //the following loop extracts the red, green, and blue channels into three arrays
                    for (int R = 0; R < bmp_data.Height; R++)
                    {

                        for (int C = 0; C < bmp_data.Width; C++)
                        {

                            //because of how the in-memory Bitmap is stored, row width is calculated with Stride; C*3 is used because this has three channels, and Bitmap data is stored as BGR
                            byte* pBase = pBmp + bmp_data.Stride * R + C * 3;
                            arrayR[R * bmp_data.Width + C] = *(pBase + 2);
                            arrayG[R * bmp_data.Width + C] = *(pBase + 1);
                            arrayB[R * bmp_data.Width + C] = *(pBase);
                        }
                    }
                    fixed (byte* pR = arrayR, pG = arrayG, pB = arrayB)
                    {

                        HOperatorSet.GenImage3(out image, "byte", bmp_data.Width, bmp_data.Height,
                                                                   new IntPtr(pR), new IntPtr(pG), new IntPtr(pB));
                        //if an error occurs here, carefully check whether the previous code is wrong
                    }
                }

                //Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);

                //BitmapData srcBmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);
                //HOperatorSet.GenImageInterleaved(out image, srcBmpData.Scan0, "bgr", bmp.Width, bmp.Height, 0, "byte", 0, 0, 0, 0, -1, 0);
                //bmp.UnlockBits(srcBmpData);
            }
            catch (Exception ex)
            {
                image = null;
            }
        }


        /// <summary>
        /// grayscale image HObject -> HImage1
        /// </summary>
        private HImage HObject2HImage1(HObject hObj)
        {
            HImage image = new HImage();
            HTuple type, width, height, pointer;
            HOperatorSet.GetImagePointer1(hObj, out pointer, out type, out width, out height);
            image.GenImage1(type, width, height, pointer);
            return image;
        }

        /// <summary>
        /// color image HObject -> HImage3
        /// </summary>
        private HImage HObject2HImage3(HObject hObj)
        {
            
            HImage image = new HImage();
            HTuple type, width, height, pointerRed, pointerGreen, pointerBlue;
            HOperatorSet.GetImagePointer3(hObj, out pointerRed, out pointerGreen, out pointerBlue,
                                          out type, out width, out height);
            image.GenImage3(type, width, height, pointerRed, pointerGreen, pointerBlue);
            //hObj.Dispose();
            return image;
        }

        // Procedures 
        // External procedures 
        // Chapter: XLD / Creation
        // Short Description: Creates an arrow shaped XLD contour. 
        private void gen_arrow_contour_xld(out HObject ho_Arrow, HTuple hv_Row1, HTuple hv_Column1,
            HTuple hv_Row2, HTuple hv_Column2, HTuple hv_HeadLength, HTuple hv_HeadWidth)
        {



            // Stack for temporary objects 
            HObject[] OTemp = new HObject[20];

            // Local iconic variables 

            HObject ho_TempArrow = null;

            // Local control variables 

            HTuple hv_Length = new HTuple(), hv_ZeroLengthIndices = new HTuple();
            HTuple hv_DR = new HTuple(), hv_DC = new HTuple(), hv_HalfHeadWidth = new HTuple();
            HTuple hv_RowP1 = new HTuple(), hv_ColP1 = new HTuple();
            HTuple hv_RowP2 = new HTuple(), hv_ColP2 = new HTuple();
            HTuple hv_Index = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_Arrow);
            HOperatorSet.GenEmptyObj(out ho_TempArrow);
            try
            {
                //This procedure generates arrow shaped XLD contours,
                //pointing from (Row1, Column1) to (Row2, Column2).
                //If starting and end point are identical, a contour consisting
                //of a single point is returned.
                //
                //input parameteres:
                //Row1, Column1: Coordinates of the arrows' starting points
                //Row2, Column2: Coordinates of the arrows' end points
                //HeadLength, HeadWidth: Size of the arrow heads in pixels
                //
                //output parameter:
                //Arrow: The resulting XLD contour
                //
                //The input tuples Row1, Column1, Row2, and Column2 have to be of
                //the same length.
                //HeadLength and HeadWidth either have to be of the same length as
                //Row1, Column1, Row2, and Column2 or have to be a single element.
                //If one of the above restrictions is violated, an error will occur.
                //
                //
                //Init
                ho_Arrow.Dispose();
                HOperatorSet.GenEmptyObj(out ho_Arrow);
                //
                //Calculate the arrow length
                hv_Length.Dispose();
                HOperatorSet.DistancePp(hv_Row1, hv_Column1, hv_Row2, hv_Column2, out hv_Length);
                //
                //Mark arrows with identical start and end point
                //(set Length to -1 to avoid division-by-zero exception)
                hv_ZeroLengthIndices.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_ZeroLengthIndices = hv_Length.TupleFind(
                        0);
                }
                if ((int)(new HTuple(hv_ZeroLengthIndices.TupleNotEqual(-1))) != 0)
                {
                    if (hv_Length == null)
                        hv_Length = new HTuple();
                    hv_Length[hv_ZeroLengthIndices] = -1;
                }
                //
                //Calculate auxiliary variables.
                hv_DR.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_DR = (1.0 * (hv_Row2 - hv_Row1)) / hv_Length;
                }
                hv_DC.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_DC = (1.0 * (hv_Column2 - hv_Column1)) / hv_Length;
                }
                hv_HalfHeadWidth.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_HalfHeadWidth = hv_HeadWidth / 2.0;
                }
                //
                //Calculate end points of the arrow head.
                hv_RowP1.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_RowP1 = (hv_Row1 + ((hv_Length - hv_HeadLength) * hv_DR)) + (hv_HalfHeadWidth * hv_DC);
                }
                hv_ColP1.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_ColP1 = (hv_Column1 + ((hv_Length - hv_HeadLength) * hv_DC)) - (hv_HalfHeadWidth * hv_DR);
                }
                hv_RowP2.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_RowP2 = (hv_Row1 + ((hv_Length - hv_HeadLength) * hv_DR)) - (hv_HalfHeadWidth * hv_DC);
                }
                hv_ColP2.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_ColP2 = (hv_Column1 + ((hv_Length - hv_HeadLength) * hv_DC)) + (hv_HalfHeadWidth * hv_DR);
                }
                //
                //Finally create output XLD contour for each input point pair
                for (hv_Index = 0; (int)hv_Index <= (int)((new HTuple(hv_Length.TupleLength())) - 1); hv_Index = (int)hv_Index + 1)
                {
                    if ((int)(new HTuple(((hv_Length.TupleSelect(hv_Index))).TupleEqual(-1))) != 0)
                    {
                        //Create_ single points for arrows with identical start and end point
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            ho_TempArrow.Dispose();
                            HOperatorSet.GenContourPolygonXld(out ho_TempArrow, hv_Row1.TupleSelect(
                                hv_Index), hv_Column1.TupleSelect(hv_Index));
                        }
                    }
                    else
                    {
                        //Create arrow contour
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            ho_TempArrow.Dispose();
                            HOperatorSet.GenContourPolygonXld(out ho_TempArrow, ((((((((((hv_Row1.TupleSelect(
                                hv_Index))).TupleConcat(hv_Row2.TupleSelect(hv_Index)))).TupleConcat(
                                hv_RowP1.TupleSelect(hv_Index)))).TupleConcat(hv_Row2.TupleSelect(hv_Index)))).TupleConcat(
                                hv_RowP2.TupleSelect(hv_Index)))).TupleConcat(hv_Row2.TupleSelect(hv_Index)),
                                ((((((((((hv_Column1.TupleSelect(hv_Index))).TupleConcat(hv_Column2.TupleSelect(
                                hv_Index)))).TupleConcat(hv_ColP1.TupleSelect(hv_Index)))).TupleConcat(
                                hv_Column2.TupleSelect(hv_Index)))).TupleConcat(hv_ColP2.TupleSelect(
                                hv_Index)))).TupleConcat(hv_Column2.TupleSelect(hv_Index)));
                        }
                    }
                    {
                        HObject ExpTmpOutVar_0;
                        HOperatorSet.ConcatObj(ho_Arrow, ho_TempArrow, out ExpTmpOutVar_0);
                        ho_Arrow.Dispose();
                        ho_Arrow = ExpTmpOutVar_0;
                    }
                }
                ho_TempArrow.Dispose();

                hv_Length.Dispose();
                hv_ZeroLengthIndices.Dispose();
                hv_DR.Dispose();
                hv_DC.Dispose();
                hv_HalfHeadWidth.Dispose();
                hv_RowP1.Dispose();
                hv_ColP1.Dispose();
                hv_RowP2.Dispose();
                hv_ColP2.Dispose();
                hv_Index.Dispose();

                return;
            }
            catch (HalconException HDevExpDefaultException)
            {
                ho_TempArrow.Dispose();

                hv_Length.Dispose();
                hv_ZeroLengthIndices.Dispose();
                hv_DR.Dispose();
                hv_DC.Dispose();
                hv_HalfHeadWidth.Dispose();
                hv_RowP1.Dispose();
                hv_ColP1.Dispose();
                hv_RowP2.Dispose();
                hv_ColP2.Dispose();
                hv_Index.Dispose();

                throw HDevExpDefaultException;
            }
        }
        // Chapter: Graphics / Output
        // Short Description:  This procedure plots tuples representing functions or curves in a coordinate system. 
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hv_WindowHandle"></param>
        /// <param name="hv_XValues"></param>
        /// <param name="hv_YValues"></param>
        /// <param name="hv_XLabel"></param>
        /// <param name="hv_YLabel"></param>
        /// <param name="hv_Color"></param>
        /// <param name="hv_GenParamName"></param>
        /// <param name="hv_GenParamValue"></param>
        public void plot_tuple(HTuple hv_WindowHandle, HTuple hv_XValues, HTuple hv_YValues,
            HTuple hv_XLabel, HTuple hv_YLabel, HTuple hv_Color, HTuple hv_GenParamName,
            HTuple hv_GenParamValue)
        {



            // Stack for temporary objects 
            HObject[] OTemp = new HObject[20];

            // Local iconic variables 

            HObject ho_ContourXGrid = null, ho_ContourYGrid = null;
            HObject ho_XArrow = null, ho_YArrow = null, ho_ContourXTick = null;
            HObject ho_ContourYTick = null, ho_Contour = null, ho_Cross = null;
            HObject ho_Filled = null, ho_Stair = null, ho_StairTmp = null;

            // Local control variables 

            HTuple hv_PreviousWindowHandle = new HTuple();
            HTuple hv_ClipRegion = new HTuple(), hv_Row = new HTuple();
            HTuple hv_Column = new HTuple(), hv_Width = new HTuple();
            HTuple hv_Height = new HTuple(), hv_PartRow1 = new HTuple();
            HTuple hv_PartColumn1 = new HTuple(), hv_PartRow2 = new HTuple();
            HTuple hv_PartColumn2 = new HTuple(), hv_Red = new HTuple();
            HTuple hv_Green = new HTuple(), hv_Blue = new HTuple();
            HTuple hv_DrawMode = new HTuple(), hv_OriginStyle = new HTuple();
            HTuple hv_XAxisEndValue = new HTuple(), hv_YAxisEndValue = new HTuple();
            HTuple hv_XAxisStartValue = new HTuple(), hv_YAxisStartValue = new HTuple();
            HTuple hv_XValuesAreStrings = new HTuple(), hv_XTickValues = new HTuple();
            HTuple hv_XTicks = new HTuple(), hv_YAxisPosition = new HTuple();
            HTuple hv_XAxisPosition = new HTuple(), hv_LeftBorder = new HTuple();
            HTuple hv_RightBorder = new HTuple(), hv_UpperBorder = new HTuple();
            HTuple hv_LowerBorder = new HTuple(), hv_AxesColor = new HTuple();
            HTuple hv_Style = new HTuple(), hv_Clip = new HTuple();
            HTuple hv_YTicks = new HTuple(), hv_XGrid = new HTuple();
            HTuple hv_YGrid = new HTuple(), hv_GridColor = new HTuple();
            HTuple hv_YPosition = new HTuple(), hv_FormatX = new HTuple();
            HTuple hv_FormatY = new HTuple(), hv_NumGenParamNames = new HTuple();
            HTuple hv_NumGenParamValues = new HTuple(), hv_GenParamIndex = new HTuple();
            HTuple hv_XGridTicks = new HTuple(), hv_YTickDirection = new HTuple();
            HTuple hv_XTickDirection = new HTuple(), hv_XAxisWidthPx = new HTuple();
            HTuple hv_XAxisWidth = new HTuple(), hv_XScaleFactor = new HTuple();
            HTuple hv_YAxisHeightPx = new HTuple(), hv_YAxisHeight = new HTuple();
            HTuple hv_YScaleFactor = new HTuple(), hv_YAxisOffsetPx = new HTuple();
            HTuple hv_XAxisOffsetPx = new HTuple(), hv_DotStyle = new HTuple();
            HTuple hv_XGridValues = new HTuple(), hv_XGridStart = new HTuple();
            HTuple hv_XCoord = new HTuple(), hv_IndexGrid = new HTuple();
            HTuple hv_YGridValues = new HTuple(), hv_YGridStart = new HTuple();
            HTuple hv_YCoord = new HTuple(), hv_Ascent = new HTuple();
            HTuple hv_Descent = new HTuple(), hv_TextWidthXLabel = new HTuple();
            HTuple hv_TextHeightXLabel = new HTuple(), hv_TextWidthYLabel = new HTuple();
            HTuple hv_TextHeightYLabel = new HTuple(), hv_XTickStart = new HTuple();
            HTuple hv_Indices = new HTuple(), hv_TypeTicks = new HTuple();
            HTuple hv_IndexTicks = new HTuple(), hv_Ascent1 = new HTuple();
            HTuple hv_Descent1 = new HTuple(), hv_TextWidthXTicks = new HTuple();
            HTuple hv_TextHeightXTicks = new HTuple(), hv_YTickValues = new HTuple();
            HTuple hv_YTickStart = new HTuple(), hv_TextWidthYTicks = new HTuple();
            HTuple hv_TextHeightYTicks = new HTuple(), hv_Num = new HTuple();
            HTuple hv_I = new HTuple(), hv_YSelected = new HTuple();
            HTuple hv_Y1Selected = new HTuple(), hv_X1Selected = new HTuple();
            HTuple hv_Index = new HTuple(), hv_Row1 = new HTuple();
            HTuple hv_Row2 = new HTuple(), hv_Col1 = new HTuple();
            HTuple hv_Col2 = new HTuple();
            HTuple hv_XValues_COPY_INP_TMP = new HTuple(hv_XValues);
            HTuple hv_YValues_COPY_INP_TMP = new HTuple(hv_YValues);

            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_ContourXGrid);
            HOperatorSet.GenEmptyObj(out ho_ContourYGrid);
            HOperatorSet.GenEmptyObj(out ho_XArrow);
            HOperatorSet.GenEmptyObj(out ho_YArrow);
            HOperatorSet.GenEmptyObj(out ho_ContourXTick);
            HOperatorSet.GenEmptyObj(out ho_ContourYTick);
            HOperatorSet.GenEmptyObj(out ho_Contour);
            HOperatorSet.GenEmptyObj(out ho_Cross);
            HOperatorSet.GenEmptyObj(out ho_Filled);
            HOperatorSet.GenEmptyObj(out ho_Stair);
            HOperatorSet.GenEmptyObj(out ho_StairTmp);
            try
            {
                //This procedure plots tuples representing functions
                //or curves in a coordinate system.
                //
                //Input parameters:
                //
                //XValues: X values of the function to be plotted
                //         If XValues is set to [], it is internally set to 0,1,2,...,|YValues|-1.
                //         If XValues is a tuple of strings, the values are taken as categories.
                //
                //YValues: Y values of the function(s) to be plotted
                //         If YValues is set to [], it is internally set to 0,1,2,...,|XValues|-1.
                //         The number of y values must be equal to the number of x values
                //         or an integral multiple. In the latter case,
                //         multiple functions are plotted, that share the same x values.
                //
                //XLabel: X-axis label
                //
                //XLabel: Y-axis label
                //
                //Color: Color of the plotted function
                //       If [] is given, the currently set display color is used.
                //       If 'none is given, the function is not plotted, but only
                //       the coordinate axes as specified.
                //       If more than one color is given, multiple functions
                //       can be displayed in different colors.
                //
                //GenParamName:  Generic parameters to control the presentation
                //               Possible Values:
                //   'axes_color': coordinate system color
                //                 Default: 'white'
                //                 If 'none' is given, no coordinate system is shown.
                //   'style': Graph style
                //            Possible values: 'line' (default), 'cross', 'step', 'filled'
                //   'clip': Clip graph to coordinate system area
                //           Possible values: 'yes', 'no' (default)
                //   'ticks': Control display of ticks on the axes
                //            If 'min_max_origin' is given (default), ticks are shown
                //            at the minimum and maximum values of the axes and at the
                //            intercept point of x- and y-axis.
                //            If 'none' is given, no ticks are shown.
                //            If any number != 0 is given, it is interpreted as distance
                //            between the ticks.
                //   'ticks_x': Control display of ticks on x-axis only
                //   'ticks_y': Control display of ticks on y-axis only
                //   'format_x': Format of the values next to the ticks of the x-axis
                //               (see tuple_string for more details).
                //   'format_y': Format of the values next to the ticks of the y-axis
                //               (see tuple_string for more details).
                //   'grid': Control display of grid lines within the coordinate system
                //           If 'min_max_origin' is given (default), grid lines are shown
                //           at the minimum and maximum values of the axes.
                //           If 'none' is given, no grid lines are shown.
                //           If any number != 0 is given, it is interpreted as distance
                //           between the grid lines.
                //   'grid_x': Control display of grid lines for the x-axis only
                //   'grid_y': Control display of grid lines for the y-axis only
                //   'grid_color': Color of the grid (default: 'dim gray')
                //   'margin': The distance in pixels of the coordinate system area
                //             to all four window borders.
                //   'margin_left': The distance in pixels of the coordinate system area
                //                  to the left window border.
                //   'margin_right': The distance in pixels of the coordinate system area
                //                   to the right window border.
                //   'margin_top': The distance in pixels of the coordinate system area
                //                 to the upper window border.
                //   'margin_bottom': The distance in pixels of the coordinate system area
                //                    to the lower window border.
                //   'start_x': Lowest x value of the x-axis
                //              Default: min(XValues)
                //   'end_x': Highest x value of the x-axis
                //            Default: max(XValues)
                //   'start_y': Lowest y value of the y-axis
                //              Default: min(YValues)
                //   'end_y': Highest y value of the y-axis
                //            Default: max(YValues)
                //   'axis_location_x': Either 'bottom', 'origin', or 'top'
                //               to position the x-axis conveniently,
                //               or the Y coordinate of the intercept point of x- and y-axis.
                //               Default: 'bottom'
                //               (Used to be called 'origin_y')
                //   'axis_location_y': Either 'left', 'origin', or 'right'
                //               to position the y-axis conveniently,
                //               or the X coordinate of the intercept point of x- and y-axis.
                //               Default: 'left'
                //               (Used to be called 'origin_x')
                //
                //GenParamValue: Values of the generic parameters of GenericParamName
                //
                //
                //Store current display settings

                if (HDevWindowStack.IsOpen())
                {
                    hv_PreviousWindowHandle = HDevWindowStack.GetActive();
                }
                HDevWindowStack.SetActive(hv_WindowHandle);

                hv_ClipRegion.Dispose();
                HOperatorSet.GetSystem("clip_region", out hv_ClipRegion);
                hv_Row.Dispose(); hv_Column.Dispose(); hv_Width.Dispose(); hv_Height.Dispose();
                HOperatorSet.GetWindowExtents(hv_WindowHandle, out hv_Row, out hv_Column, out hv_Width,
                    out hv_Height);
                hv_PartRow1.Dispose(); hv_PartColumn1.Dispose(); hv_PartRow2.Dispose(); hv_PartColumn2.Dispose();
                HOperatorSet.GetPart(hv_WindowHandle, out hv_PartRow1, out hv_PartColumn1,
                    out hv_PartRow2, out hv_PartColumn2);
                hv_Red.Dispose(); hv_Green.Dispose(); hv_Blue.Dispose();
                HOperatorSet.GetRgb(hv_WindowHandle, out hv_Red, out hv_Green, out hv_Blue);
                hv_DrawMode.Dispose();
                HOperatorSet.GetDraw(hv_WindowHandle, out hv_DrawMode);
                hv_OriginStyle.Dispose();
                HOperatorSet.GetLineStyle(hv_WindowHandle, out hv_OriginStyle);
                //
                //Set display parameters
                HOperatorSet.SetLineStyle(hv_WindowHandle, new HTuple());
                HOperatorSet.SetSystem("clip_region", "false");
                if (HDevWindowStack.IsOpen())
                {
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        HOperatorSet.SetPart(HDevWindowStack.GetActive(), 0, 0, hv_Height - 1, hv_Width - 1);
                    }
                }
                //
                //Check input coordinates
                //
                if ((int)((new HTuple(hv_XValues_COPY_INP_TMP.TupleEqual(new HTuple()))).TupleAnd(
                    new HTuple(hv_YValues_COPY_INP_TMP.TupleEqual(new HTuple())))) != 0)
                {
                    //Neither XValues nor YValues are given:
                    //Set axes to interval [0,1]
                    hv_XAxisEndValue.Dispose();
                    hv_XAxisEndValue = 1;
                    hv_YAxisEndValue.Dispose();
                    hv_YAxisEndValue = 1;
                    hv_XAxisStartValue.Dispose();
                    hv_XAxisStartValue = 0;
                    hv_YAxisStartValue.Dispose();
                    hv_YAxisStartValue = 0;
                    hv_XValuesAreStrings.Dispose();
                    hv_XValuesAreStrings = 0;
                }
                else
                {
                    if ((int)(new HTuple(hv_XValues_COPY_INP_TMP.TupleEqual(new HTuple()))) != 0)
                    {
                        //XValues are omitted:
                        //Set equidistant XValues
                        hv_XValues_COPY_INP_TMP.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_XValues_COPY_INP_TMP = HTuple.TupleGenSequence(
                                0, (new HTuple(hv_YValues_COPY_INP_TMP.TupleLength())) - 1, 1);
                        }
                        hv_XValuesAreStrings.Dispose();
                        hv_XValuesAreStrings = 0;
                    }
                    else if ((int)(new HTuple(hv_YValues_COPY_INP_TMP.TupleEqual(new HTuple()))) != 0)
                    {
                        //YValues are omitted:
                        //Set equidistant YValues
                        hv_YValues_COPY_INP_TMP.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_YValues_COPY_INP_TMP = HTuple.TupleGenSequence(
                                0, (new HTuple(hv_XValues_COPY_INP_TMP.TupleLength())) - 1, 1);
                        }
                    }
                    if ((int)(new HTuple((new HTuple((new HTuple(hv_YValues_COPY_INP_TMP.TupleLength()
                        )) % (new HTuple(hv_XValues_COPY_INP_TMP.TupleLength())))).TupleNotEqual(
                        0))) != 0)
                    {
                        //Number of YValues does not match number of XValues
                        throw new HalconException("Number of YValues is no multiple of the number of XValues!");
                        ho_ContourXGrid.Dispose();
                        ho_ContourYGrid.Dispose();
                        ho_XArrow.Dispose();
                        ho_YArrow.Dispose();
                        ho_ContourXTick.Dispose();
                        ho_ContourYTick.Dispose();
                        ho_Contour.Dispose();
                        ho_Cross.Dispose();
                        ho_Filled.Dispose();
                        ho_Stair.Dispose();
                        ho_StairTmp.Dispose();

                        hv_XValues_COPY_INP_TMP.Dispose();
                        hv_YValues_COPY_INP_TMP.Dispose();
                        hv_PreviousWindowHandle.Dispose();
                        hv_ClipRegion.Dispose();
                        hv_Row.Dispose();
                        hv_Column.Dispose();
                        hv_Width.Dispose();
                        hv_Height.Dispose();
                        hv_PartRow1.Dispose();
                        hv_PartColumn1.Dispose();
                        hv_PartRow2.Dispose();
                        hv_PartColumn2.Dispose();
                        hv_Red.Dispose();
                        hv_Green.Dispose();
                        hv_Blue.Dispose();
                        hv_DrawMode.Dispose();
                        hv_OriginStyle.Dispose();
                        hv_XAxisEndValue.Dispose();
                        hv_YAxisEndValue.Dispose();
                        hv_XAxisStartValue.Dispose();
                        hv_YAxisStartValue.Dispose();
                        hv_XValuesAreStrings.Dispose();
                        hv_XTickValues.Dispose();
                        hv_XTicks.Dispose();
                        hv_YAxisPosition.Dispose();
                        hv_XAxisPosition.Dispose();
                        hv_LeftBorder.Dispose();
                        hv_RightBorder.Dispose();
                        hv_UpperBorder.Dispose();
                        hv_LowerBorder.Dispose();
                        hv_AxesColor.Dispose();
                        hv_Style.Dispose();
                        hv_Clip.Dispose();
                        hv_YTicks.Dispose();
                        hv_XGrid.Dispose();
                        hv_YGrid.Dispose();
                        hv_GridColor.Dispose();
                        hv_YPosition.Dispose();
                        hv_FormatX.Dispose();
                        hv_FormatY.Dispose();
                        hv_NumGenParamNames.Dispose();
                        hv_NumGenParamValues.Dispose();
                        hv_GenParamIndex.Dispose();
                        hv_XGridTicks.Dispose();
                        hv_YTickDirection.Dispose();
                        hv_XTickDirection.Dispose();
                        hv_XAxisWidthPx.Dispose();
                        hv_XAxisWidth.Dispose();
                        hv_XScaleFactor.Dispose();
                        hv_YAxisHeightPx.Dispose();
                        hv_YAxisHeight.Dispose();
                        hv_YScaleFactor.Dispose();
                        hv_YAxisOffsetPx.Dispose();
                        hv_XAxisOffsetPx.Dispose();
                        hv_DotStyle.Dispose();
                        hv_XGridValues.Dispose();
                        hv_XGridStart.Dispose();
                        hv_XCoord.Dispose();
                        hv_IndexGrid.Dispose();
                        hv_YGridValues.Dispose();
                        hv_YGridStart.Dispose();
                        hv_YCoord.Dispose();
                        hv_Ascent.Dispose();
                        hv_Descent.Dispose();
                        hv_TextWidthXLabel.Dispose();
                        hv_TextHeightXLabel.Dispose();
                        hv_TextWidthYLabel.Dispose();
                        hv_TextHeightYLabel.Dispose();
                        hv_XTickStart.Dispose();
                        hv_Indices.Dispose();
                        hv_TypeTicks.Dispose();
                        hv_IndexTicks.Dispose();
                        hv_Ascent1.Dispose();
                        hv_Descent1.Dispose();
                        hv_TextWidthXTicks.Dispose();
                        hv_TextHeightXTicks.Dispose();
                        hv_YTickValues.Dispose();
                        hv_YTickStart.Dispose();
                        hv_TextWidthYTicks.Dispose();
                        hv_TextHeightYTicks.Dispose();
                        hv_Num.Dispose();
                        hv_I.Dispose();
                        hv_YSelected.Dispose();
                        hv_Y1Selected.Dispose();
                        hv_X1Selected.Dispose();
                        hv_Index.Dispose();
                        hv_Row1.Dispose();
                        hv_Row2.Dispose();
                        hv_Col1.Dispose();
                        hv_Col2.Dispose();

                        return;
                    }
                    hv_XValuesAreStrings.Dispose();
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        hv_XValuesAreStrings = hv_XValues_COPY_INP_TMP.TupleIsStringElem()
                            ;
                    }
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        {
                            HTuple
                              ExpTmpLocalVar_XValuesAreStrings = new HTuple(((hv_XValuesAreStrings.TupleSum()
                                )).TupleEqual(new HTuple(hv_XValuesAreStrings.TupleLength())));
                            hv_XValuesAreStrings.Dispose();
                            hv_XValuesAreStrings = ExpTmpLocalVar_XValuesAreStrings;
                        }
                    }
                    if ((int)(hv_XValuesAreStrings) != 0)
                    {
                        //XValues are given as strings:
                        //Show XValues as ticks
                        hv_XTickValues.Dispose();
                        hv_XTickValues = new HTuple(hv_XValues_COPY_INP_TMP);
                        hv_XTicks.Dispose();
                        hv_XTicks = 1;
                        //Set x-axis dimensions
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            {
                                HTuple
                                  ExpTmpLocalVar_XValues = HTuple.TupleGenSequence(
                                    1, new HTuple(hv_XValues_COPY_INP_TMP.TupleLength()), 1);
                                hv_XValues_COPY_INP_TMP.Dispose();
                                hv_XValues_COPY_INP_TMP = ExpTmpLocalVar_XValues;
                            }
                        }
                    }
                    //Set default x-axis dimensions
                    if ((int)(new HTuple((new HTuple(hv_XValues_COPY_INP_TMP.TupleLength())).TupleGreater(
                        1))) != 0)
                    {
                        hv_XAxisStartValue.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_XAxisStartValue = hv_XValues_COPY_INP_TMP.TupleMin()
                                ;
                        }
                        hv_XAxisEndValue.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_XAxisEndValue = hv_XValues_COPY_INP_TMP.TupleMax()
                                ;
                        }
                    }
                    else
                    {
                        hv_XAxisEndValue.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_XAxisEndValue = (hv_XValues_COPY_INP_TMP.TupleSelect(
                                0)) + 0.5;
                        }
                        hv_XAxisStartValue.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_XAxisStartValue = (hv_XValues_COPY_INP_TMP.TupleSelect(
                                0)) - 0.5;
                        }
                    }
                }
                //Set default y-axis dimensions
                if ((int)(new HTuple((new HTuple(hv_YValues_COPY_INP_TMP.TupleLength())).TupleGreater(
                    1))) != 0)
                {
                    hv_YAxisStartValue.Dispose();
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        hv_YAxisStartValue = hv_YValues_COPY_INP_TMP.TupleMin()
                            ;
                    }
                    hv_YAxisEndValue.Dispose();
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        hv_YAxisEndValue = hv_YValues_COPY_INP_TMP.TupleMax()
                            ;
                    }
                }
                else if ((int)(new HTuple((new HTuple(hv_YValues_COPY_INP_TMP.TupleLength()
                    )).TupleEqual(1))) != 0)
                {
                    hv_YAxisStartValue.Dispose();
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        hv_YAxisStartValue = (hv_YValues_COPY_INP_TMP.TupleSelect(
                            0)) - 0.5;
                    }
                    hv_YAxisEndValue.Dispose();
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        hv_YAxisEndValue = (hv_YValues_COPY_INP_TMP.TupleSelect(
                            0)) + 0.5;
                    }
                }
                else
                {
                    hv_YAxisStartValue.Dispose();
                    hv_YAxisStartValue = 0;
                    hv_YAxisEndValue.Dispose();
                    hv_YAxisEndValue = 1;
                }
                //Set default interception point of x- and y- axis
                hv_YAxisPosition.Dispose();
                hv_YAxisPosition = "default";
                hv_XAxisPosition.Dispose();
                hv_XAxisPosition = "default";
                //
                //Set more defaults
                hv_LeftBorder.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_LeftBorder = hv_Width * 0.1;
                }
                hv_RightBorder.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_RightBorder = hv_Width * 0.1;
                }
                hv_UpperBorder.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_UpperBorder = hv_Height * 0.1;
                }
                hv_LowerBorder.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_LowerBorder = hv_Height * 0.1;
                }
                hv_AxesColor.Dispose();
                hv_AxesColor = "white";
                hv_Style.Dispose();
                hv_Style = "line";
                hv_Clip.Dispose();
                hv_Clip = "no";
                hv_XTicks.Dispose();
                hv_XTicks = "min_max_origin";
                hv_YTicks.Dispose();
                hv_YTicks = "min_max_origin";
                hv_XGrid.Dispose();
                hv_XGrid = "none";
                hv_YGrid.Dispose();
                hv_YGrid = "none";
                hv_GridColor.Dispose();
                hv_GridColor = "dim gray";
                hv_YPosition.Dispose();
                hv_YPosition = "left";
                hv_FormatX.Dispose();
                hv_FormatX = "default";
                hv_FormatY.Dispose();
                hv_FormatY = "default";
                //
                //Parse generic parameters
                //
                hv_NumGenParamNames.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_NumGenParamNames = new HTuple(hv_GenParamName.TupleLength()
                        );
                }
                hv_NumGenParamValues.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_NumGenParamValues = new HTuple(hv_GenParamValue.TupleLength()
                        );
                }
                if ((int)(new HTuple(hv_NumGenParamNames.TupleNotEqual(hv_NumGenParamValues))) != 0)
                {
                    throw new HalconException("Number of generic parameter names does not match generic parameter values!");
                    ho_ContourXGrid.Dispose();
                    ho_ContourYGrid.Dispose();
                    ho_XArrow.Dispose();
                    ho_YArrow.Dispose();
                    ho_ContourXTick.Dispose();
                    ho_ContourYTick.Dispose();
                    ho_Contour.Dispose();
                    ho_Cross.Dispose();
                    ho_Filled.Dispose();
                    ho_Stair.Dispose();
                    ho_StairTmp.Dispose();

                    hv_XValues_COPY_INP_TMP.Dispose();
                    hv_YValues_COPY_INP_TMP.Dispose();
                    hv_PreviousWindowHandle.Dispose();
                    hv_ClipRegion.Dispose();
                    hv_Row.Dispose();
                    hv_Column.Dispose();
                    hv_Width.Dispose();
                    hv_Height.Dispose();
                    hv_PartRow1.Dispose();
                    hv_PartColumn1.Dispose();
                    hv_PartRow2.Dispose();
                    hv_PartColumn2.Dispose();
                    hv_Red.Dispose();
                    hv_Green.Dispose();
                    hv_Blue.Dispose();
                    hv_DrawMode.Dispose();
                    hv_OriginStyle.Dispose();
                    hv_XAxisEndValue.Dispose();
                    hv_YAxisEndValue.Dispose();
                    hv_XAxisStartValue.Dispose();
                    hv_YAxisStartValue.Dispose();
                    hv_XValuesAreStrings.Dispose();
                    hv_XTickValues.Dispose();
                    hv_XTicks.Dispose();
                    hv_YAxisPosition.Dispose();
                    hv_XAxisPosition.Dispose();
                    hv_LeftBorder.Dispose();
                    hv_RightBorder.Dispose();
                    hv_UpperBorder.Dispose();
                    hv_LowerBorder.Dispose();
                    hv_AxesColor.Dispose();
                    hv_Style.Dispose();
                    hv_Clip.Dispose();
                    hv_YTicks.Dispose();
                    hv_XGrid.Dispose();
                    hv_YGrid.Dispose();
                    hv_GridColor.Dispose();
                    hv_YPosition.Dispose();
                    hv_FormatX.Dispose();
                    hv_FormatY.Dispose();
                    hv_NumGenParamNames.Dispose();
                    hv_NumGenParamValues.Dispose();
                    hv_GenParamIndex.Dispose();
                    hv_XGridTicks.Dispose();
                    hv_YTickDirection.Dispose();
                    hv_XTickDirection.Dispose();
                    hv_XAxisWidthPx.Dispose();
                    hv_XAxisWidth.Dispose();
                    hv_XScaleFactor.Dispose();
                    hv_YAxisHeightPx.Dispose();
                    hv_YAxisHeight.Dispose();
                    hv_YScaleFactor.Dispose();
                    hv_YAxisOffsetPx.Dispose();
                    hv_XAxisOffsetPx.Dispose();
                    hv_DotStyle.Dispose();
                    hv_XGridValues.Dispose();
                    hv_XGridStart.Dispose();
                    hv_XCoord.Dispose();
                    hv_IndexGrid.Dispose();
                    hv_YGridValues.Dispose();
                    hv_YGridStart.Dispose();
                    hv_YCoord.Dispose();
                    hv_Ascent.Dispose();
                    hv_Descent.Dispose();
                    hv_TextWidthXLabel.Dispose();
                    hv_TextHeightXLabel.Dispose();
                    hv_TextWidthYLabel.Dispose();
                    hv_TextHeightYLabel.Dispose();
                    hv_XTickStart.Dispose();
                    hv_Indices.Dispose();
                    hv_TypeTicks.Dispose();
                    hv_IndexTicks.Dispose();
                    hv_Ascent1.Dispose();
                    hv_Descent1.Dispose();
                    hv_TextWidthXTicks.Dispose();
                    hv_TextHeightXTicks.Dispose();
                    hv_YTickValues.Dispose();
                    hv_YTickStart.Dispose();
                    hv_TextWidthYTicks.Dispose();
                    hv_TextHeightYTicks.Dispose();
                    hv_Num.Dispose();
                    hv_I.Dispose();
                    hv_YSelected.Dispose();
                    hv_Y1Selected.Dispose();
                    hv_X1Selected.Dispose();
                    hv_Index.Dispose();
                    hv_Row1.Dispose();
                    hv_Row2.Dispose();
                    hv_Col1.Dispose();
                    hv_Col2.Dispose();

                    return;
                }
                //
                for (hv_GenParamIndex = 0; (int)hv_GenParamIndex <= (int)((new HTuple(hv_GenParamName.TupleLength()
                    )) - 1); hv_GenParamIndex = (int)hv_GenParamIndex + 1)
                {
                    //
                    //Set 'axes_color'
                    if ((int)(new HTuple(((hv_GenParamName.TupleSelect(hv_GenParamIndex))).TupleEqual(
                        "axes_color"))) != 0)
                    {
                        hv_AxesColor.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_AxesColor = hv_GenParamValue.TupleSelect(
                                hv_GenParamIndex);
                        }
                        //
                        //Set 'style'
                    }
                    else if ((int)(new HTuple(((hv_GenParamName.TupleSelect(hv_GenParamIndex))).TupleEqual(
                        "style"))) != 0)
                    {
                        hv_Style.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_Style = hv_GenParamValue.TupleSelect(
                                hv_GenParamIndex);
                        }
                        //
                        //Set 'clip'
                    }
                    else if ((int)(new HTuple(((hv_GenParamName.TupleSelect(hv_GenParamIndex))).TupleEqual(
                        "clip"))) != 0)
                    {
                        hv_Clip.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_Clip = hv_GenParamValue.TupleSelect(
                                hv_GenParamIndex);
                        }
                        if ((int)((new HTuple(hv_Clip.TupleNotEqual("yes"))).TupleAnd(new HTuple(hv_Clip.TupleNotEqual(
                            "no")))) != 0)
                        {
                            throw new HalconException(("Unsupported clipping option: '" + hv_Clip) + "'");
                        }
                        //
                        //Set 'ticks'
                    }
                    else if ((int)(new HTuple(((hv_GenParamName.TupleSelect(hv_GenParamIndex))).TupleEqual(
                        "ticks"))) != 0)
                    {
                        hv_XTicks.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_XTicks = hv_GenParamValue.TupleSelect(
                                hv_GenParamIndex);
                        }
                        hv_YTicks.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_YTicks = hv_GenParamValue.TupleSelect(
                                hv_GenParamIndex);
                        }
                        //
                        //Set 'ticks_x'
                    }
                    else if ((int)(new HTuple(((hv_GenParamName.TupleSelect(hv_GenParamIndex))).TupleEqual(
                        "ticks_x"))) != 0)
                    {
                        hv_XTicks.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_XTicks = hv_GenParamValue.TupleSelect(
                                hv_GenParamIndex);
                        }
                        //
                        //Set 'ticks_y'
                    }
                    else if ((int)(new HTuple(((hv_GenParamName.TupleSelect(hv_GenParamIndex))).TupleEqual(
                        "ticks_y"))) != 0)
                    {
                        hv_YTicks.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_YTicks = hv_GenParamValue.TupleSelect(
                                hv_GenParamIndex);
                        }
                        //
                        //Set 'grid'
                    }
                    else if ((int)(new HTuple(((hv_GenParamName.TupleSelect(hv_GenParamIndex))).TupleEqual(
                        "grid"))) != 0)
                    {
                        hv_XGrid.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_XGrid = hv_GenParamValue.TupleSelect(
                                hv_GenParamIndex);
                        }
                        hv_YGrid.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_YGrid = hv_GenParamValue.TupleSelect(
                                hv_GenParamIndex);
                        }
                        hv_XGridTicks.Dispose();
                        hv_XGridTicks = new HTuple(hv_XTicks);
                        //
                        //Set 'grid_x'
                    }
                    else if ((int)(new HTuple(((hv_GenParamName.TupleSelect(hv_GenParamIndex))).TupleEqual(
                        "grid_x"))) != 0)
                    {
                        hv_XGrid.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_XGrid = hv_GenParamValue.TupleSelect(
                                hv_GenParamIndex);
                        }
                        //
                        //Set 'grid_y'
                    }
                    else if ((int)(new HTuple(((hv_GenParamName.TupleSelect(hv_GenParamIndex))).TupleEqual(
                        "grid_y"))) != 0)
                    {
                        hv_YGrid.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_YGrid = hv_GenParamValue.TupleSelect(
                                hv_GenParamIndex);
                        }
                        //
                        //Set 'grid_color'
                    }
                    else if ((int)(new HTuple(((hv_GenParamName.TupleSelect(hv_GenParamIndex))).TupleEqual(
                        "grid_color"))) != 0)
                    {
                        hv_GridColor.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_GridColor = hv_GenParamValue.TupleSelect(
                                hv_GenParamIndex);
                        }
                        //
                        //Set 'start_x'
                    }
                    else if ((int)(new HTuple(((hv_GenParamName.TupleSelect(hv_GenParamIndex))).TupleEqual(
                        "start_x"))) != 0)
                    {
                        hv_XAxisStartValue.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_XAxisStartValue = hv_GenParamValue.TupleSelect(
                                hv_GenParamIndex);
                        }
                        //
                        //Set 'end_x'
                    }
                    else if ((int)(new HTuple(((hv_GenParamName.TupleSelect(hv_GenParamIndex))).TupleEqual(
                        "end_x"))) != 0)
                    {
                        hv_XAxisEndValue.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_XAxisEndValue = hv_GenParamValue.TupleSelect(
                                hv_GenParamIndex);
                        }
                        //
                        //Set 'start_y'
                    }
                    else if ((int)(new HTuple(((hv_GenParamName.TupleSelect(hv_GenParamIndex))).TupleEqual(
                        "start_y"))) != 0)
                    {
                        hv_YAxisStartValue.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_YAxisStartValue = hv_GenParamValue.TupleSelect(
                                hv_GenParamIndex);
                        }
                        //
                        //Set 'end_y'
                    }
                    else if ((int)(new HTuple(((hv_GenParamName.TupleSelect(hv_GenParamIndex))).TupleEqual(
                        "end_y"))) != 0)
                    {
                        hv_YAxisEndValue.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_YAxisEndValue = hv_GenParamValue.TupleSelect(
                                hv_GenParamIndex);
                        }
                        //
                        //Set 'axis_location_y' (old name 'origin_x')
                    }
                    else if ((int)((new HTuple(((hv_GenParamName.TupleSelect(hv_GenParamIndex))).TupleEqual(
                        "axis_location_y"))).TupleOr(new HTuple(((hv_GenParamName.TupleSelect(
                        hv_GenParamIndex))).TupleEqual("origin_x")))) != 0)
                    {
                        hv_YAxisPosition.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_YAxisPosition = hv_GenParamValue.TupleSelect(
                                hv_GenParamIndex);
                        }
                        //
                        //Set 'axis_location_x' (old name: 'origin_y')
                    }
                    else if ((int)((new HTuple(((hv_GenParamName.TupleSelect(hv_GenParamIndex))).TupleEqual(
                        "axis_location_x"))).TupleOr(new HTuple(((hv_GenParamName.TupleSelect(
                        hv_GenParamIndex))).TupleEqual("origin_y")))) != 0)
                    {
                        hv_XAxisPosition.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_XAxisPosition = hv_GenParamValue.TupleSelect(
                                hv_GenParamIndex);
                        }
                        //
                        //Set 'margin'
                    }
                    else if ((int)(new HTuple(((hv_GenParamName.TupleSelect(hv_GenParamIndex))).TupleEqual(
                        "margin"))) != 0)
                    {
                        hv_LeftBorder.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_LeftBorder = hv_GenParamValue.TupleSelect(
                                hv_GenParamIndex);
                        }
                        hv_RightBorder.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_RightBorder = hv_GenParamValue.TupleSelect(
                                hv_GenParamIndex);
                        }
                        hv_UpperBorder.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_UpperBorder = hv_GenParamValue.TupleSelect(
                                hv_GenParamIndex);
                        }
                        hv_LowerBorder.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_LowerBorder = hv_GenParamValue.TupleSelect(
                                hv_GenParamIndex);
                        }
                        //
                        //Set 'margin_left'
                    }
                    else if ((int)(new HTuple(((hv_GenParamName.TupleSelect(hv_GenParamIndex))).TupleEqual(
                        "margin_left"))) != 0)
                    {
                        hv_LeftBorder.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_LeftBorder = hv_GenParamValue.TupleSelect(
                                hv_GenParamIndex);
                        }
                        //
                        //Set 'margin_right'
                    }
                    else if ((int)(new HTuple(((hv_GenParamName.TupleSelect(hv_GenParamIndex))).TupleEqual(
                        "margin_right"))) != 0)
                    {
                        hv_RightBorder.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_RightBorder = hv_GenParamValue.TupleSelect(
                                hv_GenParamIndex);
                        }
                        //
                        //Set 'margin_top'
                    }
                    else if ((int)(new HTuple(((hv_GenParamName.TupleSelect(hv_GenParamIndex))).TupleEqual(
                        "margin_top"))) != 0)
                    {
                        hv_UpperBorder.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_UpperBorder = hv_GenParamValue.TupleSelect(
                                hv_GenParamIndex);
                        }
                        //
                        //Set 'margin_bottom'
                    }
                    else if ((int)(new HTuple(((hv_GenParamName.TupleSelect(hv_GenParamIndex))).TupleEqual(
                        "margin_bottom"))) != 0)
                    {
                        hv_LowerBorder.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_LowerBorder = hv_GenParamValue.TupleSelect(
                                hv_GenParamIndex);
                        }
                    }
                    else if ((int)(new HTuple(((hv_GenParamName.TupleSelect(hv_GenParamIndex))).TupleEqual(
                        "format_x"))) != 0)
                    {
                        hv_FormatX.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_FormatX = hv_GenParamValue.TupleSelect(
                                hv_GenParamIndex);
                        }
                    }
                    else if ((int)(new HTuple(((hv_GenParamName.TupleSelect(hv_GenParamIndex))).TupleEqual(
                        "format_y"))) != 0)
                    {
                        hv_FormatY.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_FormatY = hv_GenParamValue.TupleSelect(
                                hv_GenParamIndex);
                        }
                    }
                    else
                    {
                        throw new HalconException(("Unknown generic parameter: '" + (hv_GenParamName.TupleSelect(
                            hv_GenParamIndex))) + "'");
                    }
                }
                //
                //Check consistency of start and end values
                //of the axes.
                if ((int)(new HTuple(hv_XAxisStartValue.TupleGreater(hv_XAxisEndValue))) != 0)
                {
                    throw new HalconException("Value for 'start_x' is greater than value for 'end_x'");
                }
                if ((int)(new HTuple(hv_YAxisStartValue.TupleGreater(hv_YAxisEndValue))) != 0)
                {
                    throw new HalconException("Value for 'start_y' is greater than value for 'end_y'");
                }
                //
                //Set the position of the y-axis.
                if ((int)(new HTuple(hv_YAxisPosition.TupleEqual("default"))) != 0)
                {
                    hv_YAxisPosition.Dispose();
                    hv_YAxisPosition = new HTuple(hv_XAxisStartValue);
                }
                if ((int)(new HTuple(((hv_YAxisPosition.TupleIsString())).TupleEqual(1))) != 0)
                {
                    if ((int)(new HTuple(hv_YAxisPosition.TupleEqual("left"))) != 0)
                    {
                        hv_YAxisPosition.Dispose();
                        hv_YAxisPosition = new HTuple(hv_XAxisStartValue);
                    }
                    else if ((int)(new HTuple(hv_YAxisPosition.TupleEqual("right"))) != 0)
                    {
                        hv_YAxisPosition.Dispose();
                        hv_YAxisPosition = new HTuple(hv_XAxisEndValue);
                    }
                    else if ((int)(new HTuple(hv_YAxisPosition.TupleEqual("origin"))) != 0)
                    {
                        hv_YAxisPosition.Dispose();
                        hv_YAxisPosition = 0;
                    }
                    else
                    {
                        throw new HalconException(("Unsupported axis_location_y: '" + hv_YAxisPosition) + "'");
                    }
                }
                //Set the position of the ticks on the y-axis
                //depending of the location of the y-axis.
                if ((int)(new HTuple((new HTuple(((hv_XAxisStartValue.TupleConcat(hv_XAxisEndValue))).TupleMean()
                    )).TupleGreater(hv_YAxisPosition))) != 0)
                {
                    hv_YTickDirection.Dispose();
                    hv_YTickDirection = "right";
                }
                else
                {
                    hv_YTickDirection.Dispose();
                    hv_YTickDirection = "left";
                }
                //
                //Set the position of the x-axis.
                if ((int)(new HTuple(hv_XAxisPosition.TupleEqual("default"))) != 0)
                {
                    hv_XAxisPosition.Dispose();
                    hv_XAxisPosition = new HTuple(hv_YAxisStartValue);
                }
                if ((int)(new HTuple(((hv_XAxisPosition.TupleIsString())).TupleEqual(1))) != 0)
                {
                    if ((int)(new HTuple(hv_XAxisPosition.TupleEqual("bottom"))) != 0)
                    {
                        hv_XAxisPosition.Dispose();
                        hv_XAxisPosition = new HTuple(hv_YAxisStartValue);
                    }
                    else if ((int)(new HTuple(hv_XAxisPosition.TupleEqual("top"))) != 0)
                    {
                        hv_XAxisPosition.Dispose();
                        hv_XAxisPosition = new HTuple(hv_YAxisEndValue);
                    }
                    else if ((int)(new HTuple(hv_XAxisPosition.TupleEqual("origin"))) != 0)
                    {
                        hv_XAxisPosition.Dispose();
                        hv_XAxisPosition = 0;
                    }
                    else
                    {
                        throw new HalconException(("Unsupported axis_location_x: '" + hv_XAxisPosition) + "'");
                    }
                }
                //Set the position of the ticks on the y-axis
                //depending of the location of the y-axis.
                if ((int)(new HTuple((new HTuple(((hv_YAxisStartValue.TupleConcat(hv_YAxisEndValue))).TupleMean()
                    )).TupleGreater(hv_XAxisPosition))) != 0)
                {
                    hv_XTickDirection.Dispose();
                    hv_XTickDirection = "up";
                }
                else
                {
                    hv_XTickDirection.Dispose();
                    hv_XTickDirection = "down";
                }
                //
                //Calculate basic pixel coordinates and scale factors
                //
                hv_XAxisWidthPx.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_XAxisWidthPx = (hv_Width - hv_LeftBorder) - hv_RightBorder;
                }
                hv_XAxisWidth.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_XAxisWidth = hv_XAxisEndValue - hv_XAxisStartValue;
                }
                if ((int)(new HTuple(hv_XAxisWidth.TupleEqual(0))) != 0)
                {
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        {
                            HTuple
                              ExpTmpLocalVar_XAxisStartValue = hv_XAxisStartValue - 0.5;
                            hv_XAxisStartValue.Dispose();
                            hv_XAxisStartValue = ExpTmpLocalVar_XAxisStartValue;
                        }
                    }
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        {
                            HTuple
                              ExpTmpLocalVar_XAxisEndValue = hv_XAxisEndValue + 0.5;
                            hv_XAxisEndValue.Dispose();
                            hv_XAxisEndValue = ExpTmpLocalVar_XAxisEndValue;
                        }
                    }
                    hv_XAxisWidth.Dispose();
                    hv_XAxisWidth = 1;
                }
                hv_XScaleFactor.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_XScaleFactor = hv_XAxisWidthPx / (hv_XAxisWidth.TupleReal()
                        );
                }
                hv_YAxisHeightPx.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_YAxisHeightPx = (hv_Height - hv_LowerBorder) - hv_UpperBorder;
                }
                hv_YAxisHeight.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_YAxisHeight = hv_YAxisEndValue - hv_YAxisStartValue;
                }
                if ((int)(new HTuple(hv_YAxisHeight.TupleEqual(0))) != 0)
                {
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        {
                            HTuple
                              ExpTmpLocalVar_YAxisStartValue = hv_YAxisStartValue - 0.5;
                            hv_YAxisStartValue.Dispose();
                            hv_YAxisStartValue = ExpTmpLocalVar_YAxisStartValue;
                        }
                    }
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        {
                            HTuple
                              ExpTmpLocalVar_YAxisEndValue = hv_YAxisEndValue + 0.5;
                            hv_YAxisEndValue.Dispose();
                            hv_YAxisEndValue = ExpTmpLocalVar_YAxisEndValue;
                        }
                    }
                    hv_YAxisHeight.Dispose();
                    hv_YAxisHeight = 1;
                }
                hv_YScaleFactor.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_YScaleFactor = hv_YAxisHeightPx / (hv_YAxisHeight.TupleReal()
                        );
                }
                hv_YAxisOffsetPx.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_YAxisOffsetPx = (hv_YAxisPosition - hv_XAxisStartValue) * hv_XScaleFactor;
                }
                hv_XAxisOffsetPx.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_XAxisOffsetPx = (hv_XAxisPosition - hv_YAxisStartValue) * hv_YScaleFactor;
                }
                //
                //Display grid lines
                //
                if ((int)(new HTuple(hv_GridColor.TupleNotEqual("none"))) != 0)
                {
                    hv_DotStyle.Dispose();
                    hv_DotStyle = new HTuple();
                    hv_DotStyle[0] = 5;
                    hv_DotStyle[1] = 7;
                    HOperatorSet.SetLineStyle(hv_WindowHandle, hv_DotStyle);
                    if (HDevWindowStack.IsOpen())
                    {
                        HOperatorSet.SetColor(HDevWindowStack.GetActive(), hv_GridColor);
                    }
                    //
                    //Display x grid lines
                    if ((int)(new HTuple(hv_XGrid.TupleNotEqual("none"))) != 0)
                    {
                        if ((int)(new HTuple(hv_XGrid.TupleEqual("min_max_origin"))) != 0)
                        {
                            //Calculate 'min_max_origin' grid line coordinates
                            if ((int)(new HTuple(hv_YAxisPosition.TupleEqual(hv_XAxisStartValue))) != 0)
                            {
                                hv_XGridValues.Dispose();
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    hv_XGridValues = new HTuple();
                                    hv_XGridValues = hv_XGridValues.TupleConcat(hv_XAxisStartValue, hv_XAxisEndValue);
                                }
                            }
                            else
                            {
                                hv_XGridValues.Dispose();
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    hv_XGridValues = new HTuple();
                                    hv_XGridValues = hv_XGridValues.TupleConcat(hv_XAxisStartValue, hv_YAxisPosition, hv_XAxisEndValue);
                                }
                            }
                        }
                        else
                        {
                            //Calculate equidistant grid line coordinates
                            hv_XGridStart.Dispose();
                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                            {
                                hv_XGridStart = (((hv_XAxisStartValue / hv_XGrid)).TupleCeil()
                                    ) * hv_XGrid;
                            }
                            hv_XGridValues.Dispose();
                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                            {
                                hv_XGridValues = HTuple.TupleGenSequence(
                                    hv_XGridStart, hv_XAxisEndValue, hv_XGrid);
                            }
                        }
                        hv_XCoord.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_XCoord = (hv_XGridValues - hv_XAxisStartValue) * hv_XScaleFactor;
                        }
                        //Generate and display grid lines
                        for (hv_IndexGrid = 0; (int)hv_IndexGrid <= (int)((new HTuple(hv_XGridValues.TupleLength()
                            )) - 1); hv_IndexGrid = (int)hv_IndexGrid + 1)
                        {
                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                            {
                                ho_ContourXGrid.Dispose();
                                HOperatorSet.GenContourPolygonXld(out ho_ContourXGrid, ((hv_Height - hv_LowerBorder)).TupleConcat(
                                    hv_UpperBorder), ((hv_LeftBorder + (hv_XCoord.TupleSelect(hv_IndexGrid)))).TupleConcat(
                                    hv_LeftBorder + (hv_XCoord.TupleSelect(hv_IndexGrid))));
                            }
                            if (HDevWindowStack.IsOpen())
                            {
                                HOperatorSet.DispObj(ho_ContourXGrid, HDevWindowStack.GetActive());
                            }
                        }
                    }
                    //
                    //Display y grid lines
                    if ((int)(new HTuple(hv_YGrid.TupleNotEqual("none"))) != 0)
                    {
                        if ((int)(new HTuple(hv_YGrid.TupleEqual("min_max_origin"))) != 0)
                        {
                            //Calculate 'min_max_origin' grid line coordinates
                            if ((int)(new HTuple(hv_XAxisPosition.TupleEqual(hv_YAxisStartValue))) != 0)
                            {
                                hv_YGridValues.Dispose();
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    hv_YGridValues = new HTuple();
                                    hv_YGridValues = hv_YGridValues.TupleConcat(hv_YAxisStartValue, hv_YAxisEndValue);
                                }
                            }
                            else
                            {
                                hv_YGridValues.Dispose();
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    hv_YGridValues = new HTuple();
                                    hv_YGridValues = hv_YGridValues.TupleConcat(hv_YAxisStartValue, hv_XAxisPosition, hv_YAxisEndValue);
                                }
                            }
                        }
                        else
                        {
                            //Calculate equidistant grid line coordinates
                            hv_YGridStart.Dispose();
                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                            {
                                hv_YGridStart = (((hv_YAxisStartValue / hv_YGrid)).TupleCeil()
                                    ) * hv_YGrid;
                            }
                            hv_YGridValues.Dispose();
                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                            {
                                hv_YGridValues = HTuple.TupleGenSequence(
                                    hv_YGridStart, hv_YAxisEndValue, hv_YGrid);
                            }
                        }
                        hv_YCoord.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_YCoord = (hv_YGridValues - hv_YAxisStartValue) * hv_YScaleFactor;
                        }
                        //Generate and display grid lines
                        for (hv_IndexGrid = 0; (int)hv_IndexGrid <= (int)((new HTuple(hv_YGridValues.TupleLength()
                            )) - 1); hv_IndexGrid = (int)hv_IndexGrid + 1)
                        {
                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                            {
                                ho_ContourYGrid.Dispose();
                                HOperatorSet.GenContourPolygonXld(out ho_ContourYGrid, (((hv_Height - hv_LowerBorder) - (hv_YCoord.TupleSelect(
                                    hv_IndexGrid)))).TupleConcat((hv_Height - hv_LowerBorder) - (hv_YCoord.TupleSelect(
                                    hv_IndexGrid))), hv_LeftBorder.TupleConcat(hv_Width - hv_RightBorder));
                            }
                            if (HDevWindowStack.IsOpen())
                            {
                                HOperatorSet.DispObj(ho_ContourYGrid, HDevWindowStack.GetActive());
                            }
                        }
                    }
                }
                HOperatorSet.SetLineStyle(hv_WindowHandle, new HTuple());
                //
                //
                //Display the coordinate system axes
                if ((int)(new HTuple(hv_AxesColor.TupleNotEqual("none"))) != 0)
                {
                    //Display axes
                    if (HDevWindowStack.IsOpen())
                    {
                        HOperatorSet.SetColor(HDevWindowStack.GetActive(), hv_AxesColor);
                    }
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        ho_XArrow.Dispose();
                        gen_arrow_contour_xld(out ho_XArrow, (hv_Height - hv_LowerBorder) - hv_XAxisOffsetPx,
                            hv_LeftBorder, (hv_Height - hv_LowerBorder) - hv_XAxisOffsetPx, hv_Width - hv_RightBorder,
                            0, 0);
                    }
                    if (HDevWindowStack.IsOpen())
                    {
                        HOperatorSet.DispObj(ho_XArrow, HDevWindowStack.GetActive());
                    }
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        ho_YArrow.Dispose();
                        gen_arrow_contour_xld(out ho_YArrow, hv_Height - hv_LowerBorder, hv_LeftBorder + hv_YAxisOffsetPx,
                            hv_UpperBorder, hv_LeftBorder + hv_YAxisOffsetPx, 0, 0);
                    }
                    if (HDevWindowStack.IsOpen())
                    {
                        HOperatorSet.DispObj(ho_YArrow, HDevWindowStack.GetActive());
                    }
                    //Display labels
                    hv_Ascent.Dispose(); hv_Descent.Dispose(); hv_TextWidthXLabel.Dispose(); hv_TextHeightXLabel.Dispose();
                    HOperatorSet.GetStringExtents(hv_WindowHandle, hv_XLabel, out hv_Ascent,
                        out hv_Descent, out hv_TextWidthXLabel, out hv_TextHeightXLabel);
                    hv_Ascent.Dispose(); hv_Descent.Dispose(); hv_TextWidthYLabel.Dispose(); hv_TextHeightYLabel.Dispose();
                    HOperatorSet.GetStringExtents(hv_WindowHandle, hv_YLabel, out hv_Ascent,
                        out hv_Descent, out hv_TextWidthYLabel, out hv_TextHeightYLabel);
                    if ((int)(new HTuple(hv_YTickDirection.TupleEqual("right"))) != 0)
                    {
                        if ((int)(new HTuple(hv_XTickDirection.TupleEqual("up"))) != 0)
                        {
                            if (HDevWindowStack.IsOpen())
                            {
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    HOperatorSet.DispText(HDevWindowStack.GetActive(), hv_XLabel, "image",
                                        ((hv_Height - hv_LowerBorder) - hv_TextHeightXLabel) - 3, ((hv_Width - hv_RightBorder) - hv_TextWidthXLabel) - 3,
                                        hv_AxesColor, "box", "false");
                                }
                            }
                            if (HDevWindowStack.IsOpen())
                            {
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    HOperatorSet.DispText(HDevWindowStack.GetActive(), " " + hv_YLabel, "image",
                                        hv_UpperBorder, (hv_LeftBorder + 3) + hv_YAxisOffsetPx, hv_AxesColor,
                                        "box", "false");
                                }
                            }
                        }
                        else
                        {
                            if (HDevWindowStack.IsOpen())
                            {
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    HOperatorSet.DispText(HDevWindowStack.GetActive(), hv_XLabel, "image",
                                        ((hv_Height - hv_LowerBorder) + 3) - hv_XAxisOffsetPx, ((hv_Width - hv_RightBorder) - hv_TextWidthXLabel) - 3,
                                        hv_AxesColor, "box", "false");
                                }
                            }
                            if (HDevWindowStack.IsOpen())
                            {
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    HOperatorSet.DispText(HDevWindowStack.GetActive(), " " + hv_YLabel, "image",
                                        ((hv_Height - hv_LowerBorder) - hv_TextHeightXLabel) - 3, (hv_LeftBorder + 3) + hv_YAxisOffsetPx,
                                        hv_AxesColor, "box", "false");
                                }
                            }
                        }
                    }
                    else
                    {
                        if ((int)(new HTuple(hv_XTickDirection.TupleEqual("up"))) != 0)
                        {
                            if (HDevWindowStack.IsOpen())
                            {
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    HOperatorSet.DispText(HDevWindowStack.GetActive(), hv_XLabel, "image",
                                        ((hv_Height - hv_LowerBorder) - (2 * hv_TextHeightXLabel)) + 3, hv_LeftBorder - 3,
                                        hv_AxesColor, "box", "false");
                                }
                            }
                            if (HDevWindowStack.IsOpen())
                            {
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    HOperatorSet.DispText(HDevWindowStack.GetActive(), " " + hv_YLabel, "image",
                                        hv_UpperBorder, ((hv_Width - hv_RightBorder) - hv_TextWidthYLabel) - 13,
                                        hv_AxesColor, "box", "false");
                                }
                            }
                        }
                        else
                        {
                            if (HDevWindowStack.IsOpen())
                            {
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    HOperatorSet.DispText(HDevWindowStack.GetActive(), hv_XLabel, "image",
                                        ((hv_Height - hv_LowerBorder) + 3) - hv_XAxisOffsetPx, hv_LeftBorder - 3,
                                        hv_AxesColor, "box", "false");
                                }
                            }
                            if (HDevWindowStack.IsOpen())
                            {
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    HOperatorSet.DispText(HDevWindowStack.GetActive(), " " + hv_YLabel, "image",
                                        ((hv_Height - hv_LowerBorder) - hv_TextHeightXLabel) - 3, ((hv_Width - hv_RightBorder) - (2 * hv_TextWidthYLabel)) - 3,
                                        hv_AxesColor, "box", "false");
                                }
                            }
                        }
                    }
                }
                //
                //Display ticks
                //
                if ((int)(new HTuple(hv_AxesColor.TupleNotEqual("none"))) != 0)
                {
                    if (HDevWindowStack.IsOpen())
                    {
                        HOperatorSet.SetColor(HDevWindowStack.GetActive(), hv_AxesColor);
                    }
                    if ((int)(new HTuple(hv_XTicks.TupleNotEqual("none"))) != 0)
                    {
                        //
                        //Display x ticks
                        if ((int)(hv_XValuesAreStrings) != 0)
                        {
                            //Display string XValues as categories
                            hv_XTicks.Dispose();
                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                            {
                                hv_XTicks = (new HTuple(hv_XValues_COPY_INP_TMP.TupleLength()
                                    )) / (new HTuple(hv_XTickValues.TupleLength()));
                            }
                            hv_XCoord.Dispose();
                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                            {
                                hv_XCoord = (hv_XValues_COPY_INP_TMP - hv_XAxisStartValue) * hv_XScaleFactor;
                            }
                        }
                        else
                        {
                            //Display tick values
                            if ((int)(new HTuple(hv_XTicks.TupleEqual("min_max_origin"))) != 0)
                            {
                                //Calculate 'min_max_origin' tick coordinates
                                if ((int)(new HTuple(hv_YAxisPosition.TupleEqual(hv_XAxisStartValue))) != 0)
                                {
                                    hv_XTickValues.Dispose();
                                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                    {
                                        hv_XTickValues = new HTuple();
                                        hv_XTickValues = hv_XTickValues.TupleConcat(hv_XAxisStartValue, hv_XAxisEndValue);
                                    }
                                }
                                else
                                {
                                    hv_XTickValues.Dispose();
                                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                    {
                                        hv_XTickValues = new HTuple();
                                        hv_XTickValues = hv_XTickValues.TupleConcat(hv_XAxisStartValue, hv_YAxisPosition, hv_XAxisEndValue);
                                    }
                                }
                            }
                            else
                            {
                                //Calculate equidistant tick coordinates
                                hv_XTickStart.Dispose();
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    hv_XTickStart = (((hv_XAxisStartValue / hv_XTicks)).TupleCeil()
                                        ) * hv_XTicks;
                                }
                                hv_XTickValues.Dispose();
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    hv_XTickValues = HTuple.TupleGenSequence(
                                        hv_XTickStart, hv_XAxisEndValue, hv_XTicks);
                                }
                            }
                            //Remove ticks that are smaller than the x-axis start.
                            hv_Indices.Dispose();
                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                            {
                                hv_Indices = ((hv_XTickValues.TupleLessElem(
                                    hv_XAxisStartValue))).TupleFind(1);
                            }
                            hv_XCoord.Dispose();
                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                            {
                                hv_XCoord = (hv_XTickValues - hv_XAxisStartValue) * hv_XScaleFactor;
                            }
                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                            {
                                {
                                    HTuple
                                      ExpTmpLocalVar_XCoord = hv_XCoord.TupleRemove(
                                        hv_Indices);
                                    hv_XCoord.Dispose();
                                    hv_XCoord = ExpTmpLocalVar_XCoord;
                                }
                            }
                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                            {
                                {
                                    HTuple
                                      ExpTmpLocalVar_XTickValues = hv_XTickValues.TupleRemove(
                                        hv_Indices);
                                    hv_XTickValues.Dispose();
                                    hv_XTickValues = ExpTmpLocalVar_XTickValues;
                                }
                            }
                            //
                            if ((int)(new HTuple(hv_FormatX.TupleEqual("default"))) != 0)
                            {
                                hv_TypeTicks.Dispose();
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    hv_TypeTicks = hv_XTicks.TupleType()
                                        ;
                                }
                                if ((int)(new HTuple(hv_TypeTicks.TupleEqual(4))) != 0)
                                {
                                    //String ('min_max_origin')
                                    //Format depends on actual values
                                    hv_TypeTicks.Dispose();
                                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                    {
                                        hv_TypeTicks = hv_XTickValues.TupleType()
                                            ;
                                    }
                                }
                                if ((int)(new HTuple(hv_TypeTicks.TupleEqual(1))) != 0)
                                {
                                    //Round to integer
                                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                    {
                                        {
                                            HTuple
                                              ExpTmpLocalVar_XTickValues = hv_XTickValues.TupleInt()
                                                ;
                                            hv_XTickValues.Dispose();
                                            hv_XTickValues = ExpTmpLocalVar_XTickValues;
                                        }
                                    }
                                }
                                else
                                {
                                    //Use floating point numbers
                                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                    {
                                        {
                                            HTuple
                                              ExpTmpLocalVar_XTickValues = hv_XTickValues.TupleString(
                                                ".2f");
                                            hv_XTickValues.Dispose();
                                            hv_XTickValues = ExpTmpLocalVar_XTickValues;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    {
                                        HTuple
                                          ExpTmpLocalVar_XTickValues = hv_XTickValues.TupleString(
                                            hv_FormatX);
                                        hv_XTickValues.Dispose();
                                        hv_XTickValues = ExpTmpLocalVar_XTickValues;
                                    }
                                }
                            }
                        }
                        //Generate and display ticks
                        for (hv_IndexTicks = 0; (int)hv_IndexTicks <= (int)((new HTuple(hv_XTickValues.TupleLength()
                            )) - 1); hv_IndexTicks = (int)hv_IndexTicks + 1)
                        {
                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                            {
                                hv_Ascent1.Dispose(); hv_Descent1.Dispose(); hv_TextWidthXTicks.Dispose(); hv_TextHeightXTicks.Dispose();
                                HOperatorSet.GetStringExtents(hv_WindowHandle, hv_XTickValues.TupleSelect(
                                    hv_IndexTicks), out hv_Ascent1, out hv_Descent1, out hv_TextWidthXTicks,
                                    out hv_TextHeightXTicks);
                            }
                            if ((int)(new HTuple(hv_XTickDirection.TupleEqual("up"))) != 0)
                            {
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    ho_ContourXTick.Dispose();
                                    HOperatorSet.GenContourPolygonXld(out ho_ContourXTick, (((hv_Height - hv_LowerBorder) - hv_XAxisOffsetPx)).TupleConcat(
                                        ((hv_Height - hv_LowerBorder) - hv_XAxisOffsetPx) - 5), ((hv_LeftBorder + (hv_XCoord.TupleSelect(
                                        hv_IndexTicks)))).TupleConcat(hv_LeftBorder + (hv_XCoord.TupleSelect(
                                        hv_IndexTicks))));
                                }
                                if (HDevWindowStack.IsOpen())
                                {
                                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                    {
                                        HOperatorSet.DispText(HDevWindowStack.GetActive(), hv_XTickValues.TupleSelect(
                                            hv_IndexTicks), "image", ((hv_Height - hv_LowerBorder) + 2) - hv_XAxisOffsetPx,
                                            hv_LeftBorder + (hv_XCoord.TupleSelect(hv_IndexTicks)), hv_AxesColor,
                                            "box", "false");
                                    }
                                }
                            }
                            else
                            {
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    ho_ContourXTick.Dispose();
                                    HOperatorSet.GenContourPolygonXld(out ho_ContourXTick, ((((hv_Height - hv_LowerBorder) - hv_XAxisOffsetPx) + 5)).TupleConcat(
                                        (hv_Height - hv_LowerBorder) - hv_XAxisOffsetPx), ((hv_LeftBorder + (hv_XCoord.TupleSelect(
                                        hv_IndexTicks)))).TupleConcat(hv_LeftBorder + (hv_XCoord.TupleSelect(
                                        hv_IndexTicks))));
                                }
                                if (HDevWindowStack.IsOpen())
                                {
                                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                    {
                                        HOperatorSet.DispText(HDevWindowStack.GetActive(), hv_XTickValues.TupleSelect(
                                            hv_IndexTicks), "image", ((hv_Height - hv_LowerBorder) - (2 * hv_TextHeightXTicks)) - hv_XAxisOffsetPx,
                                            hv_LeftBorder + (hv_XCoord.TupleSelect(hv_IndexTicks)), hv_AxesColor,
                                            "box", "false");
                                    }
                                }
                            }
                            if (HDevWindowStack.IsOpen())
                            {
                                HOperatorSet.DispObj(ho_ContourXTick, HDevWindowStack.GetActive());
                            }
                        }
                    }
                    //
                    if ((int)(new HTuple(hv_YTicks.TupleNotEqual("none"))) != 0)
                    {
                        //
                        //Display y ticks
                        if ((int)(new HTuple(hv_YTicks.TupleEqual("min_max_origin"))) != 0)
                        {
                            //Calculate 'min_max_origin' tick coordinates
                            if ((int)(new HTuple(hv_XAxisPosition.TupleEqual(hv_YAxisStartValue))) != 0)
                            {
                                hv_YTickValues.Dispose();
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    hv_YTickValues = new HTuple();
                                    hv_YTickValues = hv_YTickValues.TupleConcat(hv_YAxisStartValue, hv_YAxisEndValue);
                                }
                            }
                            else
                            {
                                hv_YTickValues.Dispose();
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    hv_YTickValues = new HTuple();
                                    hv_YTickValues = hv_YTickValues.TupleConcat(hv_YAxisStartValue, hv_XAxisPosition, hv_YAxisEndValue);
                                }
                            }
                        }
                        else
                        {
                            //Calculate equidistant tick coordinates
                            hv_YTickStart.Dispose();
                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                            {
                                hv_YTickStart = (((hv_YAxisStartValue / hv_YTicks)).TupleCeil()
                                    ) * hv_YTicks;
                            }
                            hv_YTickValues.Dispose();
                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                            {
                                hv_YTickValues = HTuple.TupleGenSequence(
                                    hv_YTickStart, hv_YAxisEndValue, hv_YTicks);
                            }
                        }
                        //Remove ticks that are smaller than the y-axis start.
                        hv_Indices.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_Indices = ((hv_YTickValues.TupleLessElem(
                                hv_YAxisStartValue))).TupleFind(1);
                        }
                        hv_YCoord.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_YCoord = (hv_YTickValues - hv_YAxisStartValue) * hv_YScaleFactor;
                        }
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            {
                                HTuple
                                  ExpTmpLocalVar_YCoord = hv_YCoord.TupleRemove(
                                    hv_Indices);
                                hv_YCoord.Dispose();
                                hv_YCoord = ExpTmpLocalVar_YCoord;
                            }
                        }
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            {
                                HTuple
                                  ExpTmpLocalVar_YTickValues = hv_YTickValues.TupleRemove(
                                    hv_Indices);
                                hv_YTickValues.Dispose();
                                hv_YTickValues = ExpTmpLocalVar_YTickValues;
                            }
                        }
                        //
                        if ((int)(new HTuple(hv_FormatY.TupleEqual("default"))) != 0)
                        {
                            hv_TypeTicks.Dispose();
                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                            {
                                hv_TypeTicks = hv_YTicks.TupleType()
                                    ;
                            }
                            if ((int)(new HTuple(hv_TypeTicks.TupleEqual(4))) != 0)
                            {
                                //String ('min_max_origin')
                                //Format depends on actual values
                                hv_TypeTicks.Dispose();
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    hv_TypeTicks = hv_YTickValues.TupleType()
                                        ;
                                }
                            }
                            if ((int)(new HTuple(hv_TypeTicks.TupleEqual(1))) != 0)
                            {
                                //Round to integer
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    {
                                        HTuple
                                          ExpTmpLocalVar_YTickValues = hv_YTickValues.TupleInt()
                                            ;
                                        hv_YTickValues.Dispose();
                                        hv_YTickValues = ExpTmpLocalVar_YTickValues;
                                    }
                                }
                            }
                            else
                            {
                                //Use floating point numbers
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    {
                                        HTuple
                                          ExpTmpLocalVar_YTickValues = hv_YTickValues.TupleString(
                                            ".2f");
                                        hv_YTickValues.Dispose();
                                        hv_YTickValues = ExpTmpLocalVar_YTickValues;
                                    }
                                }
                            }
                        }
                        else
                        {
                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                            {
                                {
                                    HTuple
                                      ExpTmpLocalVar_YTickValues = hv_YTickValues.TupleString(
                                        hv_FormatY);
                                    hv_YTickValues.Dispose();
                                    hv_YTickValues = ExpTmpLocalVar_YTickValues;
                                }
                            }
                        }
                        //Generate and display ticks
                        for (hv_IndexTicks = 0; (int)hv_IndexTicks <= (int)((new HTuple(hv_YTickValues.TupleLength()
                            )) - 1); hv_IndexTicks = (int)hv_IndexTicks + 1)
                        {
                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                            {
                                hv_Ascent1.Dispose(); hv_Descent1.Dispose(); hv_TextWidthYTicks.Dispose(); hv_TextHeightYTicks.Dispose();
                                HOperatorSet.GetStringExtents(hv_WindowHandle, hv_YTickValues.TupleSelect(
                                    hv_IndexTicks), out hv_Ascent1, out hv_Descent1, out hv_TextWidthYTicks,
                                    out hv_TextHeightYTicks);
                            }
                            if ((int)(new HTuple(hv_YTickDirection.TupleEqual("right"))) != 0)
                            {
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    ho_ContourYTick.Dispose();
                                    HOperatorSet.GenContourPolygonXld(out ho_ContourYTick, (((hv_Height - hv_LowerBorder) - (hv_YCoord.TupleSelect(
                                        hv_IndexTicks)))).TupleConcat((hv_Height - hv_LowerBorder) - (hv_YCoord.TupleSelect(
                                        hv_IndexTicks))), ((hv_LeftBorder + hv_YAxisOffsetPx)).TupleConcat(
                                        (hv_LeftBorder + hv_YAxisOffsetPx) + 5));
                                }
                                if (HDevWindowStack.IsOpen())
                                {
                                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                    {
                                        HOperatorSet.DispText(HDevWindowStack.GetActive(), hv_YTickValues.TupleSelect(
                                            hv_IndexTicks), "image", (((hv_Height - hv_LowerBorder) - hv_TextHeightYTicks) + 3) - (hv_YCoord.TupleSelect(
                                            hv_IndexTicks)), ((hv_LeftBorder - hv_TextWidthYTicks) - 2) + hv_YAxisOffsetPx,
                                            hv_AxesColor, "box", "false");
                                    }
                                }
                            }
                            else
                            {
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    ho_ContourYTick.Dispose();
                                    HOperatorSet.GenContourPolygonXld(out ho_ContourYTick, (((hv_Height - hv_LowerBorder) - (hv_YCoord.TupleSelect(
                                        hv_IndexTicks)))).TupleConcat((hv_Height - hv_LowerBorder) - (hv_YCoord.TupleSelect(
                                        hv_IndexTicks))), (((hv_LeftBorder + hv_YAxisOffsetPx) - 5)).TupleConcat(
                                        hv_LeftBorder + hv_YAxisOffsetPx));
                                }
                                if (HDevWindowStack.IsOpen())
                                {
                                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                    {
                                        HOperatorSet.DispText(HDevWindowStack.GetActive(), hv_YTickValues.TupleSelect(
                                            hv_IndexTicks), "image", (((hv_Height - hv_LowerBorder) - hv_TextHeightYTicks) + 3) - (hv_YCoord.TupleSelect(
                                            hv_IndexTicks)), (hv_LeftBorder + 2) + hv_YAxisOffsetPx, hv_AxesColor,
                                            "box", "false");
                                    }
                                }
                            }
                            if (HDevWindowStack.IsOpen())
                            {
                                HOperatorSet.DispObj(ho_ContourYTick, HDevWindowStack.GetActive());
                            }
                        }
                    }
                }
                //
                //Display function plot
                //
                if ((int)(new HTuple(hv_Color.TupleNotEqual("none"))) != 0)
                {
                    if ((int)((new HTuple(hv_XValues_COPY_INP_TMP.TupleNotEqual(new HTuple()))).TupleAnd(
                        new HTuple(hv_YValues_COPY_INP_TMP.TupleNotEqual(new HTuple())))) != 0)
                    {
                        hv_Num.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_Num = (new HTuple(hv_YValues_COPY_INP_TMP.TupleLength()
                                )) / (new HTuple(hv_XValues_COPY_INP_TMP.TupleLength()));
                        }
                        //
                        //Iterate over all functions to be displayed
                        HTuple end_val576 = hv_Num - 1;
                        HTuple step_val576 = 1;
                        for (hv_I = 0; hv_I.Continue(end_val576, step_val576); hv_I = hv_I.TupleAdd(step_val576))
                        {
                            //Select y values for current function
                            hv_YSelected.Dispose();
                            using (HDevDisposeHelper dh = new HDevDisposeHelper())
                            {
                                hv_YSelected = hv_YValues_COPY_INP_TMP.TupleSelectRange(
                                    hv_I * (new HTuple(hv_XValues_COPY_INP_TMP.TupleLength())), ((hv_I + 1) * (new HTuple(hv_XValues_COPY_INP_TMP.TupleLength()
                                    ))) - 1);
                            }
                            //Set color
                            if ((int)(new HTuple(hv_Color.TupleEqual(new HTuple()))) != 0)
                            {
                                HOperatorSet.SetRgb(hv_WindowHandle, hv_Red, hv_Green, hv_Blue);
                            }
                            else
                            {
                                if (HDevWindowStack.IsOpen())
                                {
                                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                    {
                                        HOperatorSet.SetColor(HDevWindowStack.GetActive(), hv_Color.TupleSelect(
                                            hv_I % (new HTuple(hv_Color.TupleLength()))));
                                    }
                                }
                            }
                            //
                            //Display in different styles
                            //
                            if ((int)((new HTuple(hv_Style.TupleEqual("line"))).TupleOr(new HTuple(hv_Style.TupleEqual(
                                new HTuple())))) != 0)
                            {
                                //Line
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    ho_Contour.Dispose();
                                    HOperatorSet.GenContourPolygonXld(out ho_Contour, ((hv_Height - hv_LowerBorder) - (hv_YSelected * hv_YScaleFactor)) + (hv_YAxisStartValue * hv_YScaleFactor),
                                        ((hv_XValues_COPY_INP_TMP * hv_XScaleFactor) + hv_LeftBorder) - (hv_XAxisStartValue * hv_XScaleFactor));
                                }
                                //Clip, if necessary
                                if ((int)(new HTuple(hv_Clip.TupleEqual("yes"))) != 0)
                                {
                                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                    {
                                        HObject ExpTmpOutVar_0;
                                        HOperatorSet.ClipContoursXld(ho_Contour, out ExpTmpOutVar_0, hv_UpperBorder,
                                            hv_LeftBorder, hv_Height - hv_LowerBorder, hv_Width - hv_RightBorder);
                                        ho_Contour.Dispose();
                                        ho_Contour = ExpTmpOutVar_0;
                                    }
                                }
                                if (HDevWindowStack.IsOpen())
                                {
                                    HOperatorSet.DispObj(ho_Contour, HDevWindowStack.GetActive());
                                }
                            }
                            else if ((int)(new HTuple(hv_Style.TupleEqual("cross"))) != 0)
                            {
                                //Cross
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    ho_Cross.Dispose();
                                    HOperatorSet.GenCrossContourXld(out ho_Cross, ((hv_Height - hv_LowerBorder) - (hv_YSelected * hv_YScaleFactor)) + (hv_YAxisStartValue * hv_YScaleFactor),
                                        ((hv_XValues_COPY_INP_TMP * hv_XScaleFactor) + hv_LeftBorder) - (hv_XAxisStartValue * hv_XScaleFactor),
                                        6, 0.785398);
                                }
                                //Clip, if necessary
                                if ((int)(new HTuple(hv_Clip.TupleEqual("yes"))) != 0)
                                {
                                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                    {
                                        HObject ExpTmpOutVar_0;
                                        HOperatorSet.ClipContoursXld(ho_Cross, out ExpTmpOutVar_0, hv_UpperBorder,
                                            hv_LeftBorder, hv_Height - hv_LowerBorder, hv_Width - hv_RightBorder);
                                        ho_Cross.Dispose();
                                        ho_Cross = ExpTmpOutVar_0;
                                    }
                                }
                                if (HDevWindowStack.IsOpen())
                                {
                                    HOperatorSet.DispObj(ho_Cross, HDevWindowStack.GetActive());
                                }
                            }
                            else if ((int)(new HTuple(hv_Style.TupleEqual("filled"))) != 0)
                            {
                                //Filled
                                hv_Y1Selected.Dispose();
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    hv_Y1Selected = new HTuple();
                                    hv_Y1Selected = hv_Y1Selected.TupleConcat(0 + hv_XAxisPosition);
                                    hv_Y1Selected = hv_Y1Selected.TupleConcat(hv_YSelected);
                                    hv_Y1Selected = hv_Y1Selected.TupleConcat(0 + hv_XAxisPosition);
                                }
                                hv_X1Selected.Dispose();
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    hv_X1Selected = new HTuple();
                                    hv_X1Selected = hv_X1Selected.TupleConcat(hv_XValues_COPY_INP_TMP.TupleMin()
                                        );
                                    hv_X1Selected = hv_X1Selected.TupleConcat(hv_XValues_COPY_INP_TMP);
                                    hv_X1Selected = hv_X1Selected.TupleConcat(hv_XValues_COPY_INP_TMP.TupleMax()
                                        );
                                }
                                if (HDevWindowStack.IsOpen())
                                {
                                    HOperatorSet.SetDraw(HDevWindowStack.GetActive(), "fill");
                                }
                                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                {
                                    ho_Filled.Dispose();
                                    HOperatorSet.GenRegionPolygonFilled(out ho_Filled, ((hv_Height - hv_LowerBorder) - (hv_Y1Selected * hv_YScaleFactor)) + (hv_YAxisStartValue * hv_YScaleFactor),
                                        ((hv_X1Selected * hv_XScaleFactor) + hv_LeftBorder) - (hv_XAxisStartValue * hv_XScaleFactor));
                                }
                                //Clip, if necessary
                                if ((int)(new HTuple(hv_Clip.TupleEqual("yes"))) != 0)
                                {
                                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                    {
                                        HObject ExpTmpOutVar_0;
                                        HOperatorSet.ClipRegion(ho_Filled, out ExpTmpOutVar_0, hv_UpperBorder,
                                            hv_LeftBorder, hv_Height - hv_LowerBorder, hv_Width - hv_RightBorder);
                                        ho_Filled.Dispose();
                                        ho_Filled = ExpTmpOutVar_0;
                                    }
                                }
                                if (HDevWindowStack.IsOpen())
                                {
                                    HOperatorSet.DispObj(ho_Filled, HDevWindowStack.GetActive());
                                }
                            }
                            else if ((int)(new HTuple(hv_Style.TupleEqual("step"))) != 0)
                            {
                                ho_Stair.Dispose();
                                HOperatorSet.GenEmptyObj(out ho_Stair);
                                for (hv_Index = 0; (int)hv_Index <= (int)((new HTuple(hv_XValues_COPY_INP_TMP.TupleLength()
                                    )) - 2); hv_Index = (int)hv_Index + 1)
                                {
                                    hv_Row1.Dispose();
                                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                    {
                                        hv_Row1 = ((hv_Height - hv_LowerBorder) - ((hv_YSelected.TupleSelect(
                                            hv_Index)) * hv_YScaleFactor)) + (hv_YAxisStartValue * hv_YScaleFactor);
                                    }
                                    hv_Row2.Dispose();
                                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                    {
                                        hv_Row2 = ((hv_Height - hv_LowerBorder) - ((hv_YSelected.TupleSelect(
                                            hv_Index + 1)) * hv_YScaleFactor)) + (hv_YAxisStartValue * hv_YScaleFactor);
                                    }
                                    hv_Col1.Dispose();
                                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                    {
                                        hv_Col1 = (((hv_XValues_COPY_INP_TMP.TupleSelect(
                                            hv_Index)) * hv_XScaleFactor) + hv_LeftBorder) - (hv_XAxisStartValue * hv_XScaleFactor);
                                    }
                                    hv_Col2.Dispose();
                                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                    {
                                        hv_Col2 = (((hv_XValues_COPY_INP_TMP.TupleSelect(
                                            hv_Index + 1)) * hv_XScaleFactor) + hv_LeftBorder) - (hv_XAxisStartValue * hv_XScaleFactor);
                                    }
                                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                    {
                                        ho_StairTmp.Dispose();
                                        HOperatorSet.GenContourPolygonXld(out ho_StairTmp, ((hv_Row1.TupleConcat(
                                            hv_Row1))).TupleConcat(hv_Row2), ((hv_Col1.TupleConcat(hv_Col2))).TupleConcat(
                                            hv_Col2));
                                    }
                                    {
                                        HObject ExpTmpOutVar_0;
                                        HOperatorSet.ConcatObj(ho_Stair, ho_StairTmp, out ExpTmpOutVar_0);
                                        ho_Stair.Dispose();
                                        ho_Stair = ExpTmpOutVar_0;
                                    }
                                }
                                {
                                    HObject ExpTmpOutVar_0;
                                    HOperatorSet.UnionAdjacentContoursXld(ho_Stair, out ExpTmpOutVar_0,
                                        0.1, 0.1, "attr_keep");
                                    ho_Stair.Dispose();
                                    ho_Stair = ExpTmpOutVar_0;
                                }
                                if ((int)(new HTuple(hv_Clip.TupleEqual("yes"))) != 0)
                                {
                                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                                    {
                                        HObject ExpTmpOutVar_0;
                                        HOperatorSet.ClipRegion(ho_Stair, out ExpTmpOutVar_0, hv_UpperBorder,
                                            hv_LeftBorder, hv_Height - hv_LowerBorder, hv_Width - hv_RightBorder);
                                        ho_Stair.Dispose();
                                        ho_Stair = ExpTmpOutVar_0;
                                    }
                                }
                                if (HDevWindowStack.IsOpen())
                                {
                                    HOperatorSet.DispObj(ho_Stair, HDevWindowStack.GetActive());
                                }
                            }
                            else
                            {
                                throw new HalconException("Unsupported style: " + hv_Style);
                            }
                        }
                    }
                }
                //
                //
                //Reset original display settings
                if (HDevWindowStack.IsOpen())
                {
                    HOperatorSet.SetPart(HDevWindowStack.GetActive(), hv_PartRow1, hv_PartColumn1,
                        hv_PartRow2, hv_PartColumn2);
                }
                HDevWindowStack.SetActive(hv_PreviousWindowHandle);

                HOperatorSet.SetRgb(hv_WindowHandle, hv_Red, hv_Green, hv_Blue);
                if (HDevWindowStack.IsOpen())
                {
                    HOperatorSet.SetDraw(HDevWindowStack.GetActive(), hv_DrawMode);
                }
                HOperatorSet.SetLineStyle(hv_WindowHandle, hv_OriginStyle);
                HOperatorSet.SetSystem("clip_region", hv_ClipRegion);
                ho_ContourXGrid.Dispose();
                ho_ContourYGrid.Dispose();
                ho_XArrow.Dispose();
                ho_YArrow.Dispose();
                ho_ContourXTick.Dispose();
                ho_ContourYTick.Dispose();
                ho_Contour.Dispose();
                ho_Cross.Dispose();
                ho_Filled.Dispose();
                ho_Stair.Dispose();
                ho_StairTmp.Dispose();

                hv_XValues_COPY_INP_TMP.Dispose();
                hv_YValues_COPY_INP_TMP.Dispose();
                hv_PreviousWindowHandle.Dispose();
                hv_ClipRegion.Dispose();
                hv_Row.Dispose();
                hv_Column.Dispose();
                hv_Width.Dispose();
                hv_Height.Dispose();
                hv_PartRow1.Dispose();
                hv_PartColumn1.Dispose();
                hv_PartRow2.Dispose();
                hv_PartColumn2.Dispose();
                hv_Red.Dispose();
                hv_Green.Dispose();
                hv_Blue.Dispose();
                hv_DrawMode.Dispose();
                hv_OriginStyle.Dispose();
                hv_XAxisEndValue.Dispose();
                hv_YAxisEndValue.Dispose();
                hv_XAxisStartValue.Dispose();
                hv_YAxisStartValue.Dispose();
                hv_XValuesAreStrings.Dispose();
                hv_XTickValues.Dispose();
                hv_XTicks.Dispose();
                hv_YAxisPosition.Dispose();
                hv_XAxisPosition.Dispose();
                hv_LeftBorder.Dispose();
                hv_RightBorder.Dispose();
                hv_UpperBorder.Dispose();
                hv_LowerBorder.Dispose();
                hv_AxesColor.Dispose();
                hv_Style.Dispose();
                hv_Clip.Dispose();
                hv_YTicks.Dispose();
                hv_XGrid.Dispose();
                hv_YGrid.Dispose();
                hv_GridColor.Dispose();
                hv_YPosition.Dispose();
                hv_FormatX.Dispose();
                hv_FormatY.Dispose();
                hv_NumGenParamNames.Dispose();
                hv_NumGenParamValues.Dispose();
                hv_GenParamIndex.Dispose();
                hv_XGridTicks.Dispose();
                hv_YTickDirection.Dispose();
                hv_XTickDirection.Dispose();
                hv_XAxisWidthPx.Dispose();
                hv_XAxisWidth.Dispose();
                hv_XScaleFactor.Dispose();
                hv_YAxisHeightPx.Dispose();
                hv_YAxisHeight.Dispose();
                hv_YScaleFactor.Dispose();
                hv_YAxisOffsetPx.Dispose();
                hv_XAxisOffsetPx.Dispose();
                hv_DotStyle.Dispose();
                hv_XGridValues.Dispose();
                hv_XGridStart.Dispose();
                hv_XCoord.Dispose();
                hv_IndexGrid.Dispose();
                hv_YGridValues.Dispose();
                hv_YGridStart.Dispose();
                hv_YCoord.Dispose();
                hv_Ascent.Dispose();
                hv_Descent.Dispose();
                hv_TextWidthXLabel.Dispose();
                hv_TextHeightXLabel.Dispose();
                hv_TextWidthYLabel.Dispose();
                hv_TextHeightYLabel.Dispose();
                hv_XTickStart.Dispose();
                hv_Indices.Dispose();
                hv_TypeTicks.Dispose();
                hv_IndexTicks.Dispose();
                hv_Ascent1.Dispose();
                hv_Descent1.Dispose();
                hv_TextWidthXTicks.Dispose();
                hv_TextHeightXTicks.Dispose();
                hv_YTickValues.Dispose();
                hv_YTickStart.Dispose();
                hv_TextWidthYTicks.Dispose();
                hv_TextHeightYTicks.Dispose();
                hv_Num.Dispose();
                hv_I.Dispose();
                hv_YSelected.Dispose();
                hv_Y1Selected.Dispose();
                hv_X1Selected.Dispose();
                hv_Index.Dispose();
                hv_Row1.Dispose();
                hv_Row2.Dispose();
                hv_Col1.Dispose();
                hv_Col2.Dispose();

                return;
            }
            catch (HalconException HDevExpDefaultException)
            {
                ho_ContourXGrid.Dispose();
                ho_ContourYGrid.Dispose();
                ho_XArrow.Dispose();
                ho_YArrow.Dispose();
                ho_ContourXTick.Dispose();
                ho_ContourYTick.Dispose();
                ho_Contour.Dispose();
                ho_Cross.Dispose();
                ho_Filled.Dispose();
                ho_Stair.Dispose();
                ho_StairTmp.Dispose();

                hv_XValues_COPY_INP_TMP.Dispose();
                hv_YValues_COPY_INP_TMP.Dispose();
                hv_PreviousWindowHandle.Dispose();
                hv_ClipRegion.Dispose();
                hv_Row.Dispose();
                hv_Column.Dispose();
                hv_Width.Dispose();
                hv_Height.Dispose();
                hv_PartRow1.Dispose();
                hv_PartColumn1.Dispose();
                hv_PartRow2.Dispose();
                hv_PartColumn2.Dispose();
                hv_Red.Dispose();
                hv_Green.Dispose();
                hv_Blue.Dispose();
                hv_DrawMode.Dispose();
                hv_OriginStyle.Dispose();
                hv_XAxisEndValue.Dispose();
                hv_YAxisEndValue.Dispose();
                hv_XAxisStartValue.Dispose();
                hv_YAxisStartValue.Dispose();
                hv_XValuesAreStrings.Dispose();
                hv_XTickValues.Dispose();
                hv_XTicks.Dispose();
                hv_YAxisPosition.Dispose();
                hv_XAxisPosition.Dispose();
                hv_LeftBorder.Dispose();
                hv_RightBorder.Dispose();
                hv_UpperBorder.Dispose();
                hv_LowerBorder.Dispose();
                hv_AxesColor.Dispose();
                hv_Style.Dispose();
                hv_Clip.Dispose();
                hv_YTicks.Dispose();
                hv_XGrid.Dispose();
                hv_YGrid.Dispose();
                hv_GridColor.Dispose();
                hv_YPosition.Dispose();
                hv_FormatX.Dispose();
                hv_FormatY.Dispose();
                hv_NumGenParamNames.Dispose();
                hv_NumGenParamValues.Dispose();
                hv_GenParamIndex.Dispose();
                hv_XGridTicks.Dispose();
                hv_YTickDirection.Dispose();
                hv_XTickDirection.Dispose();
                hv_XAxisWidthPx.Dispose();
                hv_XAxisWidth.Dispose();
                hv_XScaleFactor.Dispose();
                hv_YAxisHeightPx.Dispose();
                hv_YAxisHeight.Dispose();
                hv_YScaleFactor.Dispose();
                hv_YAxisOffsetPx.Dispose();
                hv_XAxisOffsetPx.Dispose();
                hv_DotStyle.Dispose();
                hv_XGridValues.Dispose();
                hv_XGridStart.Dispose();
                hv_XCoord.Dispose();
                hv_IndexGrid.Dispose();
                hv_YGridValues.Dispose();
                hv_YGridStart.Dispose();
                hv_YCoord.Dispose();
                hv_Ascent.Dispose();
                hv_Descent.Dispose();
                hv_TextWidthXLabel.Dispose();
                hv_TextHeightXLabel.Dispose();
                hv_TextWidthYLabel.Dispose();
                hv_TextHeightYLabel.Dispose();
                hv_XTickStart.Dispose();
                hv_Indices.Dispose();
                hv_TypeTicks.Dispose();
                hv_IndexTicks.Dispose();
                hv_Ascent1.Dispose();
                hv_Descent1.Dispose();
                hv_TextWidthXTicks.Dispose();
                hv_TextHeightXTicks.Dispose();
                hv_YTickValues.Dispose();
                hv_YTickStart.Dispose();
                hv_TextWidthYTicks.Dispose();
                hv_TextHeightYTicks.Dispose();
                hv_Num.Dispose();
                hv_I.Dispose();
                hv_YSelected.Dispose();
                hv_Y1Selected.Dispose();
                hv_X1Selected.Dispose();
                hv_Index.Dispose();
                hv_Row1.Dispose();
                hv_Row2.Dispose();
                hv_Col1.Dispose();
                hv_Col2.Dispose();

                throw HDevExpDefaultException;
            }
        }
        #endregion
    }
}

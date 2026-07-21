using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HalconDotNet;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Threading;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using System.Collections;
using System.CodeDom;
using System.Reflection;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

namespace EWindowControl
{

    public partial class EWindowControl : UserControl
    {

        #region private members
        ///// <summary>
        ///// [] whether the current window has a displayed image
        ///// </summary>
        //private bool flag_viewImage = false;
        /// <summary>
        /// image window
        /// </summary>
        private HWindow hWindow;
        /// <summary>
        /// image source[Halcon]
        /// </summary>
        private HObject ho_Source = null;
        /// <summary>
        /// backup image [used by brushing]
        /// </summary>
        private HObject ho_Source_bak = null;
        /// <summary>
        /// [HImage]backup image
        /// </summary>
        private HImage ho_img_bak = null;
        /// <summary>
        /// image source[Bitmap]
        /// </summary>
        private Bitmap sourceBitmap = null;
        /// <summary>
        /// color/monochrome
        /// </summary>
        private bool _isColor = false;
        /// <summary>
        /// image width
        /// </summary>
        private HTuple image_W;
        /// <summary>
        /// image height
        /// </summary>
        private HTuple image_H;
        /// <summary>
        /// maximum Bitmap size
        /// </summary>
        double BmpSpaceLimit = 1024 * 1024 * 2000;
        /// <summary>
        /// color for drawing current ROI
        /// </summary>
        private int[] curRoiColor;
        /// <summary>
        /// default ROI color
        /// </summary>
        private static readonly int[] DefaultColor = { 255, 0, 0, 150 };
        /// <summary>
        /// ROI number background-frame color
        /// </summary>
        private int[] ROI_Text_BackColor = new int[4] { 0, 255, 119, 0 };
        /// <summary>
        /// default ROI border color
        /// </summary>
        private static readonly int[] DefaultBorderColor = { 0, 0, 127, 150 };
        /// <summary>
        /// magic-wand grayscale tolerance value
        /// </summary>
        private int tol_MagicWand = 50;
        /// <summary>
        /// scale ratio
        /// </summary>
        private int zoomRatio = 120;
        /// <summary>
        /// enable/disable zoom function 0: 1: 2:shrink
        /// </summary>
        private int enableZoomInOut = 0;
        /// <summary>
        /// enable/disable move function
        /// </summary>
        private bool flag_MoveImage = false;
        /// <summary>
        /// display-window operation function [0: pan off, zoom off; 1: pan on ; 2: zoom-in on; 3: zoom-out on]
        /// </summary>
        private int winOperate = 0;
        /// <summary>
        /// ROI type
        /// </summary>
        private RoiType drawingType = RoiType.None;
        /// <summary>
        /// for drawing ROI
        /// </summary>
        private HDrawingObject drawingObject;
        /// <summary>
        /// draw first point X
        /// </summary>
        private double DrawingROI_First_x1 = -1;
        /// <summary>
        /// draw first point Y
        /// </summary>
        private double DrawingROI_First_y1 = -1;
        /// <summary>
        /// manual draw-ROI first-point flag
        /// </summary>
        private bool flag_DrawingROI_FirstMouseDown = false;
        /// <summary>
        /// ROI second point has been clicked
        /// </summary>
        private bool flag_DrawingROI_NextPosIsClick = false;
        /// <summary>
        /// ROI list
        /// </summary>
        private ERoiList eRoiList;
        /// <summary>
        /// [for scaling multiple ROIs] ROI list()
        /// </summary>
        private ERoiList eRoiList_Clone;
        /// <summary>
        /// display ROI
        ///</summary>
        private bool visibleROI = true;
        /// <summary>
        /// display ROI number
        /// </summary>
        private bool visibleROIText = false;
        /// <summary>
        /// grayscale value/color value
        /// </summary>
        private HTuple grayValue = new HTuple();
        /// <summary>
        /// custom display information
        /// </summary>
        private bool enableInfoFromUser = false;
        /// <summary>
        /// enable/disable copy-ROI flag
        /// </summary>
        private bool flag_CopyROI = false;
        /// <summary>
        /// enable/disable draw-ROI flag
        /// </summary>
        private bool flag_DrawingROI = false;
        /// <summary>
        /// enable/disable select-ROI flag
        /// </summary>
        private bool flag_SelectedROI = false;
        /// <summary>
        /// selected ROI list
        /// </summary>
        private ERoiList SelectedROI;
        /// <summary>
        /// copied ROI parameters
        /// </summary>
        private ROIParm CopyROIParm;
        /// <summary>
        /// default ROI size
        /// </summary>
        private int defaultRoiSize = 128;
        /// <summary>
        /// start editing ROI
        /// </summary>
        private bool flag_EditROI = false;
        /// <summary>
        /// ROI to edit
        /// </summary>
        private ROIParm editROIParm;
        /// <summary>
        /// temporary ROI parameters
        /// </summary>
        private ROIParm TempROIParm;
        /// <summary>
        /// box color [not complete]
        /// </summary>
        HTuple DrawColor = new HTuple();
        /// <summary>
        /// ROI ID to edit
        /// </summary>
        private int EditeRoi_ID = -1;
        /// <summary>
        /// for mouse events
        /// </summary>
        private EMouseEventArgs eMouseEventArgs;
        /// <summary>
        /// for Info window scaling
        /// </summary>
        private float InfoHeight = 0;
        /// <summary>
        /// ROI size increment/decrement value
        /// </summary>
        private int roiStepSize = 1;
        /// <summary>
        /// [enable/disable] magnifier
        /// </summary>
        private bool flag_Magnifier = false;
        /// <summary>
        /// [enable/disable] eraser
        /// </summary>
        private bool flag_Erase = false;
        /// <summary>
        /// eraser mode
        /// </summary>
        private bool flag_Erase_MouseIsDown = false;
        /// <summary>
        /// eraser size
        /// </summary>
        private int eraseSize = 20;
        /// <summary>
        /// eraser type
        /// </summary>
        private BrushEraseType eraseType;
        /// <summary>
        /// eraser restore
        /// </summary>
        private bool flag_Erase_Recovery = false;
        /// <summary>
        /// whether the hotkey is registered
        /// </summary>
        private bool HotKeyIsRegister = false;
        /// <summary>
        /// show/hide center line
        /// </summary>
        private bool enableCeterLine = false;
        /// <summary>
        /// paste
        /// </summary>
        private bool flag_PasteROI=false;
        /// <summary>
        /// HotKey table
        /// </summary>
        private Dictionary<int, Tuple<uint, Keys>> HotKeyTable;
        /// <summary>
        /// custom cursor
        /// </summary>
        private Bitmap manualCursor = null;
        /// <summary>
        /// [enable/disable] magic wand
        /// </summary>
        private bool flag_MagicWand=false;
        /// <summary>
        /// magic-wand style
        /// </summary>
        private RoiType magicWandType=RoiType.None;
        /// <summary>
        /// Roi TEMP
        /// </summary>
        private ROIParm TempROI = null;
        /// <summary>
        /// free-shape coordinates
        /// </summary>
        private List< double> Polygon_x;
        /// <summary>
        /// free-shape coordinates
        /// </summary>
        private List<double> Polygon_y;
        ///// <summary>
        ///// brush region
        ///// </summary>
        //private HObject BrushRegion = null;
        ///// <summary>
        ///// eraser region
        ///// </summary>
        //private HObject EraseRegion = null;
        /// <summary>
        /// region to display
        /// </summary>
        private HObject _showRegion = null;
        /// <summary>
        /// brush + eraser result region
        /// </summary>
        private HObject BrushEraseRegion = null;
        /// <summary>
        /// whether any region is displayed
        /// </summary>
        private bool flag_showRegion = false;
        /// <summary>
        /// whether baseImage is displayed
        /// </summary>
        private bool flag_showBaseImage = false;
        /// <summary>
        /// region parameters in ROI list before editing
        /// </summary>
        private ROIParm Temp_EdieRoiRegione;
        /// <summary>
        /// draw center crosshair
        /// </summary>
        private HDrawingObject drawingCenterLine;
        /// <summary>
        /// [only in edit mode]record the last drawn region
        /// </summary>
        private HObject LastEditeDrawingRebion;
        /// <summary>
        /// measurement mode
        /// </summary>
        private bool flag_measure = false;
        /// <summary>
        /// previous measurement value
        /// </summary>
        private double LastDistance = -1;
        /// <summary>
        /// whether mouse-wheel zoom is enabled
        /// </summary>
        private bool flag_MouseWheelZoom = false;
        /// <summary>
        /// enable/disable auto draw default ROI flag
        /// </summary>
        private bool flag_AutoDrawingDefaultROI = false;
        /// <summary>
        /// limit ROI ratio
        /// </summary>
        private bool lockRoiScale=false;
        /// <summary>
        /// for resize
        /// </summary>
        private double Fix_RoiW = 0;
        /// <summary>
        /// for resize
        /// </summary>
        private double Fix_RoiH = 0;
        /// <summary>
        /// ROI region
        /// </summary>
        private HObject tempRegion=new HObject();
        /// <summary>
        /// ROI region after translation
        /// </summary>
        private HObject TranslateTempRegion = new HObject();
        /// <summary>
        /// default ROI
        /// </summary>
        private ROIParm defaultROIParm;
        /// <summary>
        /// center of the final region
        /// </summary>
        private Point LastRegionCenter;
        /// <summary>
        /// initial ROI bounding box value
        /// </summary>
        private HTuple LastParmValue = new HTuple();
        /// <summary>
        /// enable/disable multiple-ROI size scaling
        /// </summary>
        private bool flag_ZoomRoi_Batch = false;
        /// <summary>
        /// enable/disable ROI size scaling
        /// </summary>
        private bool flag_ZoomRoi = false;
        #endregion

        /// <summary>
        /// 
        /// </summary>
        public EWindowControl()
        {
            InitializeComponent();
            //hSmartWindowControl1.HalconWindow.SetWindowParam("00", 1);

            hWindow = hSmartWindowControl1.HalconWindow;
    

            hSmartWindowControl1.MouseWheel += HMouseWheel;
             eRoiList =new ERoiList();

            eMouseEventArgs = new EMouseEventArgs();
            //HOperatorSet.SetSystem("clip_region", "false");
            CreateHotKeyTable();
            Polygon_x = new List<double>();
            Polygon_y = new List<double>();
            //HOperatorSet.SetSystem("clip_region", "false"); // ,regions beyond the image range will be clipped
        }

        private void HMouseWheel(object sender, MouseEventArgs e)
        {
            if (!flag_MouseWheelZoom)
                return;
            System.Drawing.Point pt = this.Location;
            int leftBorder = hSmartWindowControl1.Location.X;
            int rightBorder = hSmartWindowControl1.Location.X + hSmartWindowControl1.Size.Width;
            int topBorder = hSmartWindowControl1.Location.Y;
            int bottomBorder = hSmartWindowControl1.Location.Y + hSmartWindowControl1.Size.Height;
            if (e.X > leftBorder && e.X < rightBorder && e.Y > topBorder && e.Y < bottomBorder)
            {
                MouseEventArgs newe = new MouseEventArgs(e.Button, e.Clicks, e.X - pt.X, e.Y - pt.Y, e.Delta);

                hSmartWindowControl1.HSmartWindowControl_MouseWheel(sender, newe);
            }
            if (EWinldowShowChanged != null)
                EWinldowShowChanged();
        }

        #region public events
        /// <summary>
        /// delegate for ROI completion
        /// </summary>
        /// <param name="roi">ROI parameters</param>
        public delegate void ROI_FinishEventHandler(ROIParm roi);
        /// <summary>
        /// ROI setup completed callback event
        /// </summary>
        public event ROI_FinishEventHandler EROI_Finish;
        /// <summary>
        /// delegate from first click to edit ROI completion
        /// </summary>
        /// <param name="roi">ROI parameters</param>
        public delegate void FirstClickEditeROI_EventHandler(ROIParm roi);
        /// <summary>
        /// callback event from first click to edit ROI completion
        /// </summary>
        public event FirstClickEditeROI_EventHandler EFirstClickEditeROI;
        /// <summary>
        /// delegate for ROI region edit
        /// </summary>
        /// <param name="roi">ROI parameters</param>
        public delegate void Region_EditeEventHandler(ROIParm roi);
        /// <summary>
        /// ROI region edit
        /// </summary>
        public event Region_EditeEventHandler ERegion_Edite;
        /// <summary>
        /// delegate for mouse-move event
        /// </summary>
        /// <param name="MousePoint"></param>
        /// <param name="UserInfo">custom display string</param>
        public delegate void MouseMoveInfoArgumenys(EMouseEventArgs MousePoint,ref string UserInfo);
        /// <summary>
        /// mouse-move event
        /// </summary>
        public event MouseMoveInfoArgumenys EMouseMoveInfo;
        /// <summary>
        /// delegate for mouse-down event
        /// </summary>
        /// <param name="MousePoint"></param>
        public delegate void MouseDownInfoArgumenys(EMouseEventArgs MousePoint);
        /// <summary>
        /// mouse-down event
        /// </summary>
        public event MouseMoveInfoArgumenys EMouseDownInfo;
        /// <summary>
        /// delegate for allowing ROI editing
        /// </summary>
        /// <param name="parm"> roiparameters</param>
        public delegate void AllowEditROIArgumenys(ROIParm parm);
        /// <summary>
        /// allow ROI editing event
        /// </summary>
        public event AllowEditROIArgumenys EAllowEditROI;
        /// <summary>
        /// drag image movement completed
        /// </summary>
        public delegate void DragMoveImageFinishHandler();
        /// <summary>
        /// [probably unused,replaced by WinldowShowChanged] drag image movement completed event
        /// </summary>
        public event DragMoveImageFinishHandler EDragMoveImageFinish;
        /// <summary>
        /// delegate for ROI deletion
        /// </summary>
        /// <param name="roi">ROI parameters</param>
        public delegate void ROI_DeleteEventHandler(ROIParm roi);
        /// <summary>
        /// ROI deletion callback event
        /// </summary>
        public event ROI_DeleteEventHandler EROI_Delete;
        /// <summary>
        /// delegate for ROI deletion
        /// </summary>
        /// <param name="roiList">ROI list parameters</param>
        public delegate void MultiROI_DeleteEventHandler(ERoiList roiList);
        /// <summary>
        /// ROI deletion callback event
        /// </summary>
        public event MultiROI_DeleteEventHandler EMultiROI_Delete;
        /// <summary>
        /// delegate for hotkey
        /// </summary>
        /// <param name="Cmd"></param>
        public delegate void HotkeyEventHandler(CmdHotKey Cmd);
        /// <summary>
        /// hotkey callback event
        /// </summary>
        public event HotkeyEventHandler EHotkeyEvent;
        /// <summary>
        /// delegate for brush or eraser drawing-completed event
        /// </summary>
        /// <param name="region">valid region area</param>
        public delegate void BrushOrEraseDrawingEventHandler(HObject region);
        /// <summary>
        /// brush or eraser drawing-completed event
        /// </summary>
        public event BrushOrEraseDrawingEventHandler EBrushOrEraseDrawingDone;
        /// <summary>
        /// delegate for display-window image position or size changed event
        /// </summary>
        public delegate void ImageResizeInWinldowHandler();
        /// <summary>
        /// display-window image position or size changed event
        /// </summary>
        public event ImageResizeInWinldowHandler EWinldowShowChanged;
        /// <summary>
        /// delegate for measurement result callback event
        /// </summary>
        public delegate void MeasureResponseHandler(double distance);
        /// <summary>
        /// measurement result callback event
        /// </summary>
        public event MeasureResponseHandler EMeasureResponse;

        /// <summary>
        /// temporary edit ROI completed event
        /// </summary>
        public delegate void TempROIFinishHandler();
        /// <summary>
        /// temporary edit ROI completed event
        /// </summary>
        public event TempROIFinishHandler ETempROIFinish;

        /// <summary>
        /// delegate for fixed-ratio ROI event
        /// </summary>
        /// <param name="roi">ROI parameters</param>
        /// <param name="_type">ROI parameters</param>
        public delegate void FixScaleROI_EventHandler(HTuple roi,string _type);
        /// <summary>
        /// fixed-ratio ROI event
        /// </summary>
        public event FixScaleROI_EventHandler EFixScaleROI;


        /// <summary>
        /// delegate for selected ROI list event
        /// </summary>
        /// <param name="eRoiList">ROI list</param>
        public delegate void SelectedROIList_EventHandler(ERoiList eRoiList);
        /// <summary>
        /// selected ROI list event
        /// </summary>
        public event SelectedROIList_EventHandler ESelectedROIList;
        #endregion

        #region public members
        /// <summary>
        /// True when the control currently has an initialized source image.
        /// </summary>
        public bool HasSourceImage
        {
            get { return ho_Source != null && ho_Source.IsInitialized(); }
        }

        /// <summary>
        /// Current display zoom, calculated from the visible image width and control width.
        /// </summary>
        public double CurrentZoom
        {
            get
            {
                if (!HasSourceImage || hSmartWindowControl1.Width <= 0) return 1d;
                double y1, x1, y2, x2;
                GetWinShowSize(out y1, out x1, out y2, out x2);
                double visibleWidth = x2 - x1 + 1d;
                if (visibleWidth <= 0d) return 1d;
                return hSmartWindowControl1.Width / visibleWidth;
            }
        }

        /// <summary>
        /// Raised with the current image coordinates while the mouse moves over the image; null when no image point is available.
        /// </summary>
        public event EventHandler<PointF?> ImagePointMoved;

        /// <summary>
        /// input HObject-format image
        /// </summary>
        public HObject SourceHobject
        {
            get
            {
                return ho_Source;
            }
            set
            {
                // ---fix operation bug during continuous acquisition----
                InitialFlag();
                editROIParm = null;
                CopyROIParm = null;
                if (EAllowEditROI != null)
                    EAllowEditROI(null);
                // ---fix operation bug during continuous acquisition----

                if (value == null)
                {
                    NullSource();
                    //ShowDrawingROI(eRoiList);
                    if (eRoiList.Count > 0)
                        ShowDrawingROI(eRoiList);
                    else
                    {
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
                    }
                    return;
                }

                // release memory [cannot be removed; otherwise it accumulates]
                //if (ho_Source != null)
                //    ho_Source.Dispose();

                ho_Source = value;
                InitialImage();
                //ShowDrawingROI(eRoiList);

                if (eRoiList.Count > 0)
                    ShowDrawingROI(eRoiList);
                else
                {
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
                }
            }
        }

        /// <summary>
        /// input Bitmap-format image
        /// </summary>
        public Bitmap SourceBitmap
        {
            get
            {
                if (sourceBitmap == null && ho_Source != null)
                {
                    double ImageSpaceSize = _isColor ? image_W.D * image_H.D * 3 : image_W.D * image_H.D;
                    // exceeds BMP size limit
                    if (ImageSpaceSize > BmpSpaceLimit)
                        return null;
                    if (!_isColor)
                        HObject2Bitmap8(ho_Source, out sourceBitmap);
                    else
                        Hobject2Bitmap24(ho_Source, out sourceBitmap);
                }
                return sourceBitmap;
            }
            set
            {
                // CancelROI_Action();
                // ---fix operation bug during continuous acquisition----
                InitialFlag();
                editROIParm = null;
                CopyROIParm = null;
                if (EAllowEditROI != null)
                    EAllowEditROI(null);
                // ---fix operation bug during continuous acquisition----
                if (value == null)
                {
                    NullSource();
                    return;
                }
                sourceBitmap = value;
                ho_Source = ConverImage(sourceBitmap);
                InitialImage();
                
                if (eRoiList.Count > 0)
                    ShowDrawingROI(eRoiList);
            }
        }

        /// <summary>
        /// Clear the current source image and dispose cached bitmap resources owned by this control.
        /// </summary>
        public void ClearImage()
        {
            Bitmap oldBitmap = sourceBitmap;
            HObject oldSource = ho_Source;
            SourceBitmap = null;
            sourceBitmap = null;
            ho_Source = null;
            if (oldBitmap != null) oldBitmap.Dispose();
            if (oldSource != null && oldSource.IsInitialized()) oldSource.Dispose();
            if (ho_img_bak != null)
            {
                ho_img_bak.Dispose();
                ho_img_bak = null;
            }
            if (ImagePointMoved != null) ImagePointMoved(this, null);
        }

        /// <summary>
        /// Set the current source bitmap, dispose any previous bitmap cache, and optionally fit it to the view.
        /// </summary>
        public void SetSourceBitmap(Bitmap bitmap, bool fit)
        {
            if (bitmap == null)
            {
                ClearImage();
                return;
            }

            Bitmap oldBitmap = sourceBitmap;
            HObject oldSource = ho_Source;
            sourceBitmap = null;
            ho_Source = null;
            if (oldBitmap != null && !ReferenceEquals(oldBitmap, bitmap)) oldBitmap.Dispose();
            if (oldSource != null && oldSource.IsInitialized()) oldSource.Dispose();

            SourceBitmap = bitmap;
            if (fit) FitImage();
        }



        /// <summary>
        /// Set the current source HALCON image. The control owns an internal copy so callers may dispose their HObject after this call.
        /// </summary>
        public void SetSourceImage(HObject image, bool fit)
        {
            if (image == null || !image.IsInitialized())
            {
                ClearImage();
                return;
            }

            HObject copied = null;
            HOperatorSet.CopyImage(image, out copied);
            Bitmap oldBitmap = sourceBitmap;
            HObject oldSource = ho_Source;
            sourceBitmap = null;
            ho_Source = null;
            if (oldBitmap != null) oldBitmap.Dispose();
            if (oldSource != null && oldSource.IsInitialized()) oldSource.Dispose();

            SourceHobject = copied;
            SetShowImage(true);
            ShowSourceImageAndROI();
            if (fit) FitImage();
        }

        /// <summary>
        /// Render image-coordinate rectangle overlays and optional labels over the current source image.
        /// </summary>
        public void RenderImageOverlay(IEnumerable<Tuple<Rectangle, string, string>> overlays)
        {
            ShowSourceImageAndROI();
            if (overlays == null) return;
            foreach (var overlay in overlays)
            {
                if (overlay == null) continue;
                var rect = overlay.Item1;
                var color = string.IsNullOrWhiteSpace(overlay.Item2) ? "red" : overlay.Item2;
                hWindow.SetColor(color);
                hWindow.SetLineWidth(2);
                HObject region = null;
                try
                {
                    HOperatorSet.GenRectangle1(out region, rect.Top, rect.Left, rect.Bottom, rect.Right);
                    hWindow.DispObj(region);
                    if (!string.IsNullOrWhiteSpace(overlay.Item3))
                    {
                        HOperatorSet.DispText(hWindow, overlay.Item3, "image", rect.Top + 3, rect.Left + 3, color, "box", "false");
                    }
                }
                finally
                {
                    if (region != null && region.IsInitialized()) region.Dispose();
                }
            }
        }

        /// <summary>
        /// enable/disable double-click image auto-fit image function
        /// </summary>
        public bool EnableDoubleClickZoom
        {
            get
            {
                return hSmartWindowControl1.HDoubleClickToFitContent;
            }
            set
            {
                hSmartWindowControl1.HDoubleClickToFitContent = value;
            }
        }

        /// <summary>
        /// enable/disable
        /// </summary>
        //public bool EnableMoveImage
        //{
        //    get
        //    {
        //        return flag_MoveImage;
        //    }
        //    set
        //    {

        //    }
        //}

        /// <summary>
        /// image zoom size 0: 1: 2:shrink
        /// </summary>
        //public int EnableZoomInOut
        //{
        //    get
        //    {
        //        return enableZoomInOut;
        //    }           
        //}

        /// <summary>
        /// scale ratio
        /// </summary>
        public int ZoomRatio
        {
            get
            {
                return zoomRatio;
            }
            set
            {
                zoomRatio = value;
            }
        }
        /// <summary>
        /// magic-wand grayscale tolerance value
        /// 
        /// </summary>
        public int Tol_MagicWand
        {
            get
            {
                return tol_MagicWand;
            }
            set
            {
                tol_MagicWand = value;
            }
        }
        //public Bitmap ECursor
        //{
        //    get
        //    {
        //        return manualCursor;
        //    }
        //    set
        //    {
        //        manualCursor = value;
        //        if (value != null)
        //        {
        //            Bitmap myNewCursor = new Bitmap(manualCursor.Width * 2, manualCursor.Height * 2);
        //            Graphics graphics = Graphics.FromImage(myNewCursor);
        //            graphics.Clear(Color.FromArgb(0, 0, 0, 0));
        //            graphics.DrawImage(manualCursor, manualCursor.Width, manualCursor.Height, manualCursor.Width, manualCursor.Height);
        //            hSmartWindowControl1.Cursor = new Cursor(myNewCursor.GetHicon());
        //            graphics.Dispose();
        //            myNewCursor.Dispose();
        //        }
        //        else
        //        {
        //            hSmartWindowControl1.Cursor = Cursors.Default;
        //        }
        //    }
        //}
        /// <summary>
        /// window operation [0: pan off, zoom off
        /// ; 1: pan on
        /// ; 2: zoom-in on
        /// ; 3: zoom-out on
        /// ; 4: magnifier]
        /// </summary>
        public int WinOperate
        {
            get
            {
                return winOperate;
            }
            set
            {
                if (flag_Erase)
                    ShowSourceImageAndROI();
                InitialFlag();
                
                winOperate = value;
                switch (winOperate)
                {
                    default:                        
                        // pan off, zoom off

                        hSmartWindowControl1.Cursor = Cursors.Default;

                        break;
                    case 0:
                        // pan off, zoom off
                        hSmartWindowControl1.Cursor = Cursors.Default;
                        break;
                    case 1:
                        // pan on
                        flag_MoveImage = true;
                        SetCursor(Properties.Resources.drag);
                        break;
                    case 2:
                        // zoom-in on
                        enableZoomInOut = 1;
                        SetCursor(Properties.Resources.zoomIn);
                        break;
                    case 3:
                        // zoom-out on
                        enableZoomInOut = 2;
                        SetCursor(Properties.Resources.zoomOut);
                        break;
                    case 4:
                        // magnifier
                        flag_Magnifier = true;                       
                        SetCursor(Properties.Resources.zoomIn);

                        // force setting red frame
                        curRoiColor = new int[4];
                        curRoiColor[0] = DefaultColor[0];
                        curRoiColor[1] = DefaultColor[1];
                        curRoiColor[2] = DefaultColor[2];
                        curRoiColor[3] = DefaultColor[3];
                        flag_DrawingROI_NextPosIsClick = false;
                        flag_DrawingROI_FirstMouseDown = false;
                        drawingType = RoiType.Rectangle;
                        break;
                }
                hSmartWindowControl1.HMoveContent = flag_MoveImage;
            }
        }

        /// <summary>
        /// display ROI
        /// </summary>
        public bool VisibleROI
        {
            get
            {
                return visibleROI;
            }
            set
            {
                visibleROI = value;
                if (visibleROI)
                {
                    if (eRoiList.Count > 0)
                        ShowDrawingROI(eRoiList);
                }
                else
                {
                    if (ho_Source != null)
                    {
                        //hWindow.ClearWindow();
                        //hWindow.DispObj(ho_Source);
                        //DispSoureImage();
                    }
                }
            }
        }

        /// <summary>
        /// display ROI number
        /// </summary>
        public bool VisibleROIText
        {
            get { return visibleROIText; }
            set { visibleROIText = value; }
        }

        /// <summary>
        /// show/hide Info window
        /// </summary>
        public bool EnableInfo
        {
            get
            {
                if (tableLayoutPanel1.RowStyles[0].Height > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                if (value)
                {
                    if (InfoHeight == 0)
                        InfoHeight = 5;
                    tableLayoutPanel1.RowStyles[0].SizeType = SizeType.Percent;
                    tableLayoutPanel1.RowStyles[0].Height = InfoHeight;
                    // relayout TableLayoutPanel so the change takes effect
                    tableLayoutPanel1.PerformLayout();
                }
                else
                {
                    tableLayoutPanel1.RowStyles[0].SizeType = SizeType.Percent;
                    InfoHeight = tableLayoutPanel1.RowStyles[0].Height;
                    // set the Visible property of the second row to false
                    tableLayoutPanel1.RowStyles[0].Height = 0;
                    // relayout TableLayoutPanel so the change takes effect
                    tableLayoutPanel1.PerformLayout();
                }
            }
        }

        /// <summary>
        /// enable/disable custom Info window content
        /// </summary>
        public bool EnableInfoFromUser
        {
            get { return enableInfoFromUser; }
            set { enableInfoFromUser = value; }
        }
        /// <summary>
        /// mouse-wheel zoom
        /// </summary>
        public bool EnableMouseWheelZoom { get => flag_MouseWheelZoom; set => flag_MouseWheelZoom = value; }
        /// <summary>
        /// default ROI size [default128]
        /// </summary>
        public int DefaultRoiSize { get => defaultRoiSize; set => defaultRoiSize = value; }
        /// <summary>
        /// limit ROI ratio
        /// </summary>
        public bool LockRoiScale { get => lockRoiScale; set => lockRoiScale = value; }
        #endregion


        #region public functions
        /// <summary>
        /// show or hide base image
        /// </summary>
        /// <param name="visable"></param>
        public void SetShowImage(bool visable)
        {
            if (ho_img_bak == null)
                return;
            if (!ho_img_bak.IsInitialized())
                return;

            flag_showBaseImage = visable;
            if (visable)
                hWindow.AttachBackgroundToWindow(ho_img_bak);
            else
                hWindow.DetachBackgroundFromWindow();
        }
        /// <summary>
        /// [not complete]profile
        /// </summary>
        public void ShowProfile()
        {
            return;
            double[] x=new double[4];
            double[] y=new double[4];
            x[0] = 1;
            x[1] = 2;
            x[2] = 3;
            x[3] = 4;

            y[0] = -5;
            y[1] = 2;
            y[2] = 3;
            y[3] = -5;
            HTuple hv_GenParamValue = new HTuple();
            HTuple hv_GenParamName = new HTuple();
            HTuple hv_Color = new HTuple();
            hv_Color[0] = "medium slate blue";
            hv_Color[1] = "yellow";

            hv_GenParamName[0] = "margin_left";
            hv_GenParamName[1] = "margin_top";
            hv_GenParamName[2] = "margin_right";
            hv_GenParamName[3] = "margin_bottom";
            hv_GenParamValue[0] = 0;
            hv_GenParamValue[1] = 0;
            hv_GenParamValue[2] = 100;
            hv_GenParamValue[3] = 100;

            HDevWindowStack.Push(hWindow);
            plot_tuple(hWindow, x, y, "X", "Z", hv_Color, hv_GenParamName, hv_GenParamValue);

            hv_GenParamName[0] = "margin_left";
            hv_GenParamName[1] = "margin_top";
            hv_GenParamName[2] = "margin_right";
            hv_GenParamName[3] = "margin_bottom";
            hv_GenParamValue[0] = 500;
            hv_GenParamValue[1] = 500;
            hv_GenParamValue[2] = 200;
            hv_GenParamValue[3] = 200;

            plot_tuple(hWindow, x, y, "Z", "X", hv_Color, hv_GenParamName, hv_GenParamValue);
            //hWindow.ClearWindow();
        }

        /// <summary>
        /// display frame center line
        /// </summary>
        /// <param name="bstate"></param>
        public void SetShowCenterLine(bool bstate)
        {
            enableCeterLine = bstate;
            CrossLine();
        }
        /// <summary>
        /// set brush/eraser size
        /// </summary>
        /// <param name="_size">size</param>
        public void SetBrushOrEraseSize(int _size)
        {
            eraseSize=_size;
            //if (eraseType != BrushEraseType.None)
            //    SetBrushOrErase(eraseType);
        }
        /// <summary>
        /// read brush/eraser size
        /// </summary>
        /// <returns>size</returns>
        public int GetBrushOrEraseSize()
        {
            return eraseSize;
        }

        /// <summary>
        /// set ROI step value
        /// </summary>
        /// <param name="_size">step value</param>
        public void SetRoiStepSize(int _size)
        {
            roiStepSize=_size;
        }
        /// <summary>
        /// get ROI step value
        /// </summary>
        /// <returns>step value</returns>
        public int GetRoiStepSize()
        {
            return roiStepSize;
        }

        /// <summary>
        /// width
        /// </summary>
        /// <returns></returns>
        public int GetImageWidth()
        {
            return image_W.I;
        }
        /// <summary>
        /// height
        /// </summary>
        /// <returns></returns>
        public int GetImageHeight()
        {
            return image_H.I;
        }
        /// <summary>
        /// read image size within current display range
        /// </summary>
        public void GetWinShowSize(out double y1, out double x1, out double y2, out double x2)
        {
            hWindow.GetPart(out HTuple row, out HTuple column, out HTuple row2, out HTuple column2);
            y1 = row;
            x1 = column;
            y2 = row2;
            x2 = column2;
        }

        /// <summary>
        /// custom mouse-move display information
        /// </summary>
        /// <param name="Info">information</param>
        public void UpdateInfo(string Info)
        {
            if (EnableInfoFromUser)
                lb_Info.Text = Info;
        }

        /// <summary>
        /// Fit image
        /// </summary>
        public void FitImage()
        {
            StopWinZoomInOutAndMove();
            hWindow.SetPart(0, 0, -2, -2);
            //hSmartWindowControl1.SetFullImagePart();
            ShowSourceImageAndROI();

            //CrossLine();
            if (EWinldowShowChanged != null)
                EWinldowShowChanged();
        }



        /// <summary>
        /// configured range for window display
        /// </summary>
        /// <param name="x1">image upper-left point</param>
        /// <param name="y1">image upper-left point</param>
        /// <param name="x2">image lower-right point</param>
        /// <param name="y2">image lower-right point</param>
        ///  <param name="UseLastScale">reuse the previous tScale</param>
        /// <returns></returns>
        public void ShowSize(int x1, int y1, int x2, int y2,bool UseLastScale = true)
        {
            if (UseLastScale)
            {
                
                hWindow.GetPart(out HTuple row, out HTuple column, out HTuple row2, out HTuple column2);
                double Width = column2.D - column.D;
                double Height = row2.D - row.D;
                hWindow.SetPart(y1, x1, y1 + Height, x1 + Width);
            }
            else
            {
                hWindow.SetPart(y1, x1, y2, x2);
            }
            //int Win_w = hSmartWindowControl1.Width/2;
            //int Win_h = hSmartWindowControl1.Height/2;

            //int dispW = 0;
            //int dispH = 0;


            //int CurW = (x2 - x1) / 2;
            //int CurH = (y2 - y1) / 2;

            //// component aspect ratio
            //double WinRatio = (double)hSmartWindowControl1.Width / (double)hSmartWindowControl1.Height;
            //// image aspect ratio
            //double ImgRatio = (double)CurW / (double)CurH;

            //if (image_W > hSmartWindowControl1.Width || image_H > hSmartWindowControl1.Height)
            //{
            //    if (ImgRatio>= WinRatio)
            //    {
            //        dispW = CurW;
            //        dispH = (int)((double)CurW/ (double)WinRatio);
            //    }

            //    if (ImgRatio < WinRatio)
            //    {
            //        dispW = (int)(CurH * WinRatio);
            //        dispH = CurH;
            //    }
            //}
            //else
            //{
            //    return;
            //}


            //dispW = dispW / 2;
            //dispH = dispH / 2;
            //// center
            //int cx = (x2 + x1) / 2;
            //int cy = (y2 + y1) / 2;
            //hWindow.SetPart(cy - dispH, cx- dispW, cy + dispH, cx + dispW);
            //CrossLine();
            if (EWinldowShowChanged != null)
            {
                EWinldowShowChanged();
            }
        }

        /// <summary>
        /// configured range for window display
        /// </summary>
        /// <param name="pt1">image upper-left point</param>
        /// <param name="pt2">image lower-right point</param>
        /// <returns></returns>
        public void ShowSize(Point pt1, Point pt2)
        {
            //CrossLine();
            hWindow.SetPart(pt1.Y, pt1.X, pt2.Y, pt2.X);
            if (EWinldowShowChanged != null)
            {
                EWinldowShowChanged();
            }
        }
        /// <summary>
        /// select ROI
        /// </summary>
        public void SelectRoi()
        {
            int[] _color = new int[4];

            _color[0] = 0;
            _color[1] = 0;
            _color[2] = 255;
            _color[3] = 150;

            if (drawingObject != null)
                drawingObject.ClearDrawingObject();

            if (flag_EditROI)
                CancelROI_Action();

            WinOperate = 0;
            flag_DrawingROI = true;
            flag_SelectedROI= true;

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

            drawingType = RoiType.Rectangle;
        }

        //[Obsolete("useSetDrawingROI_Auto", true)]
        /// <summary>
        /// manually draw ROI
        /// </summary>
        /// <param name="type">ROI type</param>
        /// <param name="_color">ROI color</param>
        /// <returns></returns>
        public void SetDrawingROI(RoiType type, int[] _color = null)
        {
            if (drawingObject != null)
                drawingObject.ClearDrawingObject();

            if (flag_EditROI)
                CancelROI_Action();

            WinOperate = 0;
            flag_DrawingROI = true;

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

            drawingType = type;
            if (type == RoiType.Polygon)
            {
                Polygon_x.Clear();
                Polygon_y.Clear();
            }

        }

        /// <summary>
        /// automatically generate default ROI
        /// </summary>
        /// <param name="type">ROI type</param>
        /// <param name="_color">ROI color</param>
        /// <returns></returns>
        public void SetDrawingROI_Auto(RoiType type, int[] _color = null)
        {
            // [2025/02/05]
            if (drawingObject != null)
                drawingObject.ClearDrawingObject();

            if (flag_EditROI)
                CancelROI_Action();

            if(SelectedROI!=null)
                SelectedROI=null;

            WinOperate = 0;
            flag_AutoDrawingDefaultROI = true;

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

            if (type == RoiType.Polygon)
            {
                Polygon_x.Clear();
                Polygon_y.Clear();
            }

            defaultROIParm = CreateDefaultROI(curRoiColor, type);

            HTuple cx = new HTuple();
            HTuple cy = new HTuple();
            HTuple area=new HTuple();

            HOperatorSet.AreaCenter(defaultROIParm.Region, out area, out cy, out cx);
            LastRegionCenter = new Point((int)cx.D,(int)cy.D);
            //Console.WriteLine($"------------------------------------------------------");
            //flag_DrawingROI_FirstMouseDown = false;
            //flag_DrawingROI_NextPosIsClick = false;

            //flag_PasteROI = true;
            //drawingObject = new HDrawingObject();
            //drawingType = type;
        }


        /// <summary>
        /// create default ROI
        /// </summary>
        /// <param name="_color">color</param>
        /// <param name="roiType">type</param>
        /// <returns></returns>
        private ROIParm CreateDefaultROI(int[] _color, RoiType roiType)
        {

            ROIParm DefaultROIParm = new ROIParm();
            DefaultROIParm.VisibleROIText = false;
            DefaultROIParm.VisableROI = true;
            DefaultROIParm._color = _color;
            DefaultROIParm._type = RoiType.Region;
            HObject roiRegion = new HObject();

            switch (roiType)
            {
                case RoiType.Rectangle:
                    HOperatorSet.GenRectangle1(out roiRegion, 0, 0, defaultRoiSize, defaultRoiSize);
                    DefaultROIParm.Region = roiRegion;
                    break;
                case RoiType.Circle:
                    HOperatorSet.GenCircle(out roiRegion, 0, 0, defaultRoiSize / 2);
                    DefaultROIParm.Region = roiRegion;
                    break;
            }
            DefaultROIParm.RegionBorder= GenerateRegionBorder(DefaultROIParm.Region);
            return DefaultROIParm;
        }
        /// <summary>
        /// create default ROI
        /// </summary>
        /// <param name="_color">color</param>
        /// <param name="roiType">type</param>
        /// <returns></returns>
        private ROIParm CreateDefaultROI(int[] _color, RoiType roiType,HObject region)
        {

            ROIParm DefaultROIParm = new ROIParm();
            DefaultROIParm.VisibleROIText = false;
            DefaultROIParm.VisableROI = true;
            DefaultROIParm._color = _color;
            DefaultROIParm._type = RoiType.Region;
            HObject roiRegion = new HObject();
            DefaultROIParm.Region = region;
            //switch (roiType)
            //{
            //    case RoiType.Rectangle:
            //        HOperatorSet.GenRectangle1(out roiRegion, 0, 0, defaultRoiSize, defaultRoiSize);
            //        DefaultROIParm.Region = region;
            //        break;
            //    case RoiType.Circle:
            //        HOperatorSet.GenCircle(out roiRegion, 0, 0, defaultRoiSize / 2);
            //        DefaultROIParm.Region = region;
            //        break;
            //}
            DefaultROIParm.RegionBorder = GenerateRegionBorder(DefaultROIParm.Region);
            return DefaultROIParm;
        }
        /// <summary>
        /// [Region]generate maximum outer bounding rectangle
        /// </summary>
        /// <param name="region"></param>
        private HObject GenerateRegionBorder(HObject region)
        {

            HObject RegionBorder = new HObject();
            HOperatorSet.ShapeTrans(region, out RegionBorder,"rectangle1");
            HOperatorSet.Boundary(RegionBorder, out RegionBorder, "outer");

            return RegionBorder;
        }
        /// <summary>
        /// measurement mode
        /// </summary>
        public void SetMeasureMode()
        {
            LastDistance = -1;
            SetDrawingROI(RoiType.Line, new int[] { 255, 0, 0, 125 });
            flag_measure = true;
        }

        /// <summary>
        /// set the ROI to edit
        /// </summary>
        /// <param name="RoiID">ROI id</param>
        /// <returns>return the ROI parameters to edit</returns>
        public ROIParm SetEditeRoi(int RoiID)
        {
            //// first confirm whether this ID exists in the list
            //editROIParm = eRoiList.Find_FirstOrDefaultById(RoiID);
            //WinOperate = 0;
            //if (editROIParm == null)
            //{
            //    CancelROI_Action();
            //    return null;
            //}


            //EditeRoi_ID = RoiID;
            //flag_EditROI = true;
            //HTuple parmName, parmValue;
            //if (EditeRoi_ID == -1)
            //{
            //    CancelROI_Action();
            //    return null;
            //}


            ////EditROIParm._color = curRoiColor;
            //// then update the screen again
            //ShowDrawingROI(eRoiList);

            //// edit ROI
            //parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
            //HOperatorSet.RegionFeatures(editROIParm.Region, parmName, out parmValue);
            //InitialDrawing(editROIParm._type, editROIParm._color);
            //DrawingROI_First_x1 = parmValue[1];
            //DrawingROI_First_y1 = parmValue[0];
            //Point NextPos = new Point((int)parmValue[3].D, (int)parmValue[2].D);

            //if (editROIParm._type == RoiType.Circle)
            //{
            //    parmName = new HTuple(new string[] { "row", "column", "radius" });
            //    HOperatorSet.RegionFeatures(editROIParm.Region, parmName, out parmValue);
            //    DrawingROI_First_x1 = parmValue[1].D;
            //    DrawingROI_First_y1 = parmValue[0].D;
            //    NextPos = new Point((int)DrawingROI_First_x1 + (int)parmValue[2].D, (int)DrawingROI_First_y1 + (int)parmValue[2].D);
            //}

            //if (editROIParm._type == RoiType.Polygon)
            //{
            //    HTuple parmValue2;
            //    HOperatorSet.GetRegionPolygon(editROIParm.Region, 1, out parmValue, out parmValue2);
            //    drawingObject.CreateDrawingObjectXld(parmValue, parmValue2);

            //    if (drawingObject.ID != 0)
            //    {
            //        //associate the drawing object with the Halcon window
            //        hWindow.AttachDrawingObjectToWindow(drawingObject);
            //    }
            //}
            //else
            //{
            //    DrawingROIRegion(NextPos);
            //}
            //return editROIParm;




            // first confirm whether this ID exists in the list
            editROIParm = eRoiList.Find_FirstOrDefaultById(RoiID);
            WinOperate = 0;
            if (editROIParm == null)
            {
                CancelROI_Action();
                return null;
            }


            EditeRoi_ID = RoiID;
            flag_EditROI = true;
            HTuple parmName, parmValue;
            if (EditeRoi_ID == -1)
            {
                CancelROI_Action();
                return null;
            }


            //EditROIParm._color = curRoiColor;
            // then update the screen again
            ShowDrawingROI(eRoiList);

            // edit ROI
            parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
            HOperatorSet.RegionFeatures(editROIParm.Region, parmName, out parmValue);
            Point point_LT = new Point((int)parmValue.DArr[1], (int)parmValue.DArr[0]);
            Point point_RB = new Point((int)parmValue.DArr[3], (int)parmValue.DArr[2]);
            tempRegion = editROIParm.Region;
            DrawingROIRegion2(point_LT, point_RB);

            //InitialDrawing(editROIParm._type, editROIParm._color);
            //DrawingROI_First_x1 = parmValue[1];
            //DrawingROI_First_y1 = parmValue[0];
            //Point NextPos = new Point((int)parmValue[3].D, (int)parmValue[2].D);

            //if (editROIParm._type == RoiType.Circle)
            //{
            //    parmName = new HTuple(new string[] { "row", "column", "radius" });
            //    HOperatorSet.RegionFeatures(editROIParm.Region, parmName, out parmValue);
            //    DrawingROI_First_x1 = parmValue[1].D;
            //    DrawingROI_First_y1 = parmValue[0].D;
            //    NextPos = new Point((int)DrawingROI_First_x1 + (int)parmValue[2].D, (int)DrawingROI_First_y1 + (int)parmValue[2].D);
            //}

            //if (editROIParm._type == RoiType.Polygon)
            //{
            //    HTuple parmValue2;
            //    HOperatorSet.GetRegionPolygon(editROIParm.Region, 1, out parmValue, out parmValue2);
            //    drawingObject.CreateDrawingObjectXld(parmValue, parmValue2);

            //    if (drawingObject.ID != 0)
            //    {
            //        //associate the drawing object with the Halcon window
            //        hWindow.AttachDrawingObjectToWindow(drawingObject);
            //    }
            //}
            //else
            //{
            //    DrawingROIRegion(NextPos);
            //}
            return editROIParm;
        }


        /// <summary>
        /// copy ROI
        /// </summary>
        private void CopyRoI()
        {
            if (flag_EditROI)
            {
                flag_CopyROI = true;
                CopyROIParm = editROIParm;
                editROIParm = null;
                flag_EditROI = false;
                ShowDrawingROI(eRoiList);
                drawingObject = new HDrawingObject();
            }
        }
        /// <summary>
        /// paste ROI
        /// </summary>
        private void PasteRoI()
        {
            if (flag_CopyROI)
            {
                if (CopyROIParm == null)
                {
                    flag_CopyROI = false;
                    return;
                }                 
                drawingType = CopyROIParm._type;
                tempRegion = CopyROIParm.Region;
                winOperate = 0;
                hSmartWindowControl1.Cursor = Cursors.Default;

                LastRegionCenter = new Point(-1,-1);
                flag_PasteROI = true;
            }
       
        }
        /// <summary>
        /// cancel ROI operation
        /// </summary>
        public void CancelROI_Action()
        {
            LastEditeDrawingRebion =null;
            CopyROIParm = null;
            tempRegion=null;
            if (TempROIParm != null)
            {
                editROIParm.VisableROI = true;
                eRoiList.UpdateROI(editROIParm);
                eRoiList.RemoveList_ByID(TempROIParm.ID);
                TempROIParm = null;
            }
            editROIParm = null;
            //hWindow.ClearWindow();
            //if (EAllowEditROI != null)
            //    EAllowEditROI(null);
            ShowSourceImageAndROI();
            InitialFlag();
            if (drawingObject!=null)
                drawingObject.ClearDrawingObject();
            SelectedROI = null;
            WinOperate = 0;
        }

        /// <summary>
        /// delete ROI
        /// </summary>
        private void DeleteROI()
        {
            if(SelectedROI != null)
            {
                if (EMultiROI_Delete != null)
                    EMultiROI_Delete(SelectedROI);

                for (int i = 0; i < SelectedROI.Count; i++)
                    eRoiList.Remove_ByROI(SelectedROI[i]);

                InitialFlag();
                drawingObject.ClearDrawingObject();
                drawingType = RoiType.None;
                ShowSourceImageAndROI();
                SelectedROI=null;
            }
            else
            {
                if (flag_EditROI == false)
                    return;

                if (EROI_Delete != null && editROIParm != null)
                    EROI_Delete(editROIParm);

                eRoiList.Remove_ByROI(editROIParm);

                InitialFlag();
                drawingObject.ClearDrawingObject();
                drawingType = RoiType.None;

                if (editROIParm._type == RoiType.Region)
                {
                    ShowSourceImageAndROI();
                }
            }



        }

        ///// <summary>
        ///// get the current ROI list
        ///// </summary>
        ///// <returns></returns>
        //public List<ROIParm> GetROIParms()
        //{
        //    return RoiList;
        //}

        /// <summary>
        /// get the current ROI list
        /// </summary>
        /// <returns></returns>
        public ERoiList GetROIList()
        {
            return eRoiList.Clone();
        }
        /// <summary>
        /// get the current ROI to edit
        /// </summary>
        /// <returns></returns>
        public ROIParm GetCurEditROIParm()
        {
            return editROIParm;
        }
        /// <summary>
        /// get the current ROI to edit
        /// </summary>
        /// <returns></returns>
        public ROIParm GetCurTempROIParm()
        {
            return TempROIParm;
        }
        /// <summary>
        /// add ROI
        /// </summary>
        /// <param name="parm"> ROI parameters</param>
        public void AddRoi(ROIParm parm)
        {
            if (parm._color == null)
                parm._color = (int[])DefaultColor.Clone();
            eRoiList.AddROI(parm);
            if (eRoiList.Count > 0)
                ShowDrawingROI(eRoiList);
        }

        /// <summary>
        /// update ROI list
        /// </summary>
        public void UpdateROIList(ERoiList elist)
        {
            InitialFlag();
            eRoiList = elist;
            ShowDrawingROI(eRoiList);
            drawingObject = new HDrawingObject();
        }


        /// <summary>
        /// set ROI bounding-box size
        /// </summary>
        /// <param name="RoiList">eRoiList to edit</param>
        /// <param name="Width">size</param>
        /// <param name="Height">size</param>
        public void SetROISize(ERoiList RoiList, int Width, int Height)
        {
            HTuple parmName;
            HObject temp = new HObject();
            HObject ShapeTransRegion = new HObject();
            HTuple hv_value = new HTuple();
            HTuple hv_value1 = new HTuple();
            HTuple hv_value2 = new HTuple();
            int Half_H = Height / 2;
            int Half_W = Width / 2;
            HTuple RegionCenterValue = new HTuple();


            parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
            for (int k = 0; k < RoiList.Count; k++)
            {
                if (!RoiList[k].VisableROI)
                    continue;
                HOperatorSet.ShapeTrans(RoiList[k].Region, out ShapeTransRegion, "rectangle1");
                HOperatorSet.RegionFeatures(ShapeTransRegion, parmName, out hv_value1);


                hv_value2[0] = ((hv_value1[0] + hv_value1[2]) / 2) - Half_H;
                hv_value2[1] = ((hv_value1[1] + hv_value1[3]) / 2) - Half_W;
                hv_value2[2] = ((hv_value1[0] + hv_value1[2]) / 2) + Half_H;
                hv_value2[3] = ((hv_value1[1] + hv_value1[3]) / 2) + Half_W;
                temp = AffineTransRegion(RoiList[k].Region, hv_value1, hv_value2);
                RoiList[k].Region = temp;
            }
            eRoiList.UpdateROI(RoiList);
            ShowDrawingROI(eRoiList, false);






            //parmName = new HTuple(new string[] { "row", "column" });
            //// classify first for acceleration
            //for (int k = 0; k < RoiList.Count; k++)
            //{
            //    if (!RoiList[k].VisableROI)
            //        continue;
            //    parmName = new HTuple(new string[] { "circularity", "rectangularity" });
            //    HOperatorSet.RegionFeatures(RoiList[k].Region, parmName, out hv_value);
            //    if (hv_value.Length == 0)
            //        continue;

            //    parmName = new HTuple(new string[] { "row", "column" });
            //    HOperatorSet.ShapeTrans(RoiList[k].Region, out temp, "rectangle1");
            //    HOperatorSet.RegionFeatures(temp, parmName, out RegionCenterValue);

            //    HOperatorSet.GenRectangle1(out temp,
            //        RegionCenterValue[0] - Half_H, RegionCenterValue[1] - Half_W,
            //        RegionCenterValue[0] + Half_H, RegionCenterValue[1] + Half_W);

            //    // circularity>rectangularity && circularity>0.9
            //    if (hv_value[0] > hv_value[1] && hv_value[0] > 0.9)
            //    {
            //        // circle
            //        HOperatorSet.ShapeTrans(temp, out temp, "inner_circle");
            //    }
            //    RoiList[k].Region = temp;
            //}
            //eRoiList.UpdateROI(RoiList);
            //ShowDrawingROI(eRoiList, false);
        }

        /// <summary>
        /// increase ROI size
        /// </summary>
        /// <param name="direction">direction</param>
        public void IncreaseROI2(ROI_Direction direction)
        {
           
            HTuple parmName, parm;

            if (drawingObject == null || !drawingObject.IsInitialized())
                return;

            HObject drawingOutterRect = new HObject();
            HOperatorSet.GetDrawingObjectIconic(out drawingOutterRect, drawingObject);

            parmName = new HTuple(new string[] { "circularity", "rectangularity"});
            HTuple hv_value=new HTuple();
            HOperatorSet.RegionFeatures(tempRegion, parmName, out hv_value);


            switch (direction)
            {
                case ROI_Direction.Hor:
                    HOperatorSet.DilationRectangle1(drawingOutterRect, out drawingOutterRect, roiStepSize * 3, 1);
                    break;
                case ROI_Direction.Vert:
                    HOperatorSet.DilationRectangle1(drawingOutterRect, out drawingOutterRect, 1, roiStepSize * 3);
                    break;
                case ROI_Direction.Both:
                    HOperatorSet.DilationRectangle1(drawingOutterRect, out drawingOutterRect, roiStepSize * 3, roiStepSize * 3);
                    break;
            }
            if (hv_value[0] > hv_value[1] && hv_value[0] > 0.9)
            {
                HOperatorSet.ShapeTrans(drawingOutterRect, out drawingOutterRect, "inner_circle");
            }

            parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
            HOperatorSet.RegionFeatures(drawingOutterRect, parmName,out parm);

            tempRegion = drawingOutterRect;
            editROIParm.Region = drawingOutterRect;
            eRoiList.UpdateROI(editROIParm);
            ShowDrawingROI(eRoiList, false);
            drawingObject.SetDrawingObjectParams(parmName, parm);
            hWindow.DispObj(drawingOutterRect);
        }

        /// <summary>
        /// increase ROI size
        /// </summary>
        /// <param name="direction">direction</param>
        /// <param name="UpdateRegion">update original region</param>
        public void IncreaseROI(ROI_Direction direction,bool UpdateRegion = false)
        {
            HTuple parmName, parm;
            HTuple hv_value1 = new HTuple();
            HTuple hv_value2 = new HTuple();
            if (drawingObject == null || !drawingObject.IsInitialized())
                return;

            flag_ZoomRoi = true;
            if (UpdateRegion)
            {
                if (TranslateTempRegion!=null && TranslateTempRegion.IsInitialized())
                    tempRegion = TranslateTempRegion;
            }

            HObject drawingOutterRect = new HObject();
            HObject ShapeTransRegion=new HObject();
            //HOperatorSet.GetDrawingObjectIconic(out drawingOutterRect, drawingObject);

            parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
            HOperatorSet.ShapeTrans(tempRegion, out ShapeTransRegion, "rectangle1");
            HOperatorSet.RegionFeatures(ShapeTransRegion, parmName, out hv_value1);

            switch (direction)
            {
                case ROI_Direction.Hor:
                    HOperatorSet.DilationRectangle1(ShapeTransRegion, out drawingOutterRect, roiStepSize * 3, 1);
                    break;
                case ROI_Direction.Vert:
                    HOperatorSet.DilationRectangle1(ShapeTransRegion, out drawingOutterRect, 1, roiStepSize * 3);
                    break;
                case ROI_Direction.Both:
                    HOperatorSet.DilationRectangle1(ShapeTransRegion, out drawingOutterRect, roiStepSize * 3, roiStepSize * 3);
                    break;
            }

            HOperatorSet.RegionFeatures(drawingOutterRect, parmName, out hv_value2);
            //Console.WriteLine($"===========================================================================");
            //Console.WriteLine($"row1: {hv_value1[0].D},column1: {hv_value1[1].D},row2: {hv_value1[2].D},column2: {hv_value1[3].D}");
            //Console.WriteLine($"row1: {hv_value2[0].D},column1: {hv_value2[1].D},row2: {hv_value2[2].D},column2: {hv_value2[3].D}");

            TranslateTempRegion = AffineTransRegion(tempRegion, hv_value1, hv_value2);
            //tempRegion = TranslateTempRegion;

            ShowDrawingROI(eRoiList, false);
            drawingObject.SetDrawingObjectParams(parmName, hv_value2);
            hWindow.DispObj(TranslateTempRegion);
        }

        /// <summary>
        /// scale region
        /// </summary>
        /// <param name="region">region</param>
        /// <param name="hv_ParmValue1">initial parameters "row1", "column1", "row2", "column2"</param>
        /// <param name="hv_Parmvalue2">new parameters "row1", "column1", "row2", "column2"</param>
        private HObject AffineTransRegion(HObject region, HTuple hv_ParmValue1, HTuple hv_Parmvalue2)
        {
            HTuple initialX = new HTuple();
            HTuple initialY = new HTuple();
            //upper left
            initialX.Append(hv_ParmValue1[1]);  
            initialY.Append(hv_ParmValue1[0]);
            //lower left
            initialX.Append(hv_ParmValue1[1]);  
            initialY.Append(hv_ParmValue1[2]);
            //upper right
            initialX.Append(hv_ParmValue1[3]);  
            initialY.Append(hv_ParmValue1[0]);
            //lower right
            initialX.Append(hv_ParmValue1[3]);  
            initialY.Append(hv_ParmValue1[2]);  

            HTuple NewX = new HTuple();
            HTuple NewY = new HTuple();
            //upper left
            NewX.Append(hv_Parmvalue2[1]);
            NewY.Append(hv_Parmvalue2[0]);
            //lower left
            NewX.Append(hv_Parmvalue2[1]);
            NewY.Append(hv_Parmvalue2[2]);
            //upper right
            NewX.Append(hv_Parmvalue2[3]);
            NewY.Append(hv_Parmvalue2[0]);
            //lower right
            NewX.Append(hv_Parmvalue2[3]);
            NewY.Append(hv_Parmvalue2[2]);

            // VectorToHomMat2d requires at least 3 points, so use the four bounding-box points
            HTuple hv_HomMat2D=new HTuple();
            HObject NewRegion = new HObject();
            HOperatorSet.VectorToHomMat2d(initialY, initialX, NewY, NewX, out hv_HomMat2D);

            int zeroCount = 0;

            for (int i = 0; i < hv_HomMat2D.Length; i++)
            {
                if (hv_HomMat2D[i].D == 0)
                {
                    zeroCount++;
                }
            }
            if (zeroCount== hv_HomMat2D.Length)
            {
                return region;
            }

            HOperatorSet.AffineTransRegion(region, out NewRegion, hv_HomMat2D, "nearest_neighbor");
            return NewRegion;
        }


        /// <summary>
        /// increase ROI size
        /// </summary>
        /// <param name="direction">direction</param>
        /// <param name="RoiList">eRoiList to edit</param>
        public void IncreaseROI_Old(ROI_Direction direction,ERoiList RoiList)
        {      
            HTuple parmName, parm;
            Dictionary<string, HObject> table1 = new Dictionary<string, HObject>();
            HObject temp = new HObject();
            HObject concatObj = new HObject();
            HTuple hv_value = new HTuple();

            // classify first for acceleration
            for (int k = 0; k < RoiList.Count; k++)
            {
                parmName = new HTuple(new string[] { "circularity", "rectangularity" }); 
                HOperatorSet.RegionFeatures(RoiList[k].Region, parmName, out hv_value);
                if (hv_value.Length == 0)
                    continue;

                if (hv_value[0] > hv_value[1] && hv_value[0] > 0.9)
                {
                    switch (direction)
                    {
                        case ROI_Direction.Hor:
                            HOperatorSet.DilationRectangle1(RoiList[k].Region, out temp, roiStepSize * 3, 1);
                            break;
                        case ROI_Direction.Vert:
                            HOperatorSet.DilationRectangle1(RoiList[k].Region, out temp, 1, roiStepSize * 3);
                            break;
                        case ROI_Direction.Both:
                            HOperatorSet.DilationRectangle1(RoiList[k].Region, out temp, roiStepSize * 3, roiStepSize * 3);
                            HOperatorSet.ShapeTrans(temp, out temp, "inner_circle");
                            break;
                    }               
                }
                else
                {
                    switch (direction)
                    {
                        case ROI_Direction.Hor:
                            HOperatorSet.DilationRectangle1(RoiList[k].Region, out temp, roiStepSize * 3, 1);
                            break;
                        case ROI_Direction.Vert:
                            HOperatorSet.DilationRectangle1(RoiList[k].Region, out temp, 1, roiStepSize * 3);
                            break;
                        case ROI_Direction.Both:
                            HOperatorSet.DilationRectangle1(RoiList[k].Region, out temp, roiStepSize * 3, roiStepSize * 3);                          
                            break;
                    }
                }

                parmName = new HTuple(new string[] { "row", "column" });
                RoiList[k].Region = temp;

            }
            eRoiList.UpdateROI(RoiList);
            ShowDrawingROI(RoiList,false);
        }


        /// <summary>
        /// [for zoom in/out]get ROI list to edit
        /// </summary>
        public ERoiList GetEditeROIList()
        {
             return eRoiList_Clone ;
        }

        /// <summary>
        /// increase ROI list size
        /// </summary>
        /// <param name="direction">direction</param>
        /// <param name="RoiList">eRoiList to edit</param>
        public void IncreaseROI(ROI_Direction direction, ERoiList RoiList)
        {       
            eRoiList_Clone = RoiList.Clone();    // must clone

            HTuple parmName;
            HObject tempRegion = new HObject();
            HTuple hv_value1 = new HTuple();
            HTuple hv_value2 = new HTuple();
            parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
            HObject ShapeTransRegion = new HObject();

            for (int k = 0; k < RoiList.Count; k++)
            {
                parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
                HOperatorSet.ShapeTrans(RoiList[k].Region, out ShapeTransRegion, "rectangle1");
                HOperatorSet.RegionFeatures(ShapeTransRegion, parmName, out hv_value1);

                if (hv_value1.Length == 0)
                    continue;

                HTuple initialX = new HTuple();
                HTuple initialY = new HTuple();
                HTuple NewX = new HTuple();
                HTuple NewY = new HTuple();
                switch (direction)
                {
                    case ROI_Direction.Hor:
                        HOperatorSet.DilationRectangle1(ShapeTransRegion, out tempRegion, roiStepSize * 3, 1);
                        break;
                    case ROI_Direction.Vert:
                        HOperatorSet.DilationRectangle1(ShapeTransRegion, out tempRegion, 1, roiStepSize * 3);
                        break;
                    case ROI_Direction.Both:
                        HOperatorSet.DilationRectangle1(ShapeTransRegion, out tempRegion, roiStepSize * 3, roiStepSize * 3);
                        break;
                }
                HOperatorSet.RegionFeatures(tempRegion, parmName, out hv_value2);

                tempRegion = AffineTransRegion(RoiList[k].Region, hv_value1, hv_value2);
                //Console.WriteLine($"===========================================================================");
                //Console.WriteLine($"row1: {hv_value1[0].D},column1: {hv_value1[1].D},row2: {hv_value1[2].D},column2: {hv_value1[3].D}");
                //Console.WriteLine($"row1: {hv_value2[0].D},column1: {hv_value2[1].D},row2: {hv_value2[2].D},column2: {hv_value2[3].D}");
                eRoiList_Clone[k].Region = tempRegion;
            }
            ShowDrawingROI(eRoiList_Clone, false);


            #region draw border for better appearance
            int[] colors = new int[4] { 0, 0, 255, 128 };
            HObject rectRegion = new HObject();
            hWindow.SetRgba(colors[0], colors[1], colors[2], colors[3]);
            for (int i = 0; i < eRoiList_Clone.Count; i++)
            {
                HOperatorSet.ShapeTrans(eRoiList_Clone[i].Region, out rectRegion, "rectangle1");
                HOperatorSet.DilationRectangle1(rectRegion, out rectRegion, 5, 5);
                HOperatorSet.Boundary(rectRegion, out rectRegion, "outer");
                hWindow.DispObj(rectRegion);
            }
            #endregion
        }

        /// <summary>
        /// reduce ROI size
        /// </summary>
        /// <param name="direction">direction</param>
        public void ReduceROI_Old(ROI_Direction direction)
        {
            HTuple parmName, parm;

            if (drawingObject == null || !drawingObject.IsInitialized())
                return;

            HObject drawingOutterRect = new HObject();
            HOperatorSet.GetDrawingObjectIconic(out drawingOutterRect, drawingObject);

            parmName = new HTuple(new string[] { "circularity", "rectangularity" });
            HTuple hv_value = new HTuple();
            HOperatorSet.RegionFeatures(tempRegion, parmName, out hv_value);

            switch (direction)
            {
                case ROI_Direction.Hor:
                    HOperatorSet.ErosionRectangle1(drawingOutterRect, out drawingOutterRect, roiStepSize * 3, 1);
                    break;
                case ROI_Direction.Vert:
                    HOperatorSet.ErosionRectangle1(drawingOutterRect, out drawingOutterRect, 1, roiStepSize * 3);
                    break;
                case ROI_Direction.Both:
                    HOperatorSet.ErosionRectangle1(drawingOutterRect, out drawingOutterRect, roiStepSize * 3, roiStepSize * 3);
                    break;
            }

            if (hv_value[0] > hv_value[1] && hv_value[0] > 0.9)
            {
                HOperatorSet.ShapeTrans(drawingOutterRect, out drawingOutterRect, "inner_circle");
            }

            parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
            HOperatorSet.RegionFeatures(drawingOutterRect, parmName, out parm);
            tempRegion = drawingOutterRect;
            editROIParm.Region = drawingOutterRect;
            eRoiList.UpdateROI(editROIParm);
            ShowDrawingROI(eRoiList, false);
            drawingObject.SetDrawingObjectParams(parmName, parm);
            hWindow.DispObj(drawingOutterRect);
        }

        /// <summary>
        /// reduce ROI size
        /// </summary>
        /// <param name="direction">direction</param>
        /// <param name="UpdateRegion">update original region</param>
        public void ReduceROI(ROI_Direction direction, bool UpdateRegion = false)
        {
            HTuple parmName, parm;
            HTuple hv_value1 = new HTuple();
            HTuple hv_value2 = new HTuple();
            if (drawingObject == null || !drawingObject.IsInitialized())
                return;
            flag_ZoomRoi = true;


            if (UpdateRegion)
            {
                if (TranslateTempRegion != null && TranslateTempRegion.IsInitialized())
                    tempRegion = TranslateTempRegion;
            }


            HObject drawingOutterRect = new HObject();
            HObject ShapeTransRegion = new HObject();

            parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
            HOperatorSet.ShapeTrans(tempRegion, out ShapeTransRegion, "rectangle1");
            HOperatorSet.RegionFeatures(ShapeTransRegion, parmName, out hv_value1);

            switch (direction)
            {
                case ROI_Direction.Hor:
                    HOperatorSet.ErosionRectangle1(ShapeTransRegion, out drawingOutterRect, roiStepSize * 3, 1);
                    break;
                case ROI_Direction.Vert:
                    HOperatorSet.ErosionRectangle1(ShapeTransRegion, out drawingOutterRect, 1, roiStepSize * 3);
                    break;
                case ROI_Direction.Both:
                    HOperatorSet.ErosionRectangle1(ShapeTransRegion, out drawingOutterRect, roiStepSize * 3, roiStepSize * 3);
                    break;
            }
            HOperatorSet.RegionFeatures(drawingOutterRect, parmName, out hv_value2);

            TranslateTempRegion = AffineTransRegion(tempRegion, hv_value1, hv_value2);

            ShowDrawingROI(eRoiList, false);
            drawingObject.SetDrawingObjectParams(parmName, hv_value2);
            hWindow.DispObj(TranslateTempRegion);
        }

        /// <summary>
        /// reduce ROI size
        /// </summary>
        /// <param name="direction">direction</param>
        /// <param name="RoiList">eRoiList to edit</param>
        public void ReduceROI_Old(ROI_Direction direction, ERoiList RoiList)
        {
            HTuple parmName, parm;
            Dictionary<string, HObject> table1 = new Dictionary<string, HObject>();
            HObject temp = new HObject();
            HObject concatObj = new HObject();
            HTuple hv_value = new HTuple();

            // classify first for acceleration
            for (int k = 0; k < RoiList.Count; k++)
            {
                parmName = new HTuple(new string[] { "circularity", "rectangularity" });
                HOperatorSet.RegionFeatures(RoiList[k].Region, parmName, out hv_value);
                if (hv_value.Length == 0)
                    continue;

                if (hv_value[0] > hv_value[1] && hv_value[0] > 0.9)
                {
                    switch (direction)
                    {
                        case ROI_Direction.Hor:
                            HOperatorSet.ErosionRectangle1(RoiList[k].Region, out temp, roiStepSize * 3, 1);
                            break;
                        case ROI_Direction.Vert:
                            HOperatorSet.ErosionRectangle1(RoiList[k].Region, out temp, 1, roiStepSize * 3);
                            break;
                        case ROI_Direction.Both:
                            HOperatorSet.ErosionRectangle1(RoiList[k].Region, out temp, roiStepSize * 3, roiStepSize * 3);
                            HOperatorSet.ShapeTrans(temp, out temp, "inner_circle");
                            break;
                    }
                }
                else
                {
                    switch (direction)
                    {
                        case ROI_Direction.Hor:
                            HOperatorSet.ErosionRectangle1(RoiList[k].Region, out temp, roiStepSize * 3, 1);
                            break;
                        case ROI_Direction.Vert:
                            HOperatorSet.ErosionRectangle1(RoiList[k].Region, out temp, 1, roiStepSize * 3);
                            break;
                        case ROI_Direction.Both:
                            HOperatorSet.ErosionRectangle1(RoiList[k].Region, out temp, roiStepSize * 3, roiStepSize * 3);
                            break;
                    }
                }

                parmName = new HTuple(new string[] { "row", "column" });
                RoiList[k].Region = temp;

            }
            eRoiList.UpdateROI(RoiList);
            ShowDrawingROI(RoiList, false);
        }


        /// <summary>
        /// reduce ROI list size
        /// </summary>
        /// <param name="direction">direction</param>
        /// <param name="RoiList">eRoiList to edit</param>
        public void ReduceROI(ROI_Direction direction, ERoiList RoiList)
        {
            eRoiList_Clone = RoiList.Clone();    // must clone

            HTuple parmName;
            HObject tempRegion = new HObject();
            HTuple hv_value1 = new HTuple();
            HTuple hv_value2 = new HTuple();
            parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
            HObject ShapeTransRegion = new HObject();

            for (int k = 0; k < RoiList.Count; k++)
            {
                parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
                HOperatorSet.ShapeTrans(RoiList[k].Region, out ShapeTransRegion, "rectangle1");
                HOperatorSet.RegionFeatures(ShapeTransRegion, parmName, out hv_value1);

                if (hv_value1.Length == 0)
                    continue;

                HTuple initialX = new HTuple();
                HTuple initialY = new HTuple();
                HTuple NewX = new HTuple();
                HTuple NewY = new HTuple();
                switch (direction)
                {
                    case ROI_Direction.Hor:
                        HOperatorSet.ErosionRectangle1(ShapeTransRegion, out tempRegion, roiStepSize * 3, 1);
                        break;
                    case ROI_Direction.Vert:
                        HOperatorSet.ErosionRectangle1(ShapeTransRegion, out tempRegion, 1, roiStepSize * 3);
                        break;
                    case ROI_Direction.Both:
                        HOperatorSet.ErosionRectangle1(ShapeTransRegion, out tempRegion, roiStepSize * 3, roiStepSize * 3);
                        break;
                }
                HOperatorSet.RegionFeatures(tempRegion, parmName, out hv_value2);

                tempRegion = AffineTransRegion(RoiList[k].Region, hv_value1, hv_value2);
                //Console.WriteLine($"===========================================================================");
                //Console.WriteLine($"row1: {hv_value1[0].D},column1: {hv_value1[1].D},row2: {hv_value1[2].D},column2: {hv_value1[3].D}");
                //Console.WriteLine($"row1: {hv_value2[0].D},column1: {hv_value2[1].D},row2: {hv_value2[2].D},column2: {hv_value2[3].D}");
                eRoiList_Clone[k].Region = tempRegion;
            }
            ShowDrawingROI(eRoiList_Clone, false);
            #region draw border for better appearance
            int[] colors = new int[4] { 0, 0, 255, 128 };
            HObject rectRegion = new HObject();
            hWindow.SetRgba(colors[0], colors[1], colors[2], colors[3]);
            for (int i = 0; i < eRoiList_Clone.Count; i++)
            {
                HOperatorSet.ShapeTrans(eRoiList_Clone[i].Region, out rectRegion, "rectangle1");
                HOperatorSet.DilationRectangle1(rectRegion, out rectRegion, 5, 5);
                HOperatorSet.Boundary(rectRegion, out rectRegion, "outer");
                hWindow.DispObj(rectRegion);
            }
            #endregion
        }


        /// <summary>
        /// eraser mode
        /// </summary>
        /// <param name="_Type">brush/eraser type</param>
        public void SetBrushOrErase(BrushEraseType _Type)
        {
            if (ho_Source == null)
                return;
            InitialFlag();
            WinOperate = 0;
            eraseType = _Type;

            if (ho_Source_bak == null || !ho_Source_bak.IsInitialized())
            {
                // original image
                ho_Source_bak=(HObject)ho_Source.Clone();
            }

            switch (_Type)
            {
                case BrushEraseType.CircleBrush:
                    flag_Erase_Recovery = false;
                    flag_Erase = true;
                    break;
                case BrushEraseType.RectangleBrush:
                    flag_Erase_Recovery = false;
                    flag_Erase = true;         
                    break;
                case BrushEraseType.CircleErase:
                    flag_Erase_Recovery = true;
                    flag_Erase = true;
                    break;
                case BrushEraseType.RectangleErase:
                    flag_Erase_Recovery = true;
                    flag_Erase = true;
                    break;
            }
        }

        /// <summary>
        /// restore image
        /// </summary>
        public void SourceRecovery()
        {
            if (ho_Source_bak == null || !ho_Source_bak.IsInitialized()) return;
            ho_Source  = (HObject)ho_Source_bak.Clone();
            ho_Source_bak.Dispose();
            ho_Source_bak = null;
            BrushEraseRegion = null;
            ShowSourceImageAndROI();
        }

        /// <summary>
        /// get region drawn by brush [Region]
        /// </summary>
        /// <returns></returns>
        public HObject GetBrushRegion()
        {
            // [new version]
            if (BrushEraseRegion == null)
                return null;
            // determine whether the region is 0
            HTuple H_Area = new HTuple(), H_x = new HTuple(), H_y = new HTuple();
            HOperatorSet.AreaCenter(BrushEraseRegion, out H_Area, out H_y, out H_x);
            if (H_Area.TupleLength()==0)
                return null;
            if (H_Area == 0)
                return null;
            return BrushEraseRegion;


            // [old version] : brush>eraser>brush,brushing again can fail because of logic issues
            //HObject DiffRegion=new HObject();

            //if (BrushRegion == null && EraseRegion == null)
            //{
            //    return null;
            //}
            //else if (BrushRegion != null && EraseRegion != null)
            //{
            //    HOperatorSet.Difference(BrushRegion, EraseRegion, out DiffRegion);

            //    HObject EmptyRegion=new HObject();
            //    HOperatorSet.GenEmptyObj(out EmptyRegion);

            //}
            //else if(BrushRegion != null && EraseRegion == null)
            //{
            //    return BrushRegion;
            //}
            //else
            //{
            //    return null;
            //}

            //// determine whether the region after subtraction is 0
            //HTuple H_Area = new HTuple(), H_x = new HTuple(), H_y = new HTuple();
            //HOperatorSet.AreaCenter(DiffRegion, out H_Area, out H_y, out H_x);

            //if (H_Area==0)
            //    return null;
            //return DiffRegion;
        }
        /// <summary>
        /// magic wand
        /// </summary>
        /// <param name="_type">ROI type</param>
        public void SetMagicWand(RoiType _type)
        {

            // set color
            if (curRoiColor == null)
                curRoiColor = DefaultColor;

            InitialFlag();
            WinOperate = 0;
            flag_MagicWand=true;
            magicWandType=_type;
            SetCursor(Properties.Resources.MagicCursor);
        }

        /// <summary>
        /// Region
        /// </summary>
        /// <param name="region">Region</param>
        /// <param name="OnlyRegion">display only the region</param>
        public void ShowRegion(HObject region,bool OnlyRegion=false)
        {
            _showRegion = region;
            flag_showRegion=true;
            if (OnlyRegion)
            {
                if (_showRegion != null && _showRegion.IsInitialized())
                {
                    hWindow.ClearWindow();
                    ClearSoureImage();
                    hWindow.SetRgba(255, 0, 0, 150);
                    hWindow.DispObj(_showRegion);
                }
            }
            else
            {
                DispBaseHImage();
                //DispSoureImage();
                ShowDrawingROI(eRoiList);
                hWindow.SetRgba(255, 0, 0, 150);
                if (_showRegion != null && _showRegion.IsInitialized())
                    hWindow.DispObj(_showRegion);               
            }            
        }


        /// <summary>
        /// Region
        /// </summary>
        /// <param name="region">Region</param>
        /// <param name="_color">color</param>
        /// <param name="OnlyRegion">display only the region</param>
        public void ShowRegion(HObject region,int[] _color, bool OnlyRegion = false)
        {
            _showRegion = region;
            //if (_showRegion == null)
            //    return;

            curRoiColor = _color;
            flag_showRegion = true;
            if (OnlyRegion)
            {
                if (_showRegion != null && _showRegion.IsInitialized())
                {
                    hWindow.ClearWindow();
                    ClearSoureImage();
                    //hWindow.SetRgba(_color[0], _color[1], _color[2], _color[3]);
                    hWindow.DispObj(_showRegion);
                }
            }
            else
            {
                DispBaseHImage();
                //DispSoureImage();
                ShowDrawingROI(eRoiList);

                hWindow.SetRgba(_color[0], _color[1], _color[2], _color[3]);
                if (_showRegion != null && _showRegion.IsInitialized())
                    hWindow.DispObj(_showRegion);
            }
        }

        /// <summary>
        /// display multiple regions
        /// </summary>
        /// <param name="region">Region</param>
        /// <param name="_color">color</param>
        /// <param name="OnlyRegion">display only the region</param>
        public void ShowRegions(List<HObject> region, List<int[]> _color, bool OnlyRegion = false)
        {

            flag_showRegion = true;
            if (OnlyRegion)
            {
                if (_showRegion != null && _showRegion.IsInitialized())
                {
                    hWindow.ClearWindow();
                    ClearSoureImage();

                    for (int i = 0; i < region.Count; i++)
                    {
                        hWindow.SetRgba(_color[i][0], _color[i][1], _color[i][2], _color[i][3]);
                        hWindow.DispObj(region[i]);
                    }

                }
            }
            else
            {
                DispBaseHImage();
                //DispSoureImage();
                ShowDrawingROI(eRoiList);

                for (int i = 0; i < region.Count; i++)
                {
                    hWindow.SetRgba(_color[i][0], _color[i][1], _color[i][2], _color[i][3]);
                    hWindow.DispObj(region[i]);
                }

                //hWindow.SetRgba(_color[0], _color[1], _color[2], _color[3]);
                //if (_showRegion != null)
                //    hWindow.DispObj(_showRegion);
            }

        }
        #endregion


        #region hSmartWindowControl1 event
        private void hSmartWindowControl1_HMouseDown(object sender, HMouseEventArgs e)
        {
            double Win_y = 0;
            double Win_x = 0;
            hWindow.ConvertCoordinatesImageToWindow((double)e.Y, (double)e.X, out Win_y, out Win_x);
            Point pt = new Point((int)Win_x, (int)Win_y);
            string UserInfo = null;
            if (EMouseDownInfo != null)
            {
                if (e.Y < image_H.D && e.X < image_W.D && 0 <= e.Y && 0 <= e.X)
                {
                    Point Win_Pt = new Point((int)Win_x, (int)Win_y);
                    eMouseEventArgs.Coordinate_Image = new Point((int)e.X, (int)e.Y);
                    eMouseEventArgs.Coordinate_Win = Win_Pt;
                    eMouseEventArgs.Value = grayValue;
                    eMouseEventArgs.MouseButton = e.Button;
                    EMouseDownInfo(eMouseEventArgs,ref UserInfo);
                }
            }

            if (e.Button == MouseButtons.Left)
            {
                switch (winOperate)
                {
                    default:
                        return;
                    case 0:
                        // draw ROI
                        if (flag_DrawingROI)
                        {
                            if (!flag_DrawingROI_FirstMouseDown && !flag_DrawingROI_NextPosIsClick)
                            {
                                // first point
                                DrawingROI_First_y1 = e.Y;
                                DrawingROI_First_x1 = e.X;
                                flag_DrawingROI_FirstMouseDown = true;
                                drawingObject = new HDrawingObject();

                                if (drawingType == RoiType.Polygon)
                                {
                                    Polygon_x.Add(e.X);
                                    Polygon_y.Add(e.Y);
                                }
                            }
                            else if (flag_DrawingROI_FirstMouseDown && !flag_DrawingROI_NextPosIsClick)
                            {
                                // second point
                                flag_DrawingROI_NextPosIsClick = true;
                                if (flag_SelectedROI)
                                {
                                    HObject region = new HObject();
                                    region= Trans_drawingObjectToRegion();
                                    if (region == null)
                                        return;
                                    ERoiList list = GetSelectedRoiList(region);
                                    SelectedROI = list;
                                    if (ESelectedROIList != null)
                                        ESelectedROIList(list);                     
                                    drawingObject.ClearDrawingObject();
                                    InitialFlag();
                                }
                            }
                            return;
                        }


                        // auto draw default ROI flag
                        if (flag_AutoDrawingDefaultROI && flag_EditROI == false)
                        {
                            //ROIParm DefaultROIParm = CreateDefaultROI(curRoiColor);
                            //HOperatorSet.MoveRegion(DefaultROIParm.Region, out DefaultROIParm.Region, e.Y - (DefaultRoiSize / 2), e.X - (DefaultRoiSize / 2));
                            //ConfirmNewROI(DefaultROIParm);
                            defaultROIParm.VisableROI = true;
                            ConfirmNewROI(defaultROIParm);
                            return;
                        }


                        // determine whether this position has an editable ROI
                        if (!flag_DrawingROI && !flag_PasteROI && !flag_MagicWand && !flag_Erase)
                        {
                            if (eRoiList.Count > 0 && !flag_EditROI)
                            {
                                Point SelectPos = new Point((int)e.X, (int)e.Y);
                                EditeRoi_ID = -1;
                                if (CheckClickPosInROI(SelectPos))
                                {
                                    EditRoi(SelectPos);
                                    //flag_AutoDrawingDefaultROI=false;
                                    return;
                                }
                            }
                        }

                        // paste ROI
                        if (flag_PasteROI)
                        {
                            HTuple cx = new HTuple();
                            HTuple cy = new HTuple();
                            HTuple area = new HTuple();
                            HOperatorSet.AreaCenter(CopyROIParm.Region, out area, out cy, out cx);
                            LastRegionCenter = new Point((int)cx.D, (int)cy.D);

                            int xx = (int)e.X - LastRegionCenter.X;
                            int yy = (int)e.Y - LastRegionCenter.Y;

                            HOperatorSet.MoveRegion(CopyROIParm.Region, out tempRegion, yy, xx);
                            drawingType = CopyROIParm._type;
        
                            defaultROIParm = CreateDefaultROI(curRoiColor, drawingType,tempRegion);
                            ConfirmNewROI(defaultROIParm);                        
                        }

                        // eraser
                        if (flag_Erase)
                        {
                            if (ho_Source_bak == null)
                            {
                                // original image
                                ho_Source_bak = (HObject)ho_Source.Clone();
                            }
                            flag_Erase_MouseIsDown = true;
                        }

                        if (flag_MagicWand)
                        {
                            ROIParm rOIParm = new ROIParm();
                            rOIParm.VisibleROIText = visibleROIText;
                            rOIParm._color = (int[])curRoiColor.Clone();
                            rOIParm.Region = MagicWand((int)e.X, (int)e.Y);
                            if (rOIParm.Region == null)
                            {
                                return;
                            }
                            rOIParm._type = magicWandType;
                            rOIParm.VisableROI = visibleROI;

                            AddRoi(rOIParm);
                            if (EROI_Finish != null && eRoiList.Count != 0)
                                EROI_Finish(eRoiList[eRoiList.Count - 1]);
                            WinOperate = 0;
                        }

                        //if (flag_AutoDrawingDefaultROI_continue)
                        //{
                        //    ROIParm DefaultROIParm = CreateDefaultROI(curRoiColor);
                        //    HOperatorSet.MoveRegion(DefaultROIParm.Region, out DefaultROIParm.Region, e.Y - (DefaultRoiSize / 2), e.X - (DefaultRoiSize / 2));
                        //    ConfirmNewROI(DefaultROIParm);
                        //    return;
                        //}
                        break;
                    case 1:

                        break;
                    case 2:
                        ZoomInOutImage(pt);
                        break;
                    case 3:
                        ZoomInOutImage(pt);
                        break;
                    case 4:
                        if (!flag_DrawingROI_FirstMouseDown && !flag_DrawingROI_NextPosIsClick)
                        {
                            // first point
                            DrawingROI_First_y1 = e.Y;
                            DrawingROI_First_x1 = e.X;
                            flag_DrawingROI_FirstMouseDown = true;
                            drawingObject = new HDrawingObject();
                        }
                        else if (flag_DrawingROI_FirstMouseDown && !flag_DrawingROI_NextPosIsClick)
                        {
                            // second point
                            // magnifiermode
                            if (drawingObject.ID == 0)
                            {
                                // indicates drawing failed [same position]
                                return;
                            }
                            HTuple parmName, parm;
                            parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
                            parm = drawingObject.GetDrawingObjectParams(parmName);
                            ShowSize((int)parm.LArr[1], (int)parm.LArr[0], (int)parm.LArr[3], (int)parm.LArr[2]);
                            drawingObject.ClearDrawingObject();
                            // reset flags to zero
                            flag_DrawingROI_FirstMouseDown =false;
                            flag_DrawingROI_NextPosIsClick =false;
                        }
                        break;
                }
                return;
            }

            else if (e.Button== MouseButtons.Right)
            {
                switch (winOperate)
                {
                    default:
                        return;
                    case 0:
                        // draw new ROI
                        if (flag_DrawingROI)
                        {
                            if (flag_DrawingROI_FirstMouseDown && flag_DrawingROI_NextPosIsClick)
                                ConfirmNewROI();
                        }
                        // edit ROI
                        if (flag_EditROI)
                        {
                            if (TranslateTempRegion != null && TranslateTempRegion.IsInitialized())
                            {
                                tempRegion = TranslateTempRegion;
                            }
                            if (tempRegion != null && tempRegion.IsInitialized())
                            {
                                editROIParm.Region = tempRegion;
                            }
                            ConfirmEditROI();
                            TranslateTempRegion = null;
                            tempRegion = null;
                        }

                        // [for increasing/reducing ROI] ROIuse
                        if (eRoiList_Clone!=null && eRoiList_Clone.Count > 0)
                        {
                            eRoiList.UpdateROI(eRoiList_Clone);
                            ShowDrawingROI(eRoiList);
                            drawingType = RoiType.None;
                            InitialFlag();
                            eRoiList_Clone =null;
                            if (EROI_Finish != null && eRoiList.Count != 0)
                                EROI_Finish(null);
                        }

                        break;
                    case 1:

                        break;
                    case 2:
                        break;
                    case 3:
                        break;
                    case 4:
                        break;
                }
            }
        }

        private void hSmartWindowControl1_HMouseMove(object sender, HMouseEventArgs e)
        {
            Point pt = new Point((int)e.X, (int)e.Y);
            double CircleRadius = 0;
            if (ho_Source == null)
                return;

            if (!ho_Source.IsInitialized())
                return;
            string UserInfo = null;

            // default content format
            if (e.Y < image_H.D && e.X < image_W.D && 0 <= e.Y && 0 <= e.X)
            {
                if (ImagePointMoved != null)
                    ImagePointMoved(this, new PointF((float)e.X, (float)e.Y));

                if (enableInfoFromUser)
                {
                }
                else
                {
                    if (EMouseMoveInfo != null)
                    {
                        double Win_y = 0;
                        double Win_x = 0;
                        hWindow.ConvertCoordinatesImageToWindow((double)e.Y, (double)e.X, out Win_y, out Win_x);
                        Point Win_Pt = new Point((int)Win_x, (int)Win_y);
                        eMouseEventArgs.Coordinate_Image = new Point((int)e.X, (int)e.Y);
                        eMouseEventArgs.Coordinate_Win = Win_Pt;
                        eMouseEventArgs.Value = grayValue;
                        eMouseEventArgs.MouseButton = e.Button;
                        EMouseMoveInfo(eMouseEventArgs,ref UserInfo);
                    }

                    if (string.IsNullOrEmpty(UserInfo))
                    {
                        HOperatorSet.GetGrayval(ho_Source, e.Y, e.X, out grayValue);
                        if (_isColor)
                            lb_Info.Text = "[ X: " + e.X.ToString("n0") + " px, Y: " + e.Y.ToString("n0").ToString() + " px ] Color: " + grayValue.ToString();
                        else
                            lb_Info.Text = "[ X: " + e.X.ToString("n0") + " px, Y: " + e.Y.ToString("n0").ToString() + " px ] Gray" + grayValue.ToString();

                    }
                    else
                    {
                        lb_Info.Text = UserInfo;
                    }

                }
            }

            switch (winOperate)
            {
                default:
                    return;
                case 0:
                    // create ROI
                    if (flag_DrawingROI)
                    {
                        if (flag_DrawingROI_FirstMouseDown && !flag_DrawingROI_NextPosIsClick)
                        {
                            //Point pt = new Point((int)e.X, ((int)e.Y));
                            DrawingROIRegion(pt);
                            GetCurROIParms(eRoiList.GetID, drawingType, curRoiColor, true, false, new Point((int)e.X, (int)e.Y));
                        }
                    }
                    // measurement mode
                    if (flag_measure)
                    {
                        if (flag_DrawingROI_FirstMouseDown && flag_DrawingROI_NextPosIsClick)
                            GetCurROIParms(eRoiList.GetID, drawingType, curRoiColor, true, false, new Point((int)e.X, (int)e.Y));
                    }

                    if (flag_EditROI)
                    {
                        //if (!flag_showRegion)
                        //    GetCurROIParms_OutterRect(editROIParm.ID, editROIParm._type, editROIParm._color, editROIParm.VisableROI, editROIParm.VisibleROIText, new Point((int)e.X, (int)e.Y));


                        //GetCurROIParms_OutterRect(editROIParm.ID, editROIParm._type, editROIParm._color, editROIParm.VisableROI, editROIParm.VisibleROIText, new Point((int)e.X, (int)e.Y));
                    }



                    // auto draw default ROI flag
                    if (flag_AutoDrawingDefaultROI && flag_EditROI == false)
                    {
                        int xx = pt.X - LastRegionCenter.X;
                        int yy = pt.Y - LastRegionCenter.Y;

                        // [version 1] [display default ROI]
                        //if (eRoiList.Count == 0)
                        //    hWindow.ClearWindow();
                        //else
                        //    ShowDrawingROI(eRoiList);

                        //HOperatorSet.MoveRegion(defaultROIParm.Region, out defaultROIParm.Region, yy, xx);
                        //hWindow.DispObj(defaultROIParm.Region);
                        //drawingType = defaultROIParm._type;
                        //DrawRoiRegionAndOutterRectangleBorder(defaultROIParm.Region, pt.X, pt.Y);


                        // [version 2] [do not display default ROI]
                        HOperatorSet.MoveRegion(defaultROIParm.Region, out defaultROIParm.Region, yy, xx);
                        drawingType = defaultROIParm._type;

                        //GetCurROIParms(eRoiList.GetID, defaultROIParm, new Point((int)e.X, (int)e.Y));
                        LastRegionCenter = pt;
                        return;
                    }


                    // paste ROI
                    //if (flag_PasteROI)
                    //{

                    //    if (LastRegionCenter == new Point(-1, -1))
                    //    {
                    //        HTuple cx = new HTuple();
                    //        HTuple cy = new HTuple();
                    //        HTuple area = new HTuple();
                    //        HOperatorSet.AreaCenter(tempRegion, out area, out cy, out cx);
                    //        LastRegionCenter = new Point((int)cx.D, (int)cy.D);
                    //    }


                    //    int xx = pt.X - LastRegionCenter.X;
                    //    int yy = pt.Y - LastRegionCenter.Y;

                    //    // [version 1] [display copied ROI]
                    //    //if (eRoiList.Count == 0)
                    //    //    hWindow.ClearWindow();
                    //    //else
                    //    //    ShowDrawingROI(eRoiList);

                    //    //HOperatorSet.MoveRegion(tempRegion, out tempRegion, yy, xx);
                    //    //drawingType = CopyROIParm._type;
                    //    //DrawRoiRegionAndOutterRectangleBorder(tempRegion, pt.X, pt.Y);

                    //    //hWindow.DispObj(tempRegion);
                    //    //LastRegionCenter = pt;

                    //    //GetCurROIParms(eRoiList.GetID, CopyROIParm, new Point((int)e.X, (int)e.Y));

                    //    // [version 2] [do not display copied ROI]
                    //    HOperatorSet.MoveRegion(tempRegion, out tempRegion, yy, xx);
                    //    drawingType = CopyROIParm._type;                    
                    //    LastRegionCenter = pt;

                    //    GetCurROIParms(eRoiList.GetID, CopyROIParm, new Point((int)e.X, (int)e.Y));
                    //}

                    // eraser
                    if (flag_Erase)
                    {
                        //Point pt = new Point((int)e.X, (int)e.Y);

                        switch (eraseType)
                        {
                            case BrushEraseType.CircleBrush:
                                EraseCircle(pt);
                                break;
                            case BrushEraseType.RectangleBrush:
                                EraseRectangle(pt);
                                break;
                            case BrushEraseType.CircleErase:
                                EraseCircle(pt);
                                break;
                            case BrushEraseType.RectangleErase:
                                EraseRectangle(pt);
                                break;
                        }
                        return;
                    }
                    break;
                case 1:

                    break;
                case 2:
                    //ZoomInOutImage(pt);
                    break;
                case 3:
                    //ZoomInOutImage(pt);
                    break;
                case 4:
                    // manually draw ROI
                    if (flag_DrawingROI_FirstMouseDown && !flag_DrawingROI_NextPosIsClick)
                    {
                        //Point pt = new Point((int)e.X, ((int)e.Y));
                        DrawingROIRegion(pt);                
                    }
                    break;
            }
        }

        /// <summary>
        /// draw ROI region and border
        /// </summary>
        /// <param name="region">region</param>
        /// <param name="xx">center</param>
        /// <param name="yy">center</param>
        private void DrawRoiRegionAndOutterRectangleBorder(HObject region,double xx,double yy,bool bFirst=true)
        {
            if (bFirst)
            {
                if (drawingObject == null)
                    drawingObject = new HDrawingObject();
                if (drawingObject.IsInitialized())
                {
                    hWindow.DetachDrawingObjectFromWindow(drawingObject);
                }
            }


            HTuple parmName, parmValue;
            Point NextPos;
            //drawingType = defaultROIParm._type;


            
            Point point_RB = new Point(0, 0);
            Point point_LT=new Point(0,0);

            //parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2", "width", "height", "radius" });
            //HOperatorSet.RegionFeatures(region, parmName, out parmValue);
            //DrawingROI_First_x1 = xx;
            //DrawingROI_First_y1 = yy;
            //NextPos = new Point((int)DrawingROI_First_x1 + (int)parmValue[4].D / 2, (int)DrawingROI_First_y1 + (int)parmValue[4].D / 2);


            //point_LT = new Point((int)parmValue.DArr[1], (int)parmValue.DArr[0]);
            //point_RB = new Point((int)parmValue.DArr[3], (int)parmValue.DArr[2]);

            HObject BorderRegion = new HObject();
            HOperatorSet.ShapeTrans(region, out region, "rectangle1");
            parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2", "width", "height", "radius" });
            HOperatorSet.RegionFeatures(region, parmName, out parmValue);
            point_LT = new Point((int)parmValue.DArr[1], (int)parmValue.DArr[0]);
            point_RB = new Point((int)parmValue.DArr[3], (int)parmValue.DArr[2]);

            if (bFirst)
                DrawingROIRegion2(point_LT, point_RB);
        }
        /// <summary>
        /// draw ROI region and border
        /// </summary>
        /// <param name="ROIParm">ROIParm</param>
        /// <param name="xx">center</param>
        /// <param name="yy">center</param>
        private void DrawRoiRegionAndOutterRectangleBorder(ROIParm parm, double xx, double yy, bool bFirst = true)
        {
            if (bFirst)
            {
                if (drawingObject == null)
                    drawingObject = new HDrawingObject();
                if (drawingObject.IsInitialized())
                {
                    hWindow.DetachDrawingObjectFromWindow(drawingObject);
                }
            }


            HTuple parmName, parmValue;
            Point NextPos;
            //drawingType = defaultROIParm._type;
            HObject region = new HObject();
            drawingType = parm._type;

            Point point_RB = new Point(0, 0);
            Point point_LT = new Point(0, 0);


            HObject BorderRegion = new HObject();
            HOperatorSet.ShapeTrans(parm.Region, out region, "rectangle1");
            parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2", "width", "height", "radius" });
            HOperatorSet.RegionFeatures(region, parmName, out parmValue);
            point_LT = new Point((int)parmValue.DArr[1], (int)parmValue.DArr[0]);
            point_RB = new Point((int)parmValue.DArr[3], (int)parmValue.DArr[2]);

            if (bFirst)
                DrawingROIRegion2(point_LT, point_RB);
        }
        /// <summary>
        /// get current ROI information
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="_ty"></param>
        /// <param name="_color"></param>
        /// <param name="visableRoi"></param>
        /// <param name="visableTxt"></param>
        /// <param name="pt"></param>
        private void GetCurROIParms(int ID, RoiType _ty, int[] _color,bool visableRoi,bool visableTxt, Point pt)
        {
            HTuple parmName, parmValue;
            HObject hRegion = new HObject();
            TempROI = new ROIParm();
            TempROI.ID = ID;
            TempROI._type = _ty;
            TempROI._color = _color;
            TempROI.VisableROI = visableRoi;
            TempROI.VisibleROIText = visableTxt;

            if (drawingObject==null || drawingObject.ID == 0)
                return;

            parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
            parmValue = drawingObject.GetDrawingObjectParams(parmName);

            TempROI.Region = drawingObject.GetDrawingObjectIconic();

            //switch (TempROI._type)
            //{
            //    case RoiType.Line:
            //        if (Math.Abs(pt.Y - DrawingROI_First_y1) > 0 && Math.Abs(pt.X - DrawingROI_First_x1) > 0)
            //        {
            //            parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
            //            parmValue = drawingObject.GetDrawingObjectParams(parmName);
            //            HOperatorSet.GenRegionLine(out hRegion, parmValue[0], parmValue[1], parmValue[2], parmValue[3]);                        
            //        }
            //        else
            //            return;
            //        break;
            //    case RoiType.Rectangle:
            //        if ((pt.Y - DrawingROI_First_y1) > 2 && (pt.X - DrawingROI_First_x1) > 2)
            //        {
            //            parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
            //            parmValue = drawingObject.GetDrawingObjectParams(parmName);
            //            HOperatorSet.GenRectangle1(out hRegion, parmValue[0], parmValue[1], parmValue[2], parmValue[3]);
            //        }
            //        else
            //            return;
            //        break;
            //    case RoiType.Circle:
            //        if ((pt.Y - DrawingROI_First_y1) > 2 && (pt.X - DrawingROI_First_x1) > 2)
            //        {
            //            parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
            //            parmValue = drawingObject.GetDrawingObjectParams(parmName);
            //            HOperatorSet.GenRectangle1(out hRegion, parmValue[0], parmValue[1], parmValue[2], parmValue[3]);
            //        }
            //        else
            //            return;

            //        //if (Math.Abs(pt.Y - DrawingROI_First_y1) > 2)
            //        //{
            //        //    parmName = new HTuple(new string[] { "row", "column", "radius" });
            //        //    parmValue = drawingObject.GetDrawingObjectParams(parmName);
            //        //    HOperatorSet.GenCircle(out hRegion, parmValue[0], parmValue[1], parmValue[2]);
            //        //}
            //        //else
            //        //    return;
            //        break;
            //    case RoiType.Polygon:
            //        parmName = new HTuple(new string[] { "row", "column", });
            //        if (drawingObject.ID != 0)
            //        {
            //            parmValue = drawingObject.GetDrawingObjectParams(parmName);
            //            hRegion = null;
            //        }
            //        else
            //            parmValue = null;
            //        break;
            //    default:
            //        return;
            //}

            //bool flag_IsChanged=false;

            //if (hRegion != null)
            //{
            //    flag_IsChanged = CheckEditeParmIsChanged(parmName, parmValue);
            //    LastEditeDrawingRebion = hRegion.Clone();
            //}

            if (flag_SelectedROI == false)
            {

                if (flag_measure)
                {
                    HTuple Cx = new HTuple();
                    HTuple Cy = new HTuple();
                    Cx = (parmValue[1] + parmValue[3]) / 2;
                    Cy = (parmValue[0] + parmValue[2]) / 2;
                    HTuple distance = new HTuple();
                    HOperatorSet.DistancePp(parmValue[0], parmValue[1], parmValue[2], parmValue[3], out distance);
                    if (LastDistance != distance.D)
                    {
                        if (EMeasureResponse != null)
                            EMeasureResponse(distance.D);
                    }
                    LastDistance = distance.D;
                }
                else
                {
                    if (EAllowEditROI != null)
                        EAllowEditROI(TempROI);
                }

                //if (TempROI._type != RoiType.Polygon)
                //{
                //    TempROI.Region = hRegion;
                //    if (flag_IsChanged)
                //    {
                //        if (flag_measure)
                //        {
                //            HTuple Cx = new HTuple();
                //            HTuple Cy = new HTuple();
                //            Cx = (parmValue[1] + parmValue[3]) / 2;
                //            Cy = (parmValue[0] + parmValue[2]) / 2;
                //            HTuple distance = new HTuple();
                //            HOperatorSet.DistancePp(parmValue[0], parmValue[1], parmValue[2], parmValue[3], out distance);
                //            if (LastDistance != distance.D)
                //            {
                //                if (EMeasureResponse != null)
                //                    EMeasureResponse(distance.D);
                //            }
                //            LastDistance = distance.D;
                //        }
                //        else
                //        {
                //            if (EAllowEditROI != null)
                //                EAllowEditROI(TempROI);
                //        }
                //    }
                //    //else
                //    //{
                //    //    if (EAllowEditROI != null)
                //    //        EAllowEditROI(TempROI);
                //    //}

                //}
                //else
                //{
                //    //if (flag_IsChanged)
                //    //{
                //    //    if (EAllowEditROI != null)
                //    //        EAllowEditROI(TempROI);
                //    //}
                //    if (EAllowEditROI != null)
                //        EAllowEditROI(TempROI);
                //}
            }

        }

        /// <summary>
        /// get current ROI information
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="rOIParm">ROIParm</param>
        /// <param name="pt"></param>
        private void GetCurROIParms(int ID, ROIParm rOIParm, Point pt)
        {
            HTuple parmName, parmValue;
            HObject hRegion = new HObject();
            TempROI = new ROIParm();
            TempROI.ID = ID;
            TempROI._type = rOIParm._type;
            TempROI._color = rOIParm._color;
            TempROI.VisableROI = rOIParm.VisableROI;
            TempROI.VisibleROIText = rOIParm.VisibleROIText;

            if (drawingObject == null || drawingObject.ID == 0)
                return;

            parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
            parmValue = drawingObject.GetDrawingObjectParams(parmName);

            TempROI.Region = drawingObject.GetDrawingObjectIconic();

            
            if (flag_SelectedROI == false)
            {

                if (flag_measure)
                {
                    HTuple Cx = new HTuple();
                    HTuple Cy = new HTuple();
                    Cx = (parmValue[1] + parmValue[3]) / 2;
                    Cy = (parmValue[0] + parmValue[2]) / 2;
                    HTuple distance = new HTuple();
                    HOperatorSet.DistancePp(parmValue[0], parmValue[1], parmValue[2], parmValue[3], out distance);
                    if (LastDistance != distance.D)
                    {
                        if (EMeasureResponse != null)
                            EMeasureResponse(distance.D);
                    }
                    LastDistance = distance.D;
                }
                else
                {
                    if (EAllowEditROI != null)
                        EAllowEditROI(TempROI);
                }

                
            }

        }
        /// <summary>
        /// get current ROI information
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="_ty"></param>
        /// <param name="_color"></param>
        /// <param name="visableRoi"></param>
        /// <param name="visableTxt"></param>
        /// <param name="pt"></param>
        private void GetCurROIParms_OutterRect(int ID, RoiType _ty, int[] _color, bool visableRoi, bool visableTxt, Point pt)
        {
            //HTuple parmName, parmValue;
            //HObject hRegion = new HObject();
            //TempROI = new ROIParm();
            //TempROI.ID = ID;
            //TempROI._type = _ty;
            //TempROI._color = _color;
            //TempROI.VisableROI = visableRoi;
            //TempROI.VisibleROIText = visableTxt;

            //if (drawingObject == null || drawingObject.ID == 0)
            //    return;


            //switch (TempROI._type)
            //{
            //    case RoiType.Line:
            //        if (Math.Abs(pt.Y - DrawingROI_First_y1) > 0 && Math.Abs(pt.X - DrawingROI_First_x1) > 0)
            //        {
            //            parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
            //            parmValue = drawingObject.GetDrawingObjectParams(parmName);
            //            HOperatorSet.GenRegionLine(out hRegion, parmValue[0], parmValue[1], parmValue[2], parmValue[3]);
            //        }
            //        else
            //            return;
            //        break;
            //    case RoiType.Rectangle:
            //        if ((pt.Y - DrawingROI_First_y1) > 2 && (pt.X - DrawingROI_First_x1) > 2)
            //        {
            //            parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
            //            parmValue = drawingObject.GetDrawingObjectParams(parmName);
            //            HOperatorSet.GenRectangle1(out hRegion, parmValue[0], parmValue[1], parmValue[2], parmValue[3]);
            //        }
            //        else
            //            return;
            //        break;
            //    case RoiType.Circle:
            //        if ((pt.Y - DrawingROI_First_y1) > 2 && (pt.X - DrawingROI_First_x1) > 2)
            //        {
            //            parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
            //            parmValue = drawingObject.GetDrawingObjectParams(parmName);
            //            HOperatorSet.GenRectangle1(out hRegion, parmValue[0], parmValue[1], parmValue[2], parmValue[3]);
            //        }
            //        else
            //            return;

            //        //if (Math.Abs(pt.Y - DrawingROI_First_y1) > 2)
            //        //{
            //        //    parmName = new HTuple(new string[] { "row", "column", "radius" });
            //        //    parmValue = drawingObject.GetDrawingObjectParams(parmName);
            //        //    HOperatorSet.GenCircle(out hRegion, parmValue[0], parmValue[1], parmValue[2]);
            //        //}
            //        //else
            //        //    return;
            //        break;
            //    case RoiType.Polygon:
            //        parmName = new HTuple(new string[] { "row", "column", });
            //        if (drawingObject.ID != 0)
            //        {
            //            parmValue = drawingObject.GetDrawingObjectParams(parmName);
            //            hRegion = null;
            //        }
            //        else
            //            parmValue = null;
            //        break;
            //    default:
            //        return;
            //}

            //bool flag_IsChanged = false;

            //if (hRegion != null)
            //{
            //    flag_IsChanged = CheckEditeParmIsChanged(parmName, parmValue);
            //    LastEditeDrawingRebion = hRegion.Clone();
            //}

            //if (flag_SelectedROI == false)
            //{
            //    if (TempROI._type != RoiType.Polygon)
            //    {
            //        TempROI.Region = hRegion;
            //        if (flag_IsChanged)
            //        {
            //            if (flag_measure)
            //            {
            //                HTuple Cx = new HTuple();
            //                HTuple Cy = new HTuple();
            //                Cx = (parmValue[1] + parmValue[3]) / 2;
            //                Cy = (parmValue[0] + parmValue[2]) / 2;
            //                HTuple distance = new HTuple();
            //                HOperatorSet.DistancePp(parmValue[0], parmValue[1], parmValue[2], parmValue[3], out distance);
            //                if (LastDistance != distance.D)
            //                {
            //                    if (EMeasureResponse != null)
            //                        EMeasureResponse(distance.D);
            //                }
            //                LastDistance = distance.D;
            //            }
            //            else
            //            {
            //                if (EAllowEditROI != null)
            //                    EAllowEditROI(TempROI);
            //            }
            //        }
            //        //else
            //        //{
            //        //    if (EAllowEditROI != null)
            //        //        EAllowEditROI(TempROI);
            //        //}

            //    }
            //    else
            //    {
            //        if (flag_IsChanged)
            //        {
            //            if (EAllowEditROI != null)
            //                EAllowEditROI(TempROI);
            //        }
            //    }
            //}










            HTuple parmName, parmValue;
            HObject hRegion = new HObject();
            TempROI = new ROIParm();
            TempROI.ID = ID;
            TempROI._type = _ty;
            TempROI._color = _color;
            TempROI.VisableROI = visableRoi;
            TempROI.VisibleROIText = visableTxt;

            if (drawingObject == null || drawingObject.ID == 0)
                return;
            parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
            parmValue = drawingObject.GetDrawingObjectParams(parmName);
            hRegion = drawingObject.GetDrawingObjectIconic();

            bool flag_IsChanged = false;


            if (flag_SelectedROI == false)
            {
                if (TempROI._type != RoiType.Polygon)
                {
                    TempROI.Region = tempRegion;
                    TempROI.RegionBorder =GenerateRegionBorder(tempRegion);
 
                    if (flag_measure)
                    {
                        HTuple Cx = new HTuple();
                        HTuple Cy = new HTuple();
                        Cx = (parmValue[1] + parmValue[3]) / 2;
                        Cy = (parmValue[0] + parmValue[2]) / 2;
                        HTuple distance = new HTuple();
                        HOperatorSet.DistancePp(parmValue[0], parmValue[1], parmValue[2], parmValue[3], out distance);
                        if (LastDistance != distance.D)
                        {
                            if (EMeasureResponse != null)
                                EMeasureResponse(distance.D);
                        }
                        LastDistance = distance.D;
                    }
                    else
                    {
                        if (EAllowEditROI != null)
                            EAllowEditROI(TempROI);
                    }
                }
                else
                {
                    if (flag_IsChanged)
                    {
                        if (EAllowEditROI != null)
                            EAllowEditROI(TempROI);
                    }
                }
            }



        }

        /// <summary>
        /// update edited ROI information
        /// </summary>
        private bool UpdateEditeROI(Point pt)
        {

            HTuple parmName, parmValue;
            HObject hRegion = new HObject();
            
            if (drawingObject == null || drawingObject.ID == 0)
                return false;

            if (editROIParm == null)
                return false;

            switch (editROIParm._type)
            {
                case RoiType.Line:
                    if (Math.Abs(pt.Y - DrawingROI_First_y1) > 0 && Math.Abs(pt.X - DrawingROI_First_x1) > 0)
                    {
                        parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
                        parmValue = drawingObject.GetDrawingObjectParams(parmName);
                        HOperatorSet.GenRegionLine(out hRegion, parmValue[0], parmValue[1], parmValue[2], parmValue[3]);
                    }
                    else
                        return false;
                    break;
                case RoiType.Rectangle:
                    if (Math.Abs(pt.Y - DrawingROI_First_y1) > 2 && Math.Abs(pt.X - DrawingROI_First_x1) > 2)
                    {
                        parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
                        parmValue = drawingObject.GetDrawingObjectParams(parmName);
                        HOperatorSet.GenRectangle1(out hRegion, parmValue[0], parmValue[1], parmValue[2], parmValue[3]);
                    }
                    else
                        return false;
                    break;
                case RoiType.Circle:

                    //if (Math.Abs(pt.Y - DrawingROI_First_y1) > 2 && Math.Abs(pt.X - DrawingROI_First_x1) > 2)
                    //{
                    //    parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
                    //    parmValue = drawingObject.GetDrawingObjectParams(parmName);
                    //    HOperatorSet.GenCircle(out hRegion, parmValue[0], parmValue[1], parmValue[2]);
                    //}
                    //else
                    //    return false;

                    //if (Math.Abs(pt.Y - DrawingROI_First_y1) > 2)
                    //{
                    //    parmName = new HTuple(new string[] { "row", "column", "radius" });
                    //    parmValue = drawingObject.GetDrawingObjectParams(parmName);
                    //    HOperatorSet.GenCircle(out hRegion, parmValue[0], parmValue[1], parmValue[2]);
                    //}
                    //else
                    //    return false;
                    break;
                //case RoiType.Polygon:
                    //parmName = new HTuple(new string[] { "row", "column", });
                    //if (drawingObject.ID != 0)
                    //{
                    //    parmValue = drawingObject.GetDrawingObjectParams(parmName);
                    //    hRegion = null;
                    //}
                    //else
                        //return false;
                    //break;
                default:
                    return false;
            }

            if (hRegion==null || !hRegion.IsInitialized())
            {
                return false;
            }
            HTuple iswqual = new HTuple();
            HOperatorSet.TestEqualRegion(editROIParm.Region, hRegion, out iswqual);

         

            if (iswqual == 1)
            {
                return false;
            }
            else
            {
                editROIParm.Region = hRegion;
                return true;
            }
        }

        /// <summary>
        /// check whether ROI changed
        /// </summary>
        /// <param name="parmName">current parameter</param>
        /// <param name="parmValue">current parameter value</param>
        /// <returns></returns>
        private bool CheckEditeParmIsChanged(HTuple parmName, HTuple parmValue)
        {
            HTuple Last_parmName, Last_parmValue;
            if (LastEditeDrawingRebion != null)
            {
                switch (TempROI._type)
                {
                    case RoiType.Line:
                        Last_parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
                        HOperatorSet.RegionFeatures(LastEditeDrawingRebion, Last_parmName, out Last_parmValue);
                        break;
                    case RoiType.Rectangle:
                        Last_parmName = new HTuple(new string[] { "row1", "column1", "row2", "column2" });
                        HOperatorSet.RegionFeatures(LastEditeDrawingRebion, Last_parmName, out Last_parmValue);
                        break;
                    case RoiType.Circle:
                        Last_parmName = new HTuple(new string[] { "row", "column", "radius" });
                        HOperatorSet.RegionFeatures(LastEditeDrawingRebion, Last_parmName, out Last_parmValue);
                        break;
                    case RoiType.Polygon:
                        Last_parmName = new HTuple(new string[] { "row", "column", });
                        HOperatorSet.RegionFeatures(LastEditeDrawingRebion, Last_parmName, out Last_parmValue);
                        break;
                    default:
                        return false;
                }

                // determine whether it is consistent
                if ((int)(new HTuple(Last_parmValue.TupleEqual(parmValue))) != 0)
                    return false;
                else
                    return true;
            }
            else
                return true;
        }

        private void hSmartWindowControl1_HMouseUp(object sender, HMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (winOperate == 0)
                {
                    if (flag_Erase)
                    {
                        //eraser mode
                        flag_Erase_MouseIsDown = false;         
                    }

                    if (flag_EditROI)
                    {
                        if ( UpdateEditeROI(new Point((int)e.X, (int)e.Y)))
                        {
                            // [for testing]
                            //HTuple parmName = new HTuple(new string[] { "row", "column" });
                            //HTuple parmValue = new HTuple();
                            //HOperatorSet.RegionFeatures(editROIParm.Region, parmName, out parmValue);
                            //Console.WriteLine("Row: " + parmValue.DArr[0].ToString() + "; Col: " + parmValue.DArr[1].ToString());
                            //Console.WriteLine("ETempROIFinishHandler");
                            if (ETempROIFinish != null)
                                ETempROIFinish();
                        }
     
                    }
                }

                if (winOperate == 1)
                {
                    //CrossLine();
                    if (EDragMoveImageFinish != null)
                        EDragMoveImageFinish();
                    if (EWinldowShowChanged != null)
                        EWinldowShowChanged();
                }
                //CrossLine();
            }
        }

        private void hSmartWindowControl1_MouseEnter(object sender, EventArgs e)
        {
            if (!HotKeyIsRegister)
            {
                HotKeyIsRegister = true;
                RegisterHotKey();
                //Console.WriteLine("MouseEnter");
            }

        }

        private void hSmartWindowControl1_MouseLeave(object sender, EventArgs e)
        {
            if (HotKeyIsRegister)
            {
                HotKeyIsRegister = false;
                UnRegisterHotKey();
                //Console.WriteLine("MouseLeave");
            }

            //Console.WriteLine("MouseLeave");
        }
        #endregion

        private void hSmartWindowControl1_HInitWindow(object sender, EventArgs e)
        {
            eRoiList = new ERoiList();
            image_W = new HTuple();
            image_H=new HTuple();
            // display at most 50 items by default
            hSmartWindowControl1.HalconWindow.SetWindowParam("graphics_stack_max_element_num", 100000);
            //hSmartWindowControl1.HalconWindow.SetWindowParam("background_color", "dark olive green");
            HOperatorSet.SetSystem("clip_region", "false"); // ,regions beyond the image range will be clipped
        }

    }
}

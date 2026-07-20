using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EWindowControl
{
    /// <summary>
    /// hotkey command table
    /// </summary>
    public enum CmdHotKey
    {
        /// <summary>
        /// none
        /// </summary>
        None,
        /// <summary>
        /// copy
        /// </summary>
        Copy,
        /// <summary>
        /// paste
        /// </summary>
        Paste,
        /// <summary>
        /// cancel
        /// </summary>
        Cancel,
        /// <summary>
        /// delete
        /// </summary>
        Delete,
    }

    /// <summary>
    /// ROI type
    /// </summary>
    public enum RoiType
    {
        /// <summary>
        /// ROI type is empty[do not draw]
        /// </summary>
        None,
        /// <summary>
        /// mouse crosshair
        /// </summary>
        Cursor,
        /// <summary>
        /// draw line
        /// </summary>
        Line,
        /// <summary>
        /// rectangle
        /// </summary>
        Rectangle,
        /// <summary>
        /// circle
        /// </summary>
        Circle,
        /// <summary>
        /// free rectangle
        /// </summary>
        Polygon,
        /// <summary>
        /// ring
        /// </summary>
        Ring,
        /// <summary>
        /// three circles
        /// </summary>
        ThreeCircle,
        /// <summary>
        /// eraser
        /// </summary>
        Erase,
        /// <summary>
        /// Region
        /// </summary>
        Region,
        /// <summary>
        /// center line
        /// </summary>
        CenterLine,
    }

    /// <summary>
    /// ROI direction
    /// </summary>
    public enum ROI_Direction
    {
        /// <summary>
        /// none
        /// </summary>
        None,
        /// <summary>
        /// horizontal
        /// </summary>
        Hor,
        /// <summary>
        /// vertical
        /// </summary>
        Vert,
        /// <summary>
        /// bidirectional
        /// </summary>
        Both
    }

    /// <summary>
    /// brush/eraser type
    /// </summary>
    public enum BrushEraseType
    {
        /// <summary>
        /// none
        /// </summary>
        None,
        /// <summary>
        /// circular brush
        /// </summary>
        CircleBrush,
        /// <summary>
        /// rectangular brush
        /// </summary>
        RectangleBrush,
        /// <summary>
        /// circular eraser
        /// </summary>
        CircleErase,
        /// <summary>
        /// rectangular eraser
        /// </summary>
        RectangleErase,
    }

    /// <summary>
    /// ROI parameters
    /// </summary>
    public class ROIParm
    {
        /// <summary>
        /// ROI serial number
        /// </summary>
        public int ID;
        /// <summary>
        /// ROI type
        /// </summary>
        public RoiType _type;        
        /// <summary>
        /// display ROI
        /// </summary>
        public bool VisableROI;
        /// <summary>
        /// Region
        /// </summary>
        public HObject Region;
        /// <summary>
        /// region border
        /// </summary>
        public HObject RegionBorder;
        /// <summary>
        /// OutterRect
        /// </summary>
        public RectangleF OutterRect;
        /// <summary>
        /// ROI mark color
        /// </summary>
        public int[] _color;
        /// <summary>
        /// display ROI number
        /// </summary>
        public bool VisibleROIText;
        /// <summary>
        /// [Lock] cannot edit [not complete]
        /// </summary>
        public bool Lock;
    }

    /// <summary>
    /// 
    /// </summary>
    public struct RegionSize
    {
        /// <summary>
        /// upper left
        /// </summary>
        public Point LeftTop;
        /// <summary>
        /// lower right
        /// </summary>
        public Point RightBottom;
        /// <summary>
        /// center
        /// </summary>
        public Point Center;
        /// <summary>
        /// radius
        /// </summary>
        public double Radius;
    }

    /// <summary>
    /// mouse-move return information
    /// </summary>
    public struct EMouseEventArgs
    {
        /// <summary>
        /// image coordinates
        /// </summary>
        public Point Coordinate_Image;
        /// <summary>
        /// window coordinates
        /// </summary>
        public Point Coordinate_Win;
        /// <summary>
        /// grayscale/color
        /// </summary>
        public HTuple Value;
        /// <summary>
        /// mouse button
        /// </summary>
        public System.Windows.Forms.MouseButtons MouseButton;
    }
}

using System.Drawing;

namespace EasyFile
{
    public enum BinType
    {
        None = 0,
        Good = 1,
        Bad = 2,
        No = 3,
        Uninspected = 4,
        EdgeDie = 5,
        RefDie = 6
    }

    public enum FileType
    {
        None = 0,
        Sinf = 1
    }

    public class BinInfo
    {
        private string bin;
        public BinType Type { get; set; }
        public string Bin { get { return bin; } set { bin = value; } }
        public string Remark { get; set; }
        public Color BinColor { get; set; }
        public string Hotkey { get; set; }
        public int HotkeyID { get; set; }
        public Judgment JudgmentCriteria { get; set; }
    }

    public class Judgment
    {
        public int SequenceID { get; set; }
        public JudgmentParms[] menu { get; set; }
    }

    public class JudgmentParms
    {
        public string Name { get; set; }
        public double MaxTol { get; set; }
        public double MinTol { get; set; }
        public string Operator { get; set; }
    }

    public class BinCodeTable
    {
        public BinInfo[] BinInfoTable { get; set; }
    }

    public class CompleteInspectionResult
    {
        public InspectionResult[] menu { get; set; }
    }

    public class InspectionResult
    {
        public string Die_index { get; set; }
        public int DF_cx { get; set; }
        public int DF_cy { get; set; }
        public int DF_Area { get; set; }
        public int DF_width { get; set; }
        public int DF_height { get; set; }
        public string DF_Layer { get; set; }
        public string DF_Type { get; set; }
        public double wx { get; set; }
        public double wy { get; set; }
        public double DMS_x { get; set; }
        public double DMS_y { get; set; }
        public double DMS_Angle { get; set; }
        public int DMS_PattenMatchType { get; set; }
        public double cx { get; set; }
        public double cy { get; set; }
        public double grade { get; set; }
        public int IsFail { get; set; }
        public string Visit { get; set; }
    }

    public struct HeaderInfo
    {
        public string DEVICE;
        public string LOT;
        public string WAFER;
        public int FNLOC;
        public int ROWCT;
        public int COLCT;
        public string[] BCEQU;
        public int REFPX;
        public int REFPY;
        public string DUTMS;
        public int DIECT;
        public double XDIES;
        public double YDIES;
    }
}

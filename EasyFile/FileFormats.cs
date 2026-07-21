using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EasyFile
{
    public interface IEFile
    {
        FileType Type { get; set; }
        HeaderInfo HInfo { get; set; }
        bool LoadFile(string path);
        bool SaveFile(string path);
    }

    public class ContrelFile : IEFile
    {
        private JFile jFile;
        private CompleteInspectionResult aoiResult;

        public FileType Type { get; set; }
        public HeaderInfo HInfo { get; set; }
        public CompleteInspectionResult AoiResult { get { return aoiResult; } set { aoiResult = value; } }

        public ContrelFile()
        {
            jFile = new JFile();
            aoiResult = new CompleteInspectionResult();
        }

        public bool LoadFile(string path)
        {
            try { AoiResult = jFile.ReadJsonFile<CompleteInspectionResult>(path); return true; }
            catch { return false; }
        }

        public bool SaveFile(string path)
        {
            try { jFile.WriteJsonFile(path, AoiResult, true); return true; }
            catch { return false; }
        }
    }

    public class SinfFileFormat : IEFile
    {
        private char[] delimiterChars;
        private string[][] RowData;
        private FileType _Type;
        private HeaderInfo hinfo;
        private JsonConveter JsonConveter;

        public FileType Type { get { return _Type; } set { _Type = value; } }
        public HeaderInfo HInfo { get { return hinfo; } set { hinfo = value; } }
        public string[][] BinCodeData { get { return RowData; } set { RowData = value; } }

        public SinfFileFormat()
        {
            delimiterChars = new[] { ' ', ',', ':' };
            JsonConveter = new JsonConveter();
            _Type = FileType.Sinf;
        }

        public bool LoadFile(string path)
        {
            try { ReadSinfFile(path); return true; }
            catch { return false; }
        }

        public bool SaveFile(string path)
        {
            try
            {
                using (var writer = new StreamWriter(path))
                {
                    SaveHeaderInfo(writer);
                    SaveRowData(writer);
                }
                return true;
            }
            catch { return false; }
        }

        private void LoadHeaderInfo(ref HeaderInfo info, string temp)
        {
            if (string.IsNullOrWhiteSpace(temp) || !temp.Contains(":")) return;
            var parts = temp.Split(new[] { ':' }, 2);
            var key = parts[0].Trim();
            var value = parts.Length > 1 ? parts[1].Trim() : string.Empty;
            int i; double d;
            switch (key)
            {
                case "DEVICE": info.DEVICE = value; break;
                case "LOT": info.LOT = value; break;
                case "WAFER": info.WAFER = value; break;
                case "FNLOC": if (int.TryParse(value, out i)) info.FNLOC = i; break;
                case "ROWCT": if (int.TryParse(value, out i)) info.ROWCT = i; break;
                case "COLCT": if (int.TryParse(value, out i)) info.COLCT = i; break;
                case "BCEQU": info.BCEQU = value.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries); break;
                case "REFPX": if (int.TryParse(value, out i)) info.REFPX = i; break;
                case "REFPY": if (int.TryParse(value, out i)) info.REFPY = i; break;
                case "DUTMS": info.DUTMS = value; break;
                case "DIECT": if (int.TryParse(value, out i)) info.DIECT = i; break;
                case "XDIES": if (double.TryParse(value, out d)) info.XDIES = d; break;
                case "YDIES": if (double.TryParse(value, out d)) info.YDIES = d; break;
            }
        }

        private void ReadSinfFile(string path)
        {
            var rows = new List<string[]>();
            var lines = File.ReadAllLines(path);
            bool rowMode = false;
            foreach (var line in lines)
            {
                var temp = line.Trim();
                if (temp.Length == 0) continue;
                if (temp.StartsWith("RowData", StringComparison.OrdinalIgnoreCase)) { rowMode = true; continue; }
                if (!rowMode) LoadHeaderInfo(ref hinfo, temp);
                else rows.Add(temp.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries));
            }
            RowData = rows.ToArray();
        }

        private void SaveHeaderInfo(StreamWriter writer)
        {
            writer.WriteLine("DEVICE:" + hinfo.DEVICE);
            writer.WriteLine("LOT:" + hinfo.LOT);
            writer.WriteLine("WAFER:" + hinfo.WAFER);
            writer.WriteLine("FNLOC:" + hinfo.FNLOC);
            writer.WriteLine("ROWCT:" + hinfo.ROWCT);
            writer.WriteLine("COLCT:" + hinfo.COLCT);
            writer.WriteLine("BCEQU:" + (hinfo.BCEQU == null ? string.Empty : string.Join(" ", hinfo.BCEQU)));
            writer.WriteLine("REFPX:" + hinfo.REFPX);
            writer.WriteLine("REFPY:" + hinfo.REFPY);
            writer.WriteLine("DUTMS:" + hinfo.DUTMS);
            writer.WriteLine("DIECT:" + hinfo.DIECT);
            writer.WriteLine("XDIES:" + hinfo.XDIES);
            writer.WriteLine("YDIES:" + hinfo.YDIES);
            writer.WriteLine("RowData:");
        }

        private void SaveRowData(StreamWriter writer)
        {
            if (RowData == null) return;
            foreach (var row in RowData)
                writer.WriteLine(row == null ? string.Empty : string.Join(" ", row));
        }
    }
}

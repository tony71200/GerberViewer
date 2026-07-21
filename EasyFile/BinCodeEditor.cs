using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EasyFile
{
    public class BinCodeEditor
    {
        private BinCodeTable binTable;
        private List<BinInfo> binInfoList;
        private JsonConveter JsonConveter;
        private char[] delimiterChars;
        private int HotKeyInitialID = 1000;

        public BinCodeTable BinTable
        {
            get
            {
                if (binTable == null) binTable = new BinCodeTable();
                binTable.BinInfoTable = binInfoList.ToArray();
                return binTable;
            }
            set
            {
                binTable = value ?? new BinCodeTable();
                binInfoList = binTable.BinInfoTable != null ? binTable.BinInfoTable.ToList() : new List<BinInfo>();
            }
        }

        public BinCodeEditor()
        {
            binInfoList = new List<BinInfo>();
            JsonConveter = new JsonConveter();
            binTable = new BinCodeTable();
            delimiterChars = new[] { ',', ':', ' ' };
        }

        public bool SetBinCode(BinInfo binInfo, string binID)
        {
            if (binInfo == null) return false;
            binInfo.Bin = binID;
            if (!CheckBin(binInfo)) return false;
            binInfo.HotkeyID = GetLastHotKeyID() + 1;
            binInfoList.Add(binInfo);
            binTable.BinInfoTable = binInfoList.ToArray();
            return true;
        }

        public bool UpdateBinCode(BinInfo OldbinInfo, BinInfo NewbinInfo)
        {
            if (OldbinInfo == null || NewbinInfo == null) return false;
            var idx = binInfoList.FindIndex(x => string.Equals(x.Bin, OldbinInfo.Bin, StringComparison.OrdinalIgnoreCase));
            if (idx < 0) return false;
            NewbinInfo.HotkeyID = OldbinInfo.HotkeyID > 0 ? OldbinInfo.HotkeyID : GetLastHotKeyID() + 1;
            binInfoList[idx] = NewbinInfo;
            binTable.BinInfoTable = binInfoList.ToArray();
            return true;
        }

        public bool DeleteBinCode(BinInfo binInfo)
        {
            if (binInfo == null) return false;
            var removed = binInfoList.RemoveAll(x => string.Equals(x.Bin, binInfo.Bin, StringComparison.OrdinalIgnoreCase)) > 0;
            binTable.BinInfoTable = binInfoList.ToArray();
            return removed;
        }

        public bool EditBinCode(BinInfo OldbinInfo, BinInfo NewbinInfo)
        {
            return UpdateBinCode(OldbinInfo, NewbinInfo);
        }

        public bool CheckBin(string bin)
        {
            if (string.IsNullOrWhiteSpace(bin)) return false;
            return !binInfoList.Any(x => string.Equals(x.Bin, bin, StringComparison.OrdinalIgnoreCase));
        }

        public bool CheckBin(BinInfo info)
        {
            if (info == null) return false;
            if (string.IsNullOrWhiteSpace(info.Bin)) return false;
            return !binInfoList.Any(x => !ReferenceEquals(x, info) && string.Equals(x.Bin, info.Bin, StringComparison.OrdinalIgnoreCase));
        }

        public void SaveBinCodeTable(string path, bool SaveCSV)
        {
            var jsonPath = Path.ChangeExtension(path, ".json");
            var jfile = new JFile();
            jfile.WriteJsonFile(jsonPath, BinTable, true);
            if (SaveCSV) SaveCsv(Path.ChangeExtension(path, ".csv"));
        }

        public void LoadBinCodeTable(string path)
        {
            var jsonPath = Path.ChangeExtension(path, ".json");
            var jfile = new JFile();
            BinTable = jfile.ReadJsonFile<BinCodeTable>(jsonPath);
        }

        public void SaveCsv(string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            using (var writer = new StreamWriter(path))
            {
                writer.WriteLine("Type,BinCode,Remark");
                foreach (var item in binInfoList)
                    writer.WriteLine(string.Join(",", item.Type, item.Bin, item.Remark));
            }
        }

        public int GetLastHotKeyID()
        {
            if (binInfoList.Count == 0) return HotKeyInitialID;
            return Math.Max(HotKeyInitialID, binInfoList.Max(x => x.HotkeyID));
        }

        public int GetLastBinCodeID()
        {
            int max = 0;
            foreach (var bin in binInfoList.Select(x => x.Bin))
            {
                int parsed;
                if (int.TryParse(bin, out parsed)) max = Math.Max(max, parsed);
            }
            return max;
        }
    }
}

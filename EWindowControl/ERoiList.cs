using HalconDotNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EWindowControl
{
    /// <summary>
    /// ROI list
    /// </summary>
    public class ERoiList
    {
        private List<ROIParm> lists;
        /// <summary>
        /// ROI number
        /// </summary>
        private int ROI_ID = 1;
        /// <summary>
        /// MAX ROI number [max: 1000000]
        /// </summary>
        private int MAX_ROI_ID = 1000000;
        private Object _root;
        /// <summary>
        /// ROI list
        /// </summary>
        public ERoiList()
        {
            lists = new List<ROIParm>();
            _root = new object();
        }
        /// <summary>
        /// ROI parameters at the specified position
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ROIParm this[int index]
        {
            get
            {
                lock (_root)
                {
                    return lists[index];
                }
            }
            set
            {
                lock (_root)
                {
                    lists[index] = value;
                }
            }
        }
        /// <summary>
        /// list count
        /// </summary>
        public int Count
        {
            get
            {
                return lists.Count;
            }
        }
        /// <summary>
        /// get ID number
        /// </summary>
        public int GetID
        {
            get
            {
                CheckRoiIndex();
                //ROI_ID++;
                return ROI_ID;
            }
        }
        /// <summary>
        /// delete ROI from list by list index
        /// </summary>
        /// <param name="index">list index</param>
        public void RemoveList_ByIdx(int index)
        {
            lists.RemoveAt(index);
        }

        /// <summary>
        /// delete ROI from list by ROI number
        /// </summary>
        /// <param name="ID">ROI ID</param>
        public void RemoveList_ByID(int ID)
        {
            ROIParm Find = lists.FirstOrDefault(x => x.ID == ID);
            if (Find != null)
                lists.Remove(Find);
        }
        /// <summary>
        /// delete ROI from list by ROI
        /// </summary>
        /// <param name="parm">ROI parameters</param>
        public void Remove_ByROI(ROIParm parm)
        {
            lists.Remove(parm);
        }
        /// <summary>
        /// add ROI to list
        /// </summary>
        /// <param name="parm">parameters</param>
        /// <param name="NeedCheckID">whether to check the ID; if needed, the ID is assigned automatically; otherwise it is added directly to the list, which may risk duplicate IDs</param>
        public void AddROI(ROIParm parm,bool NeedCheckID=true)
        {
            GenerateOutterRectangle(parm);
            if (NeedCheckID)
            {
                CheckRoiIndex();
                parm.ID = ROI_ID;
            }
            lists.Add(parm);
            ROI_ID++;
        }
        /// <summary>
        /// generate maximum outer bounding rectangle
        /// </summary>
        /// <param name="parm"></param>
        private void GenerateOutterRectangle(ROIParm parm)
        {
            return;
            RectangleF rectangleF = new RectangleF();
            HTuple hv_Value= new HTuple();
            HOperatorSet.RegionFeatures(parm.Region, (((new HTuple("row1")).TupleConcat("column1")).TupleConcat("row2")).TupleConcat("column2"), out hv_Value);

            rectangleF.Y = (float)hv_Value.DArr[0];
            rectangleF.X = (float)hv_Value.DArr[1];
            rectangleF.Width = (float)hv_Value.DArr[3]-(float)hv_Value.DArr[1];
            rectangleF.Height = (float)hv_Value.DArr[2]-(float)hv_Value.DArr[0];

            parm.OutterRect=rectangleF;
        }
        /// <summary>
        /// add ROI to list
        /// </summary>
        /// <param name="parms">parameters</param>
        public void AddBatchROI(ROIParm[] parms)
        {
            for (int i = 0; i < parms.Length; i++)
            {
                ROIParm parm = parms[i];
                GenerateOutterRectangle(parm);
                CheckRoiIndex();
                parm.ID = ROI_ID;
                lists.Add(parm);
                ROI_ID++;
            }
        }

        /// <summary>
        /// update ROI
        /// </summary>
        /// <param name="parm">parameters</param>
        public void UpdateROI(ROIParm parm)
        {
            ROIParm Find = lists.FirstOrDefault(x => x.ID == parm.ID);
            if (Find != null)
            {
                GenerateOutterRectangle(parm);
                //Find = parm;
                Find.VisibleROIText = parm.VisibleROIText;
                Find.VisableROI = parm.VisableROI;
                Find.Region = parm.Region;
                Find._type = parm._type;
                Find.OutterRect=parm.OutterRect;
                //parm.regionSize = lists[i].regionSize;
                Find.ID = parm.ID;
                Find._color = parm._color;
                Find.Lock= parm.Lock;
            }
                //Find = parm;
        }


        /// <summary>
        /// update ROI
        /// </summary>
        /// <param name="parmList">parameter list</param>
        public void UpdateROI(ERoiList parmList)
        {
            Dictionary<int, int> table = new Dictionary<int, int>();
            for (int i = 0; i < lists.Count; i++)
            {
                table.Add(lists[i].ID,i );
            }
            ROIParm Find = null;
            for (int i = 0; i < parmList.Count; i++)
            {
                ROIParm parm = parmList[i];
                if (table.ContainsKey(parm.ID))
                {
                    Find = lists[table[parm.ID]];

                    //Find = parm;
                    Find.VisibleROIText = parm.VisibleROIText;
                    Find.VisableROI = parm.VisableROI;
                    Find.Region = parm.Region;
                    Find._type = parm._type;
                    Find.OutterRect = parm.OutterRect;
                    //parm.regionSize = lists[i].regionSize;
                    Find.ID = parm.ID;
                    Find._color = parm._color;
                    Find.Lock = parm.Lock;
                }
            }
        }
        /// <summary>
        /// deep copy
        /// </summary>
        /// <returns></returns>
        public ERoiList Clone()
        {
            ERoiList CloneERoiList = new ERoiList();
            CloneERoiList.lists=new List<ROIParm>();

            for (int i = 0; i < lists.Count; i++)
            {
                ROIParm parm =new ROIParm();
                parm.VisibleROIText = lists[i].VisibleROIText;
                parm.VisableROI = lists[i].VisableROI;
                parm.Region = lists[i].Region;
                parm._type = lists[i]._type;
                parm.OutterRect = lists[i].OutterRect;
                parm.ID = lists[i].ID;
                parm._color = lists[i]._color;
                parm.Lock= lists[i].Lock;
                CloneERoiList.lists.Add(parm);
            }
            return CloneERoiList;
        }
     
        /// <summary>
        /// clear list
        /// </summary>
        /// <param name="parm">ROI parameters</param>
        public void ClearROI(ROIParm parm)
        {
            lists.Clear();
            ROI_ID = 1;          
        }


        /// <summary>
        /// find whether it is in the list by parameters
        /// </summary>
        /// <param name="parm">ROI parameters</param>
        /// <returns></returns>
        public ROIParm Find_FirstOrDefault(ROIParm parm)
        {
            return lists.FirstOrDefault(x => x == parm);
        }
        /// <summary>
        /// find list entry by ROI number
        /// </summary>
        /// <param name="ID">ROI ID</param>
        /// <returns></returns>
        public ROIParm Find_FirstOrDefaultById(int ID)
        {
            return lists.FirstOrDefault(x => x.ID == ID);
        }
        /// <summary>
        /// find list entry by ROI number
        /// </summary>
        /// <param name="ID">ROI ID</param>
        /// <returns></returns>
        public int Find_IndxById(int ID)
        {
            return lists.FindIndex(x => x.ID == ID);
        }


        /// <summary>
        /// confirm ROI_ID ordering
        /// </summary>
        private void CheckRoiIndex()
        {
            if (lists == null) return;

            if (lists.Count > 0)
            {
                //int MinId=int.MaxValue;
                bool[] ID_Ary=new bool[MAX_ROI_ID];
                for (int i = 0; i < lists.Count; i++)
                {
                    ID_Ary[lists[i].ID] = true;
                }

                for (int i = 1; i < ID_Ary.Length; i++)
                {
                    if (ID_Ary[i] == false)
                    {
                        ROI_ID = i;
                        break;
                    }
                }

                //for (int i = 1; i < MAX_ROI_ID; i++)
                //{
                //    ROIParm FindParm = lists.FirstOrDefault(x => x.ID == i);
                //    if (FindParm == null)
                //    {
                //        ROI_ID = i;
                //        break;
                //    }
                //}
            }
            else
            {
                ROI_ID = 1;
            }
        }

    }
}

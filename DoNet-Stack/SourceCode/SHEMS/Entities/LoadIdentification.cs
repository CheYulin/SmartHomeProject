﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SHEMS.Entities
{
    public enum EVENT_TYPE
    {
        ON, OFF
    }

    public class LoadIDResult
    {
        private string name;

        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        private string imgpath;

        public string Imgpath
        {
            get { return imgpath; }
            set { imgpath = value; }
        }
        private DateTime datetime;

        public DateTime Datetime
        {
            get { return datetime; }
            set { datetime = value; }
        }
        private EVENT_TYPE event_type;

        public EVENT_TYPE Event_type
        {
            get { return event_type; }
            set { event_type = value; }
        }
        public LoadIDResult()
        {

        }
    }
   
    public class LoadData
    {
        float ap;

        public float Ap
        {
            get { return ap; }
            set { ap = value; }
        }
        float rp;

        public float Rp
        {
            get { return rp; }
            set { rp = value; }
        }

        DateTime time;

        public DateTime Time
        {
            get { return time; }
            set { time = value; }
        }

        public LoadData(float ap, float rp, DateTime time)
        {

            this.ap = ap;
            this.rp = rp;

            this.time = time;
        }



    }
    
    public class LoadIdentification
    {
        static float THRESHOLD = 0.1f;
        static int WINDOW_NUM = 5;			//滑动平均滞后的时间      为WINDOW_NUM
        static float AP_THRESHOLD = 10;
        static float BASE_POWER_THRESHOLD = 5;
 
    
        public static bool isLampOn = false;
        public static bool isNoteBookOn = false;
        public static bool isPotOn = false;
        public static string NAME_POT = "电热水壶";
        public static string NAME_LAMP = "节能灯";
        public static string NAME_NOTEBOOK = "笔记本电脑";
        

        public static string mapName2ImgPath(string name)
        {
            string imgPath = "ms-appx:///Assets/";
            if(name.Equals(NAME_LAMP))
            {
                imgPath += "lamp.png";
            }
            else if(name.Equals(NAME_POT))
            {
                imgPath += "";
            }
            else if(name.Equals(NAME_NOTEBOOK))
            {
                imgPath += "";
            }
            else
            {
                imgPath += null;
            }
            return imgPath;
        }
      
       
        private class WindowOperator // 用来方便做平滑处理的类
        {
            /**
             * @author cyl 得到平滑处理后的数据点
             *
             */
            public Queue<float> apque;
            public Queue<float> rpque;
            float ap_average;
            float rp_average;
            Queue<DateTime> dateque;

            public WindowOperator()
            {
                apque = new Queue<float>();
                rpque = new Queue<float>();
                dateque = new Queue<DateTime>();
                ap_average = 0;
                rp_average = 0;
            }

            public WindowOperator(WindowOperator windowOperator)
            {
                this.ap_average = windowOperator.ap_average;
                this.rp_average = windowOperator.rp_average;
                this.apque = windowOperator.apque;
                this.rpque = windowOperator.rpque;
                this.dateque = windowOperator.dateque;

            }

            public LoadData addData(float ap, float rp, DateTime time)
            {
                if (apque.Count < WINDOW_NUM)
                {
                    apque.Enqueue(ap);
                    rpque.Enqueue(rp);
                    dateque.Enqueue(time);
                    int nowsize = apque.Count;
                    this.ap_average = (this.ap_average * (nowsize - 1) + ap) / nowsize;
                    this.rp_average = (this.rp_average * (nowsize - 1) + rp) / nowsize;
                    ////Log.i("apque_size",""+apque.Count);
                    if (apque.Count == WINDOW_NUM)
                        return new LoadData(this.ap_average, this.rp_average, time);
                    else
                        return null;			//表示开始的处理还不够对应的平滑点数
                }
                if (apque.Count == WINDOW_NUM)
                {
                    this.ap_average = (this.ap_average * WINDOW_NUM - (float)apque.Dequeue() + ap) / WINDOW_NUM;
                    this.rp_average = (this.rp_average * WINDOW_NUM - (float)rpque.Dequeue() + rp) / WINDOW_NUM;
                    apque.Enqueue(ap);
                    rpque.Enqueue(rp);
                    dateque.Enqueue( time);
                    //assert(apque.Count<WINDOW_NUM+1);		//断言
                    return new LoadData(this.ap_average, this.rp_average, time);
                }
                else
                    return null;			//出问题的时候
            }
        }


        private static bool whetherPosibleEvent(LoadData former, LoadData latter, WindowOperator window_operator)
        {
            if (window_operator.apque.Count == WINDOW_NUM)				//窗口正确形成的时候
            {
                float ap_change = latter.Ap - former.Ap;
                float rp_change = latter.Rp - former.Rp;
                float ap_rate_change_Abs = 0;
                float rp_rate_change_Abs = 0;
                if (Math.Abs(former.Ap) > BASE_POWER_THRESHOLD)				//如果比较的             基础的值比较大才选择使用
                {
                    ap_rate_change_Abs = ap_change / former.Ap;
                }
                if (Math.Abs(former.Rp) > BASE_POWER_THRESHOLD)
                {
                    rp_rate_change_Abs = rp_change / former.Rp;
                }
                if (Math.Abs(ap_rate_change_Abs) > THRESHOLD || Math.Abs(rp_rate_change_Abs) > THRESHOLD)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;						//窗口不形成  不能给权限进行分析
            }
        }

        private static bool ConfirmEvent(LoadData former, LoadData latter, WindowOperator window_operator, float threshold)
        {
            if (window_operator.apque.Count == WINDOW_NUM)				//窗口正确形成的时候
            {
                float ap_change = latter.Ap - former.Ap;
                float rp_change = latter.Rp - former.Rp;
                float ap_rate_change_Abs = 0;
                float rp_rate_change_Abs = 0;
                if (Math.Abs(former.Ap) > BASE_POWER_THRESHOLD)				//如果比较的             基础的值比较大才选择使用
                {
                    ap_rate_change_Abs = ap_change / former.Ap;
                }
                if (Math.Abs(former.Rp) > BASE_POWER_THRESHOLD)
                {
                    rp_rate_change_Abs = rp_change / former.Rp;
                }
                if (Math.Abs(ap_rate_change_Abs) > threshold || Math.Abs(rp_rate_change_Abs) > threshold)
                {
                    //Log.i("change", ap_change + "   ;" + rp_change);
                    //Log.i("changeRate", ap_rate_change_Abs + "  ;" + rp_rate_change_Abs);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;						//窗口不形成  不能给权限进行分析
            }
        }

        private static String matchload(double loadinap, double loadinrp,bool isOnNow)
        {
           
            if (loadinap > 1800 && loadinap < 1900)
            {

                isPotOn=isOnNow;    
                return NAME_POT;
            }

            else if (loadinap > 5 && loadinap < 20)
            {
                isLampOn = isOnNow;
                return NAME_LAMP;
            }
            else if (loadinap > 20 && loadinap < 50)
            {
                double absrp = Math.Abs(loadinrp);
                if (absrp > 11 && absrp < 20)
                {
                    isNoteBookOn = isOnNow;
                    return NAME_NOTEBOOK;
                }
                else
                    return "Unknow";
            }
            else if (loadinap < 5)
            {
                return "Unknow";
            }
            else
                return "Unknow";
        }

        public static List<LoadIDResult> handleHisLogDBMethodWindow(List<LoadData> loadDatalist)
        {
            List<LoadIDResult> loadeventslist = new List<LoadIDResult>();
            WindowOperator window_operator = new WindowOperator();
            LoadData former = null;
            LoadData latter = null;
            Queue<LoadData> loadDataQue;
            loadDataQue = new Queue<LoadData>();			//主要用来存储出现可疑点的

            int index = -1;
            while (loadDatalist.Count > (index++)+1)
            {
                DateTime time = loadDatalist[index].Time;
                float ap = loadDatalist[index].Ap;
                float rp = loadDatalist[index].Rp;

                former = latter;					//原来的相对在后面的的点     到了在前面的点的位置
                latter = window_operator.addData(ap, rp, time);

                //后面是找是否有可疑点            然后再进行查找
                if (former != null)
                {
                    if (whetherPosibleEvent(former, latter, window_operator))		//可疑点
                    {
                        //参考
                        LoadData reference = former;			//作为一个参考后面进行参考
                        LoadData possibleInOrOut = latter;			//可疑点
                        LoadData thelast = latter;				//就是可疑点      和之后要检测的点
                        WindowOperator tempOperator = new WindowOperator(window_operator);		//用来存储突变的点
                        int cursormovetime = 0;						//记录游标到底移动了多少				
                        for (int move = 0; move < WINDOW_NUM - 1; move++)		//之后往后走WINDOW_NUM-1   个 点看看  进行一个确认的过程  因为我们的变化基本是一个突变的过程
                        {
                            //第一步，先拿数据：           记得游标在正常结束的时候也要进行回退
                            if (loadDatalist.Count > index+1)
                            {
                                cursormovetime++;
                                index++;            //拿下一个点
                                //Log.i("cursor movetime", "move" + move + " ;cursortime" + cursormovetime);
                                time = loadDatalist[index].Time;
                                ap = loadDatalist[index].Ap;
                                rp = loadDatalist[index].Rp;
                                thelast = tempOperator.addData(ap, rp, time);		//做平滑处理
                            }
                            else
                            {
                                for (int j = 0; j < cursormovetime; j++)
                                {
                                    index -= cursormovetime;        
                                    cursormovetime = 0;
                                }
                            }

                            //第二步比较判断
                            if (ConfirmEvent(reference, thelast, tempOperator, THRESHOLD + cursormovetime * 0.1f) == false)
                            {
                                for (int j = 0; j < cursormovetime; j++)
                                {
                                    index -= cursormovetime;			//游标回退             
                                }
                                cursormovetime = 0;
                                break;												//否决的话就不做下去了
                            }
                            else
                            {
                                //第三步：如果做到了最后一个判断
                                if (move == WINDOW_NUM - 2)					//到了最后的把关点的判断正确就要进行显示或者存数据库的相关操作
                                {
                                    String tempstr = null;
                                    bool trueForIn = thelast.Ap > reference.Ap;
                                    String event_status = null;
                                    String ApplianceNameStr = null;

                                    //							db_loadinfo=new LoadInfoDB(this);
                                    float loadap = Math.Abs(thelast.Ap - reference.Ap);
                                    float loadrp = -Math.Abs(thelast.Rp - reference.Rp);
                                    //先做一个匹配的检验    要从loginfo中匹配到才算ok
                                   EVENT_TYPE eventType;
                                    if (trueForIn)
                                    {
                                        tempstr = "Load detected：in\n";
                                        eventType = EVENT_TYPE.ON;
                                    }
                                    else
                                    {
                                        tempstr = "Load detected: out\n ";
                                        eventType = EVENT_TYPE.OFF;
                                    }
                                   
                                    {
                                        ApplianceNameStr = matchload(loadap, loadrp, trueForIn);
                                        LoadIDResult loadIDResult=new LoadIDResult{
                                            Name=ApplianceNameStr,
                                            Imgpath=mapName2ImgPath(ApplianceNameStr),
                                            Datetime=reference.Time,
                                            Event_type=eventType
                                        };
                                        if (!loadIDResult.Name.Equals("Unknow"))
                                            loadeventslist.Add(loadIDResult);
                                    }
      					

                                }

                            }
                        }

                    }

                }
            }
 
            return loadeventslist;
        }

    }
}

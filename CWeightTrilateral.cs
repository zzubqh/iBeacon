using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using DatabaseProvider;
using log4net;
using MathNet.Numerics.LinearAlgebra.Double;


[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace Algorithm
{
    /// <summary>
    /// 三角形定位算法实现类
    /// </summary>
    public class CWeightTrilateral
    {
        public struct APInfo
        {
            public string uuid;
            public CPoint location;
            public int rssi;
            public int A; //A为距离探测设备1m时的rssi值的绝对值，最佳范围在45-49之间
            public double n; //n为环境衰减因子，需要测试矫正，最佳范围在3.25-4.5之间
            public double height;
        }
               
        static double totalWeight = 0;

        /// <summary>
        /// 根据AP收到的rssi值计算终端UE的位置
        /// </summary>
        /// <param name="APList">AP的信息，格式：AP_id:rssi </param>
        /// <returns>返回终端的坐标</returns>
        public static CPoint GetLocation(List<APInfo> APList)
        {            
            if(APList == null || APList.Count < 3)
            {
                throw new Exception("the number of AP is less then 3, cloud not located the mobel unit, please check AP!");
            }

            //取AP数据的前5个进行分组计算，如果参与计算的AP数量太多，会造成累计误差增大
            List<APInfo> APSort = (from ap in APList orderby ap.rssi descending select ap).Take(5).ToList();

            //将AP分组
            List<object> temp = APSort.ConvertAll(s => (object)s);
            CCombineAlgorithm ca = new CCombineAlgorithm(temp.ToArray(), 3);
            object[][] combineAPArray = ca.getResult();
            CPoint deviceLocation = new CPoint();
            for (int i = 0; i < combineAPArray.GetLength(0); i++)
            {                
                List<APInfo> apList = new List<APInfo>();
                foreach(object obj in combineAPArray[i])
                {
                    apList.Add((APInfo)obj);
                }
                //得到加权后的坐标
                deviceLocation += CaculateByAPList(apList);
            }

            return new CPoint(deviceLocation.X / totalWeight, deviceLocation.Y / totalWeight);
        }

        /// <summary>
        /// 根据三角形定位算法计算UE位置
        /// </summary>
        /// <param name="ap_list"></param>
        /// <returns>返回UE的定位坐标(x,y)</returns>
        private static CPoint CaculateByAPList(List<APInfo> apArray)
        {
            double[,] a_array = new double[2, 2];
            double[] b_array = new double[2];

            //距离数组
            double[] distanceArray = new double[3];
            for(int i = 0; i< 3; i++)
            {
                distanceArray[i] = GetDisFromRSSI(apArray[i]);
            }

            //系数矩阵A初始化
            for (int i = 0; i< 2; i++)
            {
                a_array[i, 0] = 2 * (apArray[i].location.X - apArray[2].location.X);
                a_array[i, 1] = 2 * (apArray[i].location.Y - apArray[2].location.Y);
            }
 
            //矩阵b初始化
            for (int i = 0; i< 2; i++)
            {
                b_array[i] = Math.Pow(apArray[i].location.X, 2)
                            - Math.Pow(apArray[2].location.X, 2)
                            + Math.Pow(apArray[i].location.Y, 2)
                            - Math.Pow(apArray[2].location.Y, 2)
                            + Math.Pow(distanceArray[2], 2)
                            - Math.Pow(distanceArray[i], 2);
            }
            var matrixA = DenseMatrix.OfArray(a_array);
            var vectorB = new DenseVector(b_array);
            //计算 X=(A^T * A)^-1 * A^T * b
            var a1 = matrixA.Transpose(); // A的转置
            var a2 = a1 * matrixA;
            var resultX = a2.Inverse() * a1 * vectorB;
            double[] res = resultX.ToArray();                      

            /*对应的权值*/
            double weight = 0;
            for (int i = 0; i < 3; i++)
            {
                weight += (1.0 / distanceArray[i]);
            }
            totalWeight += weight;

            return new CPoint(res[0] * weight, res[1] * weight);
        }

        /// <summary>
        /// 根据AP的ID在数据库中查找配置信息
        /// </summary>
        /// <param name="ap_id"></param>
        /// <returns>返回AP的配置信息</returns>
        private static APInfo GetAPInfo(string ap_uuid)
        {
            ILog log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            CAPInfoProvider apPro = new CAPInfoProvider();
            AP_Info_table entity = apPro.GetEntityByUUID(ap_uuid);
            APInfo apInfo = new APInfo();
            try
            {
                apInfo.location = new CPoint(entity.x.Value, entity.y.Value);
                apInfo.A = entity.A.Value;
                apInfo.n = entity.n.Value;
                apInfo.height = entity.height.Value;                    
            }
            catch(Exception ex)
            {
                log.Error(string.Format("get AP entity  from {0} error! please check AP UUID!", ap_uuid));
            }            
            return apInfo;
        }

        /// <summary>
        /// 利用RSSI得到设备距AP的水平距离
        /// </summary>
        /// <param name="ap_info">参与运算的AP实例</param>        
        /// <returns>单位：m</returns>
        private static double GetDisFromRSSI(APInfo ap_info)
        {
            double rawDis = 0.0;           
            double power = (ap_info.A - ap_info.rssi) / (10 * ap_info.n);
            rawDis = Math.Pow(10, power);
            //返回AP到UE的水平距离
            return Math.Sqrt(Math.Pow(rawDis, 2) - Math.Pow(ap_info.height, 2));           
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algorithm
{
    /// <summary>
    /// 封装圆类
    /// </summary>
    public class CRound
    {
        private CPoint center; //圆心坐标
        private double r; //半径

        public CRound()
        {
            this.center = new CPoint();
            this.r = 0.0;
        }

        public CRound(CPoint center, double r1)
        {
            this.center = center;
            this.r = r1;
        }

        public CPoint Center
        {
            get { return center; }
            set { center = value; }
        }
       
        public double R
        {
            get { return r; }
            set { r = value; }
        }
    }
}

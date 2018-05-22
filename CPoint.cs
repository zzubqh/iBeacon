using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algorithm
{
    public class CPoint
    {
        private double x, y; //坐标(x,y)

        public CPoint()
        {
            this.x = 0.0;
            this.y = 0.0;
        }

        public CPoint(double x, double y)
        {
            this.x = x;
            this.y = y;
        }
        public double X
        {
            get { return x; }
            set { x = value; }
        }

        public double Y
        {
            get { return y; }
            set { y = value; }
        }

        public static CPoint operator +(CPoint lhs, CPoint rhs)
        {
            CPoint point = new CPoint();
            point.X = lhs.X + rhs.X;
            point.Y = lhs.Y + rhs.Y;
            return point;
        }
    }
}

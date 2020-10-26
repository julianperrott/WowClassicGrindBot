using Common;
using System;

namespace PatherPath.Graph
{
    public class Triangle
    {
        private ILocation _A;
        private ILocation _B;
        private ILocation _C;
        private double _a;
        private double _b;
        private double _c;

        public Triangle(ILocation A, ILocation B, ILocation C)
        {
            this._A = A;
            this._B = B;
            this._C = C;
            _a = Length(B, C);
            _b = Length(A, C);
            _c = Length(A, B);
        }

        public static double Length(ILocation a, ILocation b)
        {
            return Math.Sqrt((Math.Pow((double)(a.X - b.X), 2.0) + Math.Pow((double)(a.Y - b.Y), 2.0)));
        }

        public Angle a_Angle
        {
            get
            {
                return new Angle(Math.Acos
                (
                    (Math.Pow(_b, 2.0) + Math.Pow(_c, 2.0) - Math.Pow(_a, 2.0))
                    / (2 * _b * _c)
                ));
            }
        }

        public Angle b_Angle
        {
            get
            {
                return new Angle(Math.Acos
                (
                    (Math.Pow(_a, 2.0) + Math.Pow(_c, 2.0) - Math.Pow(_b, 2.0))
                    / (2 * _a * _c)
                ));
            }
        }

        public Angle c_Angle
        {
            get
            {
                return new Angle(Math.Acos
                (
                    (Math.Pow(_a, 2.0) + Math.Pow(_b, 2.0) - Math.Pow(_c, 2.0))
                    / (2 * _a * _b)
                ));
            }
        }
    }

    public class Angle
    {
        public double radians;
        public double degrees;

        public Angle(double radians)
        {
            this.radians = radians;
            this.degrees = RadianToDegrees(radians);
        }

        public static double RadianToDegrees(double radians)
        {
            return (radians * 180) / Math.PI;
        }
    }
}
// ================================================
// Harvey Triana / Visionary-SAS
// Cubic equation solver by Cardano´s method
// ================================================

using System;
using System.Numerics;

namespace MoreDegrees
{
    public static class CubicSolver
    {
        private const double Closetozero = 1.0E-8, THIRD = 1.0 / 3.0;

        public static Complex[] Solve(double a, double b, double c, double d)
        {
            if (a == 0)
            {
                return Array.Empty<Complex>();
            }

            // constants
            double j = b / a;
            double k = c / a;
            double l = d / a;
            double p = -(j * j / 3.0) + k;
            double q = (2.0 / 27.0 * j * j * j) - (j * k / 3.0) + l;
            double t = q * q / 4.0 + p * p * p / 27.0;

            // force to zero if it is very close to zero
            if (Math.Abs(t) < Closetozero)
            {
                t = 0;
            }

            if (Math.Abs(q) < Closetozero)
            {
                q = 0;
            }

            double r1 = 0, r2 = 0, r3 = 0, i1 = 0, i2 = 0, i3 = 0, i;

            // There are three cases according to the value of t
            if (t > 0)
            {
                // one real, two complexs
                // real root
                r1 = CubeRoot(-q / 2.0 + Math.Sqrt(t)) + CubeRoot(-q / 2.0 - Math.Sqrt(t));
                // two complex roots
                r2 = -r1 / 2.0;
                r3 = r2; // conjugated
                // imaginary
                if (q == 0)
                {
                    i = Math.Sqrt(k);
                }
                else
                {
                    i = Math.Sqrt(Math.Abs(Math.Pow(r1 / 2.0, 2.0) + q / r1));
                }

                i1 = 0;
                i2 = i;
                i3 = -i;
            }

            if (t == 0)
            {
                // three real roots, at least two equal
                r1 = 2.0 * CubeRoot(-q / 2.0);
                r2 = -r1 / 2.0 + Math.Sqrt(Math.Pow(r1 / 2.0, 2.0) + q / r1);
                r3 = -r1 / 2.0 - Math.Sqrt(Math.Pow(r1 / 2.0, 2.0) + q / r1);
            }

            if (t < 0)
            {
                // three real roots
                var x = -q / 2.0;
                var y = Math.Sqrt(-t); // make t positive
                var angle = Math.Atan(y / x);
                if (q > 0)
                {
                    // if q > 0 the angle becomes 2 * PI - angle
                    angle = Math.PI - angle;
                }

                r1 = 2.0 * Math.Sqrt(-p / 3.0) * Math.Cos(angle / 3.0);
                r2 = 2.0 * Math.Sqrt(-p / 3.0) * Math.Cos((angle + 2.0 * Math.PI) / 3.0);
                r3 = 2.0 * Math.Sqrt(-p / 3.0) * Math.Cos((angle + 4.0 * Math.PI) / 3.0);
            }

            // output
            var result = new Complex[3];
            result[0] = new Complex(r1 - j / 3.0, i1);
            result[1] = new Complex(r2 - j / 3.0, i2);
            result[2] = new Complex(r3 - j / 3.0, i3);
            return result;
        }

        private static double CubeRoot(double number) => number < 0 ? -Math.Pow(-number, THIRD) : Math.Pow(number, THIRD);
    }
}

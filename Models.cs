using System;
using System.Collections.Generic;
using System.Linq;

namespace MicroGrad
{
    public static class Global
    {
        public static int ValueCount { get; set; } = 1;
    }

    public class Value
    {
        public Value(double data)
        {
            Data = data;
        }

        public double Data { get; set; }
        public int Index { get; set; } = Global.ValueCount++;
        public List<Value> Children { get; set; } = new List<Value>();
        public List<Value> Prev { get => Children.Distinct().ToList(); }
        public char Op { get; set; }    

        public static Value operator -(Value a) => new Value(-a.Data);
        public static Value operator +(Value a, Value b) => new Value(a.Data + b.Data) { Children = new[] { a, b }.ToList(), Op = '+' };
        public static Value operator *(Value a, Value b) => new Value(a.Data * b.Data) { Children = new[] { a, b }.ToList(), Op = '*' };

    }


    // Operator Overloading Example
    public readonly struct Fraction
    {
        private readonly int num;
        private readonly int den;

        public Fraction(int numerator, int denominator)
        {
            if (denominator == 0)
            {
                throw new ArgumentException("Denominator cannot be zero.", nameof(denominator));
            }
            num = numerator;
            den = denominator;
        }

        public static Fraction operator +(Fraction a) => a;
        public static Fraction operator -(Fraction a) => new Fraction(-a.num, a.den);

        public static Fraction operator +(Fraction a, Fraction b)
            => new Fraction(a.num * b.den + b.num * a.den, a.den * b.den);

        public static Fraction operator -(Fraction a, Fraction b)
            => a + (-b);

        public static Fraction operator *(Fraction a, Fraction b)
            => new Fraction(a.num * b.num, a.den * b.den);

        public static Fraction operator /(Fraction a, Fraction b)
        {
            if (b.num == 0)
            {
                throw new DivideByZeroException();
            }
            return new Fraction(a.num * b.den, a.den * b.num);
        }

        public override string ToString() => $"{num} / {den}";
    }
}

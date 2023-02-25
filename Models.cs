using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MicroGrad
{
    public static class Global
    {
        public static int ValueCount { get; set; } = 1;
    }

    public class Module
    {
        // Weights and biases
        public List<object> Parameters { get; set; }
    }

    public class Neuron : Module
    {
        public Neuron(int nin, bool nonlin = true)
        {
            Initialize(nin);
            Nonlin = nonlin;
        }

        public void Initialize(int nin)
        {
            var rand = new Random(1234);
            W = Enumerable.Range(1, nin).Select(i => rand.NextDouble() * 2 - 1).ToArray();
            B = rand.NextDouble() * 2 - 1;
        }

        public double[] W { get; set; }
        public double B { get; set; }
        public bool Nonlin { get; set; }
    }

    public class Layer : Module
    {
        public List<Neuron> Neurons { get; set; }

        public Layer(int nin, int nout)
        {
            for (int i = 0; i < nout; i++)
            {
                //Neurons.Add(new Neuron())
            }
        }
    }
    public class MLP : Module
    {
        public MLP(int nin, int nout)
        {
            for (int i = 0; i < nout; i++)
            {
                //Layers.Add(new Layer())
            }
        }

        public List<Layer> Layers { get; set; } = new List<Layer>();
    }

    public enum Op
    {
        Add = 1,
        Multiply = 2,
        Subtract = 3,
        Divide = 4,

    }

    [DebuggerDisplay("{Index, nq}: {Data, nq}")]
    public class Value
    {
        public Value(double data)
        {
            Data = data;
        }

        public double Data { get; set; }
        //public delegate double Forward()

        public Value Forward()
        {
            switch (Op)
            {
                case Op.Add:
                    return Children[0] + Children[1]; 
                case Op.Multiply:
                    return Children[0] * Children[1];
            }
            return null;
        }
        public double Grad { get; set; }
        public int ID { get; set; } = Global.ValueCount++;
        public List<Value> Children { get; set; } = new List<Value>();
        //public List<Value> Prev { get => Children.Distinct().ToList(); }
        public Op Op { get; set; }    


        public static Value operator +(Value a, Value b)
        {
            return new Value(a.Data + b.Data)
            {
                Children = new[] { a, b }.ToList(),
                Op = Op.Add,
            };
        }
        public static Value operator *(Value a, Value b)
        {
            return new Value(a.Data * b.Data)
            {
                Children = new[] { a, b }.ToList(),
                Op = Op.Multiply,
            };
        }

        public void Backward()
        {

        }
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

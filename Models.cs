using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MicroGrad
{
    public static class Global
    {
        public static int ValueId { get; set; } = 1;
        public static Random Rand { get; set; } = new Random(1234);
    }

    public interface Module
    {
        // Weights and biases
        public IEnumerable<Value> Parameters { get; }
    }

    public class Neuron : Module
    {
        public Neuron(int nin, string nonlin = null)
        {
            Initialize(nin);
            switch (nonlin)
            {
                case "relu":
                    break;
            }
        }

        public void Initialize(int nin)
        {
            W = Enumerable.Range(1, nin).Select(i => new Value(Global.Rand.NextDouble() * 2 - 1)).ToArray();
            B = new Value(Global.Rand.NextDouble() * 2 - 1);
        }

        public double Forward(double[] X)
        {
            double sum = 0;
            foreach(var tup in X.Zip(W))
            {
                sum += tup.First * tup.Second.Data;
            }
            return Nonlin(sum.Value()).Data;
        }

        public IEnumerable<Value> Parameters { get => W.Append(B); }
        public Value[] W { get; set; }
        public Value B { get; set; }
        //public delegate Value Calculate(double[] X)
        //public Calculate Forward { get; set; }
        //return X.Zip(W).Select(tup => tup.First * tup.Second + B).Sum();
        //public bool Nonlin { get; set; }
        public delegate Value Nonlinearity(Value input);
        public Nonlinearity Nonlin {get;set;} = (Value input) => input;

    }


    public class Layer : Module
    {
        public List<Neuron> Neurons { get; set; } = new List<Neuron>();
        public IEnumerable<Value> Parameters { get => Neurons.SelectMany(n=>n.Parameters); }

        public Layer(int nin, int nout)
        {
            for (int i = 0; i < nout; i++)
            {

                Neurons.Add(new Neuron(nin)
                {

                });
            }
            
        }
        public double Forward(double[] X)
        {
            double ret = 0;
            foreach (var neuron in Neurons)
                ret = neuron.Forward(X);
            return ret;
        }

    }
    public class MLP : Module
    {
        public MLP(int nin, int[] nouts)
        {
            var sizes = nouts.Prepend(nin).ToArray();
            for (int i = 0; i < nouts.Length; i++)
            {
                Layers.Add(new Layer(sizes[i], sizes[i + 1]));
            }
                
        }
        public double Forward(double[] X)
        {
            double ret = 0;
            foreach(var layer in Layers)
                ret = layer.Forward(X);
            return ret;
        }

        public IEnumerable<Value> Parameters { get => Layers.SelectMany(n=>n.Parameters); }
        public List<Layer> Layers { get; set; } = new List<Layer>();
    }

    public enum Op
    {
        Add = 1,
        Multiply = 2,
        Subtract = 3,
        Divide = 4,

    }

    [DebuggerDisplay("{Id, nq}: {Data, nq}")]
    public class Value
    {
        public Value(double data)
        {
            Data = data;
        }

        public double Data { get; set; }
        //public delegate double Forward()
        //public delegate double Calculate(double[] X = null);
        //public Calculate Calc { get; set; } 

        /// <summary>
        /// Stores and returns the calculation result
        /// </summary>
        public double Forward()
        {
            switch (Op)
            {
                case Op.Add:
                    Data = Children[0].Data + Children[1].Data; break;
                case Op.Multiply:
                    Data = Children[0].Data * Children[1].Data; break;
            }
            return Data;
        }

        public bool IsLeaf { get => !Children.Any(); }
        public double Grad { get; set; }
        public int Id { get; set; } = Global.ValueId++;
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FluentMermaid;
using FluentMermaid.Enums;
using FluentMermaid.Flowchart;
using FluentMermaid.Flowchart.Enum;

namespace MicroGrad
{
    public static class Global
    {
        public static int ValueId { get; set; } = 1;
        public static string Context { get; set; }
        public static Random Rand { get; set; } = new Random(1234);
    }

    public interface Module
    {
        // Weights and biases
        public IEnumerable<Value> Parameters { get; }
    }

    public class Neuron : Module
    {
        public Neuron(IEnumerable<Value> X, string nonlin = null)
        {
            Initialize(X);
            switch (nonlin)
            {
                case "relu":
                    break;
            }
        }

        public void Initialize(IEnumerable<Value> X)
        {

            W = Enumerable.Range(1, X.Count()).Select(i => new Value()).ToArray();
            B = new Value();
            Values = X.Zip(W).Select(tup => tup.First * tup.Second).ToList();
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
        public List<Value> Values { get; set; } = new List<Value>();
        public Value[] W { get; set; }
        public Value[] X { get; set; }
        public Value B { get; set; }
        public Value Output {
            get
            {
                //Value[] weighted = ;
                //foreach (var tup in X.Zip(W))
                //{
                //    sum += tup.First * tup.Second;
                //}
                Global.Context = "Aggregate";
                return Values.Aggregate((x,y)=>x+y);
            }
        }
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

        public Layer(int nin, int nout, Layer lastLayer)
        {
            for (int i = 0; i < nout; i++)
            {
                var X = lastLayer?.Neurons.Select(n => n.Output)
                    ?? Enumerable.Range(1, 1).Select(i => new Value());

                Neurons.Add(new Neuron(X)
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
        public MLP(int[] layerSizes)
        {
            for (int i = 0; i < layerSizes.Length - 1; i++)
                Layers.Add(new Layer(layerSizes[i], layerSizes[i + 1], Layers.LastOrDefault()));
                
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
        public Value(double? data = null, string context = null)
        {
            context = context ?? Global.Context;
            Data = data ?? Global.Rand.NextDouble() * 2 - 1;
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
        public string Context { get; set; }
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


        public string Diagram
        {
            get
            {
                var nodes = new Dictionary<Value, INode>(); // new List<INode>();
                var chart = FlowChart.Create(Orientation.LeftToRight);

                Build(this);
                void Build(Value node)
                {
                    INode iNode;
                    if (!nodes.ContainsKey(node))
                    {
                        iNode = chart.TextNode($"{node.Id}: {node.Data.ToString()}", Shape.RoundEdges);
                        nodes.Add(node, iNode);
                    }
                    else
                        iNode = nodes[node];

                    if (node.Children.Any())
                    {

                        var opNode = chart.TextNode(node.Op.ToString(), Shape.Circle);
                        chart.Link(opNode, nodes[node], Link.Arrow, "");
                        foreach (var child in node.Children)
                        {
                            Build(child);
                            chart.Link(nodes[child], opNode, Link.Arrow, "");


                        }
                    }
                }


                string mermaidSyntax = chart.Render();// Regex.Unescape(chart.Render());
                return mermaidSyntax;

            }
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

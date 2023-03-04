using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using FluentMermaid;
using FluentMermaid.Enums;
using FluentMermaid.Flowchart;
using FluentMermaid.Flowchart.Enum;

namespace MicroGrad
{
    public static class Global
    {
        public static int ValueId { get; set; } = 1;
        public static int NeuronId { get; set; } = 1;
        public static string Context { get; set; }
        public static Random Rand { get; set; } = new Random(1234);


    }

    public class Context
    {
        public Layer Layer { get; set; }
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
            Id = Global.NeuronId++;
            Initialize(X);
            switch (nonlin)
            {
                case "relu":
                    break;
            }
        }

        public void Initialize(IEnumerable<Value> X)
        {
           
            W = Enumerable.Range(1, X.Count()).Select(i => new Value(null, "W", this)).ToArray();
            B = new Value(null, "B", this);
            //Global.Context = "Summation";
            Output = X.Zip(W).Select(tup => tup.First * tup.Second).Aggregate((x, y) => x + y);

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

        public int Id { get; set; }
        public IEnumerable<Value> Parameters { get => W.Append(B); }
        public List<Value> Values { get; set; } = new List<Value>();
        public Value[] W { get; set; }
        public Value[] X { get; set; }
        public Value B { get; set; }
        public Value Output { get; set; }
        public Layer Layer { get; set; }
        //    get
        //    {
        //        //Global.Context = "Agg";
        //        var ret = Values.Aggregate((x, y) => x + y);
        //        Global.Context = null;
        //        return ret;
        //    }
        //}
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
        public MLP Parent { get; set; }
        public int LayerIndex { get => Parent.Layers.IndexOf(this); }

        public Layer(int nin, int nout, Layer lastLayer)
        {
            for (int i = 0; i < nout; i++)
            {
                var X = lastLayer?.Neurons.Select(n => n.Output)
                    ?? Enumerable.Range(1, 1).Select(i => new Value(null,"Input"));
                ;
                Neurons.Add(new Neuron(X)
                {
                    Layer = this,
                }) ;
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
                Layers.Add(new Layer(layerSizes[i], layerSizes[i + 1], Layers.LastOrDefault()) { Parent = this, }) ;
                
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

    public class Op
    {
        public Op(OpChar opChar) => this.Char = opChar;
        public Op(char opChar) => this.Char = (OpChar)(opChar);

        public OpChar Char { get; set; }
        public string Display { get => Char.ToString(); }
        public string Color
        {
            get
            {
                switch (Char)
                {
                    case (OpChar.Add): return "DC143C";
                    case (OpChar.Multiply): return "6495ED";
                    default: return "A9A9A9";
                }
            }
        }
    }

    public enum OpChar
    {
        Multiply = 'x',
        Add = '+',
        Subtract = '-',
        Divide = '/',
        Power = '^',
    }

    [DebuggerDisplay("{Id, nq}: {Data, nq}")]
    public class Value
    {
        public Value(double? data = null, string comment = null, Neuron neuron = null)
        {
            Comment = comment;
            Context = Global.Context?.ToString();
            Data = data ?? Global.Rand.NextDouble() * 2 - 1;
            Neuron = neuron;
        }

        public Neuron Neuron { get; set; }
        public double Data { get; set; }
        public string DataDisplay { get => Data.ToString().Substring(0, 6); }

        /// <summary>
        /// Stores and returns the calculation result
        /// </summary>
        public double Forward()
        {
            //switch (Op)
            //{
            //    case "+":
            //        Data = Children[0].Data + Children[1].Data; break;
            //    case "x":
            //        Data = Children[0].Data * Children[1].Data; break;
            //}
            return Data;
        }

        public bool IsLeaf { get => !Children.Any(); }
        public double Grad { get; set; }
        public string Comment { get; set; }
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
                Op = new Op('+'),
            };
        }
        public static Value operator *(Value a, Value b)
        {
            return new Value(a.Data * b.Data)
            {
                Children = new[] { a, b }.ToList(),
                Op = new Op('x'),
            };
        }

        public void Backward()
        {

        }


        public string Diagram
        {
            get
            {
                var opCount = Global.ValueId;
                var fc = "flowchart LR\n";
                var classDefs = "";
                int neuronSubgraph = 0;
                fc += "classDef Node fill:#FFFACD\n";
                fc += "classDef Add fill:#DC143C\n";
                fc += "classDef Multiply fill:#6495ED\n";

                //var nodes = new Dictionary<Value, INode>(); // new List<INode>();
                var nodes = new List<int>(); // new List<INode>();

                Build(this);
                void Build(Value v)
                {
                    var opId = opCount++;

                    if (v.Neuron != null)
                        if (v.Neuron.Id != neuronSubgraph)
                        {
                            if (neuronSubgraph > 0)
                                fc += $"end\n";

                            neuronSubgraph = v.Neuron.Id;
                            fc += $"subgraph Neuron_{neuronSubgraph}\n";
                        }
                        else;
                    else;

                    // Add the node to graph
                    if (!nodes.Contains(v.Id))
                    {
                        fc += $"Node_{v.Id}[{v.Comment}: {v.DataDisplay}]\n";
                        classDefs += $"class Node_{v.Id} Node\n";
                    }
                    else
                        ; //iNode = nodes[node];

                    if (v.Children.Any())
                    {
                        // Add a node showing the operation that calculated the parent node's value
                        fc += $"Op_{opId}(({(char)v.Op.Char}))\n";

                        // Declare the op node's class
                        classDefs += $"class Op_{opId} {v.Op.Char}\n";

                        // Link the op node with the parent node
                        fc += $"Op_{opId} --> Node_{v.Id}\n";

                        foreach (var child in v.Children)
                        {
                            fc += $"Node_{child.Id} --> Op_{opId}\n";
                            Build(child);
                        }
                    }
                    else;
                }

                if (neuronSubgraph > 0)
                    fc += $"end\n";

                return fc + classDefs;
            }
        }
    }

    public class MermaidFlowchart
    {
        public class Node
        {

        }
    }
}

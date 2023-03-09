using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Xml.Linq;
using FluentMermaid;
using FluentMermaid.Enums;
using FluentMermaid.Flowchart;
using FluentMermaid.Flowchart.Enum;
using static System.Net.Mime.MediaTypeNames;

namespace MicroGrad
{
    public static class Global
    {
        public static int ValueId { get; set; }
        public static int NeuronId { get; set; }
        public static string Context { get; set; }
        public static Random Rand { get; set; } = new Random(1234);
        public static List<Value> Values { get; internal set; } = new List<Value>();
        public static string FlowchartClassDefinitions;
        public static string FlowchartDefinition =
            "flowchart LR\n" +
            "classDef Node fill:#FFFACD\n" +
            "classDef Neuron fill:#8B4513\n" +
            "classDef Add fill:#DC143C\n" +
            "classDef Multiply fill:#6495ED\n\n";

        public static void Initialize()
        {
            ValueId = NeuronId = 1;
        }
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

    [DebuggerDisplay("{Id, nq}: {W.Length, nq}")]
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
        public delegate Value Nonlinearity(Value input);
        public Nonlinearity Nonlin {get;set;} = (Value input) => input * new Value(1);
    }

    public class Layer : Module
    {
        public List<Neuron> Neurons { get; set; } = new List<Neuron>();
        public IEnumerable<Value> Parameters { get => Neurons.SelectMany(n=>n.Parameters); }
        public MLP Parent { get; set; }
        public int LayerIndex { get => Parent.Layers.IndexOf(this); }

        public Layer(int nin, int nout, Layer lastLayer)
        {
            // Set the layer's inputs to outputs of prev layer or create new values
            var X = lastLayer?.Neurons.Select(n => n.Output)
                ?? Enumerable.Range(1, nin).Select(i => new Value(null, "Input"));

            for (int i = 0; i < nout; i++)
            {
                Neurons.Add(new Neuron(X) { Layer = this, });
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
        public string Diagram { get => Global.FlowchartDefinition + string.Join("\n", Layers.Last().Neurons.Select(n => n.Output.Diagram)) + "\n\n" + Global.FlowchartClassDefinitions; }
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

            Global.Values.Add(this); // TODO: remove
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
            //        Data = Parents[0].Data + Parents[1].Data; break;
            //    case "x":
            //        Data = Parents[0].Data * Parents[1].Data; break;
            //}
            return Data;
        }

        public bool IsLeaf { get => !Parents.Any(); }
        public double Grad { get; set; }
        public string Comment { get; set; }
        public string Context { get; set; }
        public int Id { get; set; } = Global.ValueId++;
        public List<Value> Parents { get; set; } = new List<Value>();
        public Value Child { get; set; } //{ get => Global.Values.First(v => v.Parents.Contains(this)); }
        //public List<Value> Prev { get => Parents.Distinct().ToList(); }
        public Op Op { get; set; } 

        public static Value operator +(Value a, Value b)
        {
            var ret = new Value(a.Data + b.Data)
            {
                Parents = new[] { a, b }.ToList(),
                Op = new Op('+'),
            };
            a.Child = ret;
            b.Child = ret;
            ret.Neuron = b.Neuron;
            return ret;
        }

        public static Value operator *(Value a, Value b)
        {
            var ret = new Value(a.Data * b.Data)
            {
                Parents = new[] { a, b }.ToList(),
                Op = new Op('x'),
            };
            a.Child = ret;
            b.Child = ret;
            ret.Neuron = b.Neuron;
            return ret;
        }

        public void Backward()
        {

        }

        public class Link
        {
            public Link(Value from, Value to)
            {
                From = from;
                To = to;
            }

            public string Text { get => $"{From.Id} --> {To.Id}"; }
            public Value From { get; set; }
            public Value To { get; set; }

        }
        public class Node
        {
            public Node(string lineType, string nodeType, int depth, Value value = null, Value to = null)
            {
                NodeType = nodeType;
                Depth = depth;
                Value = value;

            }

            public string Text
            {
                get =>
                        NodeType == "neuron" ? "" :
                        NodeType == "Op" ? $"{OpText} %% {Depth}"
                    : null ;

                    //LineType == "link" ?

                    //$"{NodeType}{Value.Id}[{Value.Comment}: {Value.DataDisplay}]".PadRight(40, ' ') + "%% {depth}\n" :
                    //LineType == "link" ? $"{Value.Id} --> {To.Id}".PadRight(40, ' ') + "%% {depth}\n" :
                    //null;

            }
            public int Depth { get; set; }
            public Value Value { get; set; }
            //public string LineType { get; set; } // node, link
            public string NodeType { get; set; } // op, neuron
            public string OpText { get => "Op_" + Value.Parents.Select(p => p.Id.ToString()).Join("-") + "_" + Value.Id.ToString(); }

    }

    public string Diagram
        {
            get
            {
                var nodes = new List<Node>();
                var links = new List<Link>();
                var depth = 0;

                Build(this);
                void Build(Value v)
                {
                    depth++;
                    foreach (var parent in v.Parents)
                    {
                        Build(parent);
                        links.Add(new Link(parent, v)); // ($"Node_{parent.Id}", Op(v));
                    }
                    nodes.Add(new Node())

                    if (nodes.Any(dl=>dl.Value == v))
                        return;

                    string neuronId = v.Neuron?.Id.ToString();
                    //string layer = v.Neuron.Layer.
                    if (null != neuronId)
                    {
                        //fc += $"\nsubgraph Neuron_{neuronId} %% {depth}\n";
                        Global.FlowchartClassDefinitions += $"class Neuron_{neuronId} Neuron\n";
                    }

                    // Add the node to graph
                        //fc += $"Node_{v.Id}[{v.Comment}: {v.DataDisplay}] %% {depth}\n";
                        Global.FlowchartClassDefinitions += $"class Node_{v.Id} Node\n";

                    if (v.Parents.Any())
                    {
                        // Add a node showing the operation that calculated the parent node's value
                        nodes.Add(new Node("node", "op", depth, v));
                        //fc += $"{Op(v)}(({(char)v.Op.Char})) %% {depth}\n";

                        // Declare the op node's class
                        Global.FlowchartClassDefinitions += $"class {Op(v)} {v.Op.Char}\n";

                        //if (neuronId != null)
                        //    fc += $"end\n\n";

                        // Link the op node with each parent node
                        Link(Op(v), $"Node_{v.Id}");
                    }

                    //else if (neuronId != null)
                    //    fc += $"end %% {depth}\n\n";
                    else;
                    depth--;
                }

                //string Op(Value v) => "Op_" + v.Parents.Select(p => p.Id.ToString()).Join("-") + "_" + v.Id.ToString();
                //void Add(Value v, string type)
                //{
                //    //lines.Add(new OrderedLine(type, ) { Depth = depth, Value = v });
                //}
                //void Link(string from, string to)
                //{
                //    fc += $"{from} --> {to}".PadRight(40, ' ') + "%% {depth}\n";
                //}

                foreach (var layer in nodes.GroupBy(l => l.Value.Neuron.Layer))
                {
                    Global.FlowchartClassDefinitions += $"class Layer_{layer.Key.LayerIndex} Layer\n";

                    foreach (var neuron in layer.GroupBy(l => l.Value.Neuron))
                    {
                        Global.FlowchartClassDefinitions += $"class Neuron_{neuron.Key.Id} Neuron\n";

                    }
                }

                return fc;
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

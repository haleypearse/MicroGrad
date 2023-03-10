using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using FluentMermaid;
using FluentMermaid.Enums;
using FluentMermaid.Flowchart;
using FluentMermaid.Flowchart.Enum;
using static System.Net.Mime.MediaTypeNames;
using static MicroGrad.Value;

namespace MicroGrad
{
    public static class Global
    {
        public static int ValueId { get; set; }
        public static int NeuronId { get; set; }
        public static string Context { get; set; }
        public static Random Rand { get; set; } = new Random(1234);
        public static List<Value> Values { get; internal set; } = new List<Value>();
        public static List<string> FlowchartClassDefinitions { get; set; } =
            ("classDef Node fill:#FFFACD;" +
            "classDef Neuron fill:#8B4513;" +
            "classDef Add fill:#DC143C;" +
            "classDef Multiply fill:#6495ED;"
            ).Split(new char[] {';' },StringSplitOptions.RemoveEmptyEntries).ToList();
        public static string FlowchartDefinition = "flowchart LR\n";

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
        public string Diagram { get => Global.FlowchartDefinition + string.Join("\n", Layers.Last().Neurons.Select(n => Value.GetDiagram(n.Output))) + "\n\n" + Global.FlowchartClassDefinitions.Distinct().Join("\n"); }
        public string OldDiagram { get => Global.FlowchartDefinition + string.Join("\n", Layers.Last().Neurons.Select(n => n.Output.Diagram)) + "\n\n" + Global.FlowchartClassDefinitions; }

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
            if (comment == null)
                ;
            Comment = comment;
            Context = Global.Context?.ToString();
            Data = data ?? Global.Rand.NextDouble() * 2 - 1;
            Neuron = neuron;

            Global.Values.Add(this); // TODO: remove
        }

        public Neuron Neuron { get; set; }
        public double Data { get; set; }
        public string DataDisplay { get => Data.ToString().Substring(0, 6); }
        public string IdDisplay { get => "N" + Id.ToString().PadLeft(Global.ValueId.ToString().Length, '0'); }

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
        //public string NodeDisplay
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
        public string DiagramText()
        {
            return $"{IdDisplay}[{Comment}: {DataDisplay}]";
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
            public Node(string nodeType, Value value = null, Value to = null)
            {
                NodeType = nodeType;
                Value = value;

            }

            public string DiagramText
            {
                get =>
                        NodeType == "neuron" ? $"{NodeType}{Value.Id}[{Value.Comment}: {Value.DataDisplay}]" :
                        NodeType == "op" ? $"{OpText} %% {Depth}"
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

        /// <summary>
        /// recreation of Karpathy's but it seems like it can't work
        /// </summary>
        public List<Value> TopologicalParentValues
        {
            get
            {
                var ret = new List<Value>();
                var visited = new List<Value>();
                Build(this);
                void Build(Value v)
                {
                    if (!visited.Contains(v))
                    {
                        visited.Add(v);
                        foreach (var parent in Parents)
                            Build(parent);
                        ret.Add(v);
                    }
                    else;
                }
                return ret;
            }
        }

        public static (List<Value>, List<(Value, Value)>) NodesAndLinks(Value root)
        {
            var nodes = new List<Value>();
            var links = new List<(Value, Value)>();
            Build(root);
            void Build(Value v)
            {
                if (!nodes.Contains(v))
                {
                    nodes.Add(v);
                    foreach (var parent in v.Parents)
                    {
                        links.Add((parent, v));
                        Build(parent);
                    }
                }
                else;
            }
            return (nodes, links);
        }
        
        public static string GetDiagram(Value root)
        {
            var nodesAndLinks = Value.NodesAndLinks(root);
            var nodes = nodesAndLinks.Item1;
            var linksToAdd = nodesAndLinks.Item2.ToList();

            var links = new StringBuilder();
            var fc = new StringBuilder();

            foreach (var layerGroup in nodesAndLinks.Item1.GroupBy(l => l.Neuron?.Layer))            {
                var layer = layerGroup.Key?.LayerIndex;
                if (layer != null)
                {
                    fc.Append($"subgraph Layer_{layer}\n");
                    Global.FlowchartClassDefinitions.Add($"class Layer_{layer} Layer");
                }

                foreach (var neuronGroup in layerGroup.GroupBy(l => l.Neuron))
                {
                    var neuron = neuronGroup.Key?.Id;
                    if (neuron != null)
                    {
                        fc.Append($"\tsubgraph Neuron_{neuron}\n");
                        Global.FlowchartClassDefinitions.Add($"class Neuron_{neuron} Neuron");
                    }
                    else;

                    foreach (var value in neuronGroup)
                    {
                        fc.Append(value.DiagramText() + "\n");
                        foreach (var link in linksToAdd.Where(l => l.Item1 == value).ToList())
                        {
                            fc.Append(link.Item1.DataDisplay + " --> " + link.Item2.DataDisplay + "\n");
                            linksToAdd.Remove(link);
                        }
                    }

                    // close neuron subgraph
                    if (neuron != null)
                        fc.Append($"\tend\n");
                }

                // close layer subgraph
                if (layer != null)
                    fc.Append($"end\n");

                // add remaining links
                foreach (var link in linksToAdd)
                {
                    fc.Append(link.Item1.DataDisplay + " --> " + link.Item2.DataDisplay + "\n");
                    //linksToAdd.Remove(link);
                }
            }

            return links.ToString() + fc.ToString();

        }


        public string Diagram
        {
            get
            {
                var nodes = new List<Node>();
                //var links = new List<Link>();
                var links = new StringBuilder();
                //var depth = 0;

                Build(this);
                void Build(Value v)
                {
                    //depth++;
                    foreach (var parent in v.Parents)
                    {
                        Build(parent);
                        links.Append($"Node_{parent.Id} --> Node_{v.Id}\n"); // ($"Node_{parent.Id}", Op(v));
                    }
                    nodes.Add(new Node("node", v));

                    if (nodes.Any(dl=>dl.Value == v))
                        return;

                    string neuronId = v.Neuron?.Id.ToString();
                    //string layer = v.Neuron.Layer.
                    if (null != neuronId)
                    {
                        //fc += $"\nsubgraph Neuron_{neuronId} %% {depth}\n";
                        //Global.FlowchartClassDefinitions += $"class Neuron_{neuronId} Neuron\n";
                    }

                    // Add the node to graph
                        //fc += $"Node_{v.Id}[{v.Comment}: {v.DataDisplay}] %% {depth}\n";
                        //Global.FlowchartClassDefinitions += $"class Node_{v.Id} Node\n";

                    if (v.Parents.Any())
                    {
                        nodes.Add(new Node("op", v));
                        links.Append($"{Op(v)} --> Node_{v.Id}\n");
                    }

                }

                string Op(Value v) => "Op_" + v.Parents.Select(p => p.Id.ToString()).Join("-") + "_" + v.Id.ToString();
                //void Add(Value v, string type)
                //{
                //    //lines.Add(new OrderedLine(type, ) { Depth = depth, Value = v });
                //}
                //void Link(string from, string to)
                //{
                //    fc += $"{from} --> {to}".PadRight(40, ' ') + "%% {depth}\n";
                //}
                var fc = new StringBuilder();

                foreach (var layerGroup in nodes.GroupBy(l => l.Value.Neuron?.Layer))
                {
                    var layer = layerGroup.Key?.LayerIndex;
                    if (layer != null)
                    {
                        fc.Append($"\nsubgraph Layer_{layer}\n");
                        //Global.FlowchartClassDefinitions += $"class Layer_{layer} Layer\n";
                    }

                    foreach (var neuronGroup in layerGroup.GroupBy(l => l.Value.Neuron))
                    {
                        var neuron = neuronGroup.Key?.Id;
                        if (neuron != null)
                        {
                            fc.Append($"\nsubgraph Neuron_{neuron}\n");
                            Global.FlowchartClassDefinitions.Append($"class Neuron_{neuron} Neuron");
                            //Global.FlowchartClassDefinitions += $"class Neuron_{neuron} Neuron\n";
                        }
                            
                        foreach (var value in neuronGroup)
                            fc.Append(value.DiagramText);

                        if (neuron != null)
                            fc.Append($"\nend\n");
                    }
                    if (layer != null)
                        fc.Append($"\nend\n");
                }

                return links.ToString() + fc.ToString();
            }
        }
    }
}

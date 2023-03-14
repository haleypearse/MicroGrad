using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
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

[assembly: DebuggerDisplay("{Key,nq}: {Value,nq}", Target = typeof(KeyValuePair<int, MicroGrad.Value>))]

namespace MicroGrad
{

    public static class Global
    {
        //public static int ValueId { get; set; }
        public static int GetId(string type, object obj)
        {
            if (!Ids.ContainsKey(type))
                Ids.Add(type, new Dictionary<int, object>());
            var newId = Ids[type].Keys.LastOrDefault() + 1;
            Ids[type].Add(newId, obj);
            return newId;

        }
        //public static Dictionary<string, Dictionary<int, object>> Ids { get; set; } = new Dictionary<string, Dictionary<int, object>>();
        public static Dictionary<string, Dictionary<int, object>> Ids { get; set; }
        public static Dictionary<char, Op> Ops { get; set; } = "x+-/^".ToDictionary(c=>c, c=>new Op(c));
        //public static int NeuronId { get; set; }
        //public static string Context { get; set; }
        public static Random Rand { get; set; }
        //public static List<Value> Values { get; internal set; }
        public static Dictionary<string,string> FlowchartClassDefinitions { get; set; } =
            ("Node~fill:#FFFACD/" +
            "Neuron~fill:#8B4513/" +
            "Layer~fill:#331122/" + //, height:100%
            "Add~fill:#DC143C/" +
            "Multiply~fill:#6495ED/"
            ).Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
            .ToDictionary(c => c.Split('~')[0], c => c.Split('~')[1]);
        public static List<string> FlowchartClassAssignments { get; set; } = new List<string>();
        public static string FlowchartDefinition = "flowchart LR\n" +
            "%%{ init: { 'theme': 'dark', 'themeVariables': {'textColor': '#fff', 'borderColor': '#7C0000', 'lineColor': '#F8B229'}}}%%\n";

        public static void Initialize()
        {
            Rand = new Random(1234);
            Ids = new Dictionary<string, Dictionary<int, object>>();
            //Values = new List<Value>();
            //ValueId = NeuronId = 1;
        }
    }

    public class Record
    {
        public Record()
        {
            Id = Global.GetId(GetType().Name, this);
        }

        public string TypeSpecificId { get => GetType().Name + "_" + Id.ToString(); } //.PadLeft(3, '0')
        public int Id { get; } // = Global.Id++;
        //public int Id { get; set; } = Global.ValueId++;
    }
    public class Module : Record
    {
        // Weights and biases
        public IEnumerable<Value> Parameters { get; }
    }

    [DebuggerDisplay("{Id, nq}: {W.Length, nq}")]
    public class Neuron : Module
    {
        public Neuron(IEnumerable<Value> X, string nonlin = null)
        {
            //Id = Global.NeuronId++;
            Initialize(X);
            switch (nonlin)
            {
                case "relu":
                    break;
            }
        }

        public void Initialize(IEnumerable<Value> X)
        {
            W = Enumerable.Range(1, X.Count()).Select(i => new Value(null, "W" + i, this)).ToArray();
            b = new Value(null, "B", this);
            var products = X.Zip(W).Select(xw => xw.First * xw.Second);
            Output = products.Aggregate((x, y) => x + y) + b;
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

        public IEnumerable<Value> Parameters { get => W.Append(b); }
        public List<Value> Values { get; set; } = new List<Value>();
        public Value[] W { get; set; }
        public Value[] X { get; set; }
        public Value b { get; set; }
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
            var X = lastLayer?.Neurons.Select(n => n.Output);
            if (X == null)
            {
                X = Enumerable.Range(1, nin).Select(i => new Value(null, "I")).ToList();
                //var inputNeuron = 
            }

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
            Root.Parents.AddRange(Layers.Last().Neurons.Select(n=>n.Output));
            //Layers.Last().Neurons.ForEach(n=>n.Output.Child = Root);
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
        /// <summary>
        /// Output values are gathered under this root node for convenience
        /// </summary>
        public Value Root { get; set; } = new Value(null, "Root");
        public string Diagram { get
            {
                return Global.FlowchartDefinition + Value.GetDiagram(Root) + "\n\n"
                    + string.Join("",
                    Global.FlowchartClassDefinitions
                    //.Where(def=>Global.FlowchartClassAssignments.Any(s=> def.Key == s.Split(' ')[1].Split('_').First()))
                    .Select(def=>$"classDef {def.Key} {def.Value}\n"))
                    + Global.FlowchartClassAssignments.Distinct().Join("\n"); 
            } }
    }

    [DebuggerDisplay("{Display, nq}")]
    public class Op
    {
        public Op(OpChar opChar) => this.Char = opChar;
        public Op(char opChar) => this.Char = (OpChar)(opChar);

        public OpChar Char { get; set; }
        public string Display { get => ((char)Char).ToString(); }
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

    [DebuggerDisplay("{DebuggerDisplay()}")]
    public class Value : Record
    {
        public Value(double? data = null, string comment = null, Neuron neuron = null, List<Value> parents = null, Op op = null)
        {
            Comment = comment;
            Data = data ?? Global.Rand.NextDouble() * 2 - 1;
            Neuron = neuron;
            Parents = parents ?? new List<Value>();
            Op = op;

            foreach (var combinable in Parents.Where(p => p.Op == op).ToList())
            {
                Parents.InsertRange(Parents.IndexOf(combinable),combinable.Parents);
                Parents.Remove(combinable);
            }
        }

        public Neuron Neuron { get; set; }
        public double Data { get; set; }
        public string DebuggerDisplay()
        {
            return $"{Parents.Select(p=>p.Comment+p.Neuron?.Id).Join("-")}_{Comment+Neuron?.Id}, {DataDisplay}";

        }
        public string DataDisplay { get => Math.Round(Data,3).ToString(); }
        //public string IdDisplay { get => "Node_" + Id.ToString().PadLeft(3, '0'); }

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
        public List<Value> Parents { get; set; }
        public Value Child { get; set; } //{ get => Global.Values.First(v => v.Parents.Contains(this)); }
        //public List<Value> Prev { get => Parents.Distinct().ToList(); }
        public Op Op { get; set; } 

        public static Value operator +(Value a, Value b)
        {
            var ret = new Value(a.Data + b.Data, null, b.Neuron, new List<Value>() { a, b }, Global.Ops['+']);
            a.Child = ret;
            b.Child = ret;
            return ret;
        }

        public static Value operator *(Value a, Value b)
        {
            var ret = new Value(a.Data + b.Data, null, b.Neuron, new List<Value>() { a, b }, Global.Ops['x']);
            a.Child = ret;
            b.Child = ret;
            return ret;
        }

        public void Backward()
        {

        }
        public string DiagramText()
        {
            return $"{TypeSpecificId}[{DebuggerDisplay()}]";
            return Op == null
                ? $"{TypeSpecificId}[{Comment}: {DataDisplay}]"
                : $"{TypeSpecificId}[{Op.Display}: {DataDisplay}]";
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

        public static (List<Value>, List<(Value, Value)>) NodesAndLinks(Value root, bool ExcludeRoot = false)
        {
            var nodes = new List<Value>();
            var links = new List<(Value, Value)>();
            Build(root);
            void Build(Value v)
            {
                if (!nodes.Contains(v))
                {
                    if (v != root || !ExcludeRoot)
                        nodes.Add(v);
                    foreach (var parent in v.Parents)
                    {
                        if (v != root || !ExcludeRoot)
                            links.Add((parent, v));
                        Build(parent);
                    }
                }
            }
            //if (ExcludeRoot)
            //{
            //    nodes.Remove(root);
            //    links = links.Where(l => l.Item1 == root || l.Item2 == root).ToList();
            //}
            return (nodes, links);
        }
        
        public static string GetDiagram(Value root)
        {
            var nodesAndLinks = Value.NodesAndLinks(root, true);
            var nodes = nodesAndLinks.Item1;
            var valuesToLink = nodesAndLinks.Item2.ToList();
            var indent = 0;
            //var links = new StringBuilder();
            var lines = new List<string>();
            var fc = new StringBuilder();

            foreach (var layerGroup in nodesAndLinks.Item1.GroupBy(l => l.Neuron?.Layer).OrderBy(g=>g.Key == null))
            {
                // Open layer subgraph
                var layer = layerGroup.Key?.TypeSpecificId;
                if (layer != null)
                {
                    AddLine($"subgraph {layer}");
                    Global.FlowchartClassAssignments.Add($"class {layer} Layer");
                    indent ++;
                }


                foreach (var neuronGroup in layerGroup.GroupBy(l => l.Neuron).OrderBy(g => g.Key == null))
                {
                    // Open neuron subgraph
                    var neuron = neuronGroup.Key?.TypeSpecificId;
                    if (neuron != null)
                    {
                        AddLine($"subgraph {neuron}");
                        Global.FlowchartClassAssignments.Add($"class {neuron} Neuron");
                        indent ++;
                    }

                    foreach (var value in neuronGroup)
                    {
                        AddLine(value.DiagramText());
                        AddOpsOrLinks(neuron: neuronGroup.Key);
                    }

                    // close neuron subgraph
                    if (neuron != null)
                    {
                        indent--;
                        AddLine("end");
                    }
                }

                // close layer subgraph
                if (layer != null)
                {
                    indent--;
                    AddLine("end");
                }
            }

            AddOpsOrLinks(); // Add remaining

            return lines.Join("\n");

            // When neuron parameter is set, the op will be added to the neuron, but adding links is postponed
            void AddOpsOrLinks(Neuron neuron = null)
            {
                var linksToChildren = valuesToLink.Where(l => neuron == null || l.Item2.Neuron == neuron).GroupBy(l => l.Item2);

                foreach (var linksToChild in linksToChildren)
                {
                    // If child is the result of an operation
                    if (linksToChild.Key.Op != null)
                    {
                        // Reuse summation and multiplication nodes instead of chaining them
                        // If



                        var opName = Op(linksToChild.Key);
                        var line = $"{opName}(({((char)linksToChild.Key.Op.Char)}))";

                        if (linksToChild.Count() > 1 && linksToChild.All(l=>l.Item1.Op?.Char == l.Item2.Op?.Char))
                        {

                        }
                        if (!lines.Any(l=>l.Trim(new char[] {' ', '\t', '\n'}).Equals(line)))
                            AddLine(line);
                        else;

                        if (neuron == null)
                        {
                            // Link parents to op
                            foreach (var link in linksToChild.ToList())
                            {
                                AddLink(link.Item1.TypeSpecificId, opName);
                                valuesToLink.Remove(link);
                            }

                            // Link op to child
                            AddLink(opName, linksToChild.Key.TypeSpecificId);
                        }
                    }
                    else
                    {
                        // Link directly
                        if (neuron == null)
                            foreach (var link in linksToChild.ToList())
                            {
                                AddLink(link.Item1.TypeSpecificId, link.Item2.TypeSpecificId);
                                valuesToLink.Remove(link);
                            }
                    }
                }
            }
            void AddLine(string s) => lines.Add(new string('\t', indent) + s); 
            void AddLink(string from, string to) => lines.Add(new string('\t', indent) + from + " --> " + to);
            string Op(Value v) => v.Parents.Select(p=>p.Id.ToString()).Join("-") + "_" + v.Id.ToString();

        }
    }
}

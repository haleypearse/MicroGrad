using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using MermaidNetHtmlBuilder;
using System.IO;
using System.Security.Principal;
using System.Xml;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using System.IO.Compression;
using FluentMermaid.Enums;
using FluentMermaid.Flowchart;
using FluentMermaid.Flowchart.Enum;
using System.Text.RegularExpressions;
using FluentMermaid;

namespace MicroGrad
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //NNFun();

            CreateHostBuilder(args).Build().Run();
        }


        private static string CreateDiagram(Value root)
        {
            var nodes = new Dictionary<Value, INode>(); // new List<INode>();
            var chart = FlowChart.Create(Orientation.LeftToRight);

            Build(root);
            void Build(Value node)
            {
                INode iNode;
                if (!nodes.ContainsKey(node))
                {
                    iNode = chart.TextNode($"{node.Id}: {node.DataDisplay}", Shape.RoundEdges);
                    nodes.Add(node, iNode);
                }
                else
                    iNode = nodes[node];

                if (node.Parents.Any())
                {

                    var opNode = chart.TextNode(((char)node.Op.Char).ToString(), Shape.Circle);
                    chart.Link(opNode, nodes[node], Link.Arrow, "");
                    foreach (var child in node.Parents)
                    {
                        Build(child);
                        chart.Link(nodes[child], opNode, Link.Arrow, "");


                    }
                }
            }


            string mermaidSyntax = chart.Render();// Regex.Unescape(chart.Render());
            return mermaidSyntax;
            var e = new List<INode>();

            ; 

            //var outputHtml = new HtmlDocument();
            //outputHtml.LoadHtml("<html><body></body></html>");
            //var body = outputHtml.QuerySelector("body");

            //var merm = new MermaidHtmlBuilder();

            //var builder = new MermaidHtmlBuilder();
            //var descriptions = new Dictionary<string, string>();
            //var outDir = Path.GetDirectoryName(@"C:\Users\Haley\OneDrive - StandardFusion\SF\");
            //var outFileName = "Mermaid Test.zip"
            //;
            ////builder.SaveZip(Path.GetDirectoryName(inputXml) + outFileName, diagram, descriptions);
            //var zipArray = builder.BuildZip(diagram, descriptions);
            //var zipStream = new MemoryStream();
            //zipStream.Write(zipArray, 0, zipArray.Length);
            //var zip = new ZipFile(zipStream);

            //ZipInputStream inStream = new ZipInputStream(zipStream);


            //foreach (ZipEntry ze in zip)
            //    if (ze.Name == "index.html")
            //    {
            //        string text = new StreamReader(zip.GetInputStream(ze)).ReadToEnd();

            //        var html = new HtmlDocument();
            //        html.LoadHtml(text);
            //        var incomingBody = HtmlNode.CreateNode(html.QuerySelector("body").OuterHtml);
            //        scripts = incomingBody.QuerySelectorAll("script").ToList();

            //        var workflowHtmlSection = HtmlNode.CreateNode("<div></div>");
            //        workflowHtmlSection.AppendChild(HtmlNode.CreateNode($"<h2>{workflowName}</h2>"));
            //        workflowHtmlSection.AppendChild(incomingBody.QuerySelector(".mermaid"));
            //        //if (body.QuerySelector("script") == null)
            //        //    body.AppendChildren(scripts);
            //        //else;

            //        body.AppendChild(workflowHtmlSection);
            //    }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }

    public static class ExtensionMethods
    {
        public static Value Value(this double val) => new Value(val);
        public static string Join(this string[] s, string separator = null) => string.Join(separator ?? "", s);
        public static string Join(this IEnumerable<string> s, string separator = null) => string.Join(separator ?? "", s.ToArray());
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace MicroGrad.Pages
{
    public class IndexModel : PageModel
    {
        public string DiagramContent { get; set; }
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public string Diagram { get; set; }

        public void OnGet()
        {
            NNFun();
        }


        public void NNFun()
        {
            //var a = 3.0.Value();
            //var b = 4.0.Value();
            //var c = a * b;
            //var d = 5.0.Value();
            //var e = c + d;


            var mlp = new MLP(new[] { 2, 2, 1 });
            var last = mlp.Layers.Last().Neurons.Last().Output;
            DiagramContent = last.Diagram;

            //string myname = Request.Form["first_name_txt"];

            //var flowchartNode = 

            //var test = mlp.Forward(new double[] { 2 });

            //var props = test.GetType().GetProperties();
            ;
            //CreateDiagram(e);
        }
    }
}

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
        private readonly ILogger<IndexModel> _logger;
        public string DiagramContent { get; set; }
        public string Diagram { get; set; }
        public int[] NNParams { get; private set; } = new[] {  2,1 };
        public string InvalidParams { get; set; }

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
            Global.Initialize();
        }


        public void OnGet()
        {
            NNFun();
        }
        
        public void OnPost()
        {
            Global.Initialize();
            var content = Request.Form["LayerSizes"];
            try
            {
                NNParams = content.ToString().Split(',').Select(s => int.Parse(s)).ToArray();
            }
            catch
            {
                InvalidParams = content;
            }

            NNFun();
        }




        public void NNFun()
        {
            //var topoTest = Value.NodesAndLinks(new MLP(NNParams).Layers.Last().Neurons.Last().Output);
            //var diagramText = Value.GetDiagram(new MLP(NNParams).Layers.Last().Neurons.Last().Output);

            var mlp = new MLP(NNParams);
            DiagramContent = mlp.Diagram;
            //DiagramContent = diagramText;

        }
    }
}

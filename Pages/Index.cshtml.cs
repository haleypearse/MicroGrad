﻿using System;
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
        public int[] NNParams { get; private set; } = new[] { 2, 3, 1 };

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
            NNParams = content.ToString().Split(',').Select(s => int.Parse(s)).ToArray();

            NNFun();
        }




        public void NNFun()
        {

            DiagramContent = new MLP(NNParams).Diagram;

        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PicklesDoc.Pickles.Test;

namespace Pickles.Test.CL
{
    class Program
    {
        static void Main(string[] args)
        {
            var test = new PicklesDoc.Pickles.Test.Formatters.JSON.WhenFormattingAFolderStructureWithFeatures();
            test.ShouldContainTheFeatures();
        }
    }
}

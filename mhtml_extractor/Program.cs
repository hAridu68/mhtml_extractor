using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
namespace mhtml_extractor
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {                
                Console.WriteLine("Usage: {Process.GetCurrentProcess().ProcessName} file.mhtml");
                Console.WriteLine("-----------");
                Console.WriteLine($"{"",-2}MHTML (MIME encapsulation of aggregate HTML documents) Extructor Content of MIMEs");
                return;
            }
            ItemExtractor ti = new ItemExtractor(args[0]);
            ti.Extruct();
        }
    }
}

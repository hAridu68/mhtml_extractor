using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mhtml_extractor
{
    class Program
    {
        static void Main(string[] args)
        {
            ItemExtractor ti = new ItemExtractor(@"D:\Shared\n\COBA.mhtml");
            ti.Extruct();
            Console.ReadLine();
        }
    }
}

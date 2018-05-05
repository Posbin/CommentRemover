using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace CommentRemover
{
    class Program
    {
        static void Main(string[] args)
        {
            string rootFolder = "./src";
            string[] targetExtensions = new string[] {"*.cs", "*.cpp", "*.h"};
            CommentRemover.Remove(rootFolder, targetExtensions);
            Console.WriteLine("Finished!!");
        }
    }
}

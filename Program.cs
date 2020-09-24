using System;
using System.IO;

namespace s02170142
{
    class Program
    {
        static void Main(string[] args)
        {
            //Create array of images to work with all of them
            String[] filePaths = Directory.GetFiles(@".");
            foreach (string var in filePaths)
            {
                Console.WriteLine(var);
            }
           

        }
    }
}

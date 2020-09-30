using System;
using SixLabors.ImageSharp; // Из одноимённого пакета NuGet
using SixLabors.ImageSharp.PixelFormats;
using System.Linq;
using SixLabors.ImageSharp.Processing;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntime;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
namespace MainApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello user!");
            Console.WriteLine("Please enter direcory path you would like to process");
            Console.WriteLine("Or press enter to use default res folder ");
            String inputDir = "";
            inputDir = Console.ReadLine();
            if (inputDir.Length < 2)
                //inputDir = "/Users/denis/Documents/C#/Lab1WithLibs/s02170142/ImgProcLib/res";
                inputDir=@"./ImgProcLib/res";
            ImgProcLib.ImageProcClass imgProc = new ImgProcLib.ImageProcClass(inputDir);
            // Console.WriteLine(imgProc.GetFilePaths() == null);
            if (imgProc.GetFilePaths() == null)

                return;


            _=Task.Run(() =>
            {
                Console.WriteLine("Press Spacebar to cancel");
                while (!(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Spacebar))
                {
                }
                imgProc.InterruptTasks();
            });
            await imgProc.StartProc();
             




        }
    }
}

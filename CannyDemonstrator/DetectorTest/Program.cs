using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CannyDemonstrator;
using System.Diagnostics;

namespace DetectorTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch stopwatch = new Stopwatch();

            CannyEdgeDetector detector = new CannyEdgeDetector();
            string fileName = @"C:\Users\Muhamed\Desktop\Coding\Images\plane.jpg";

            detector.Options.maxThreadCount = 2;
            detector.LoadImage(fileName);
            stopwatch.Start();
            detector.Run();
            stopwatch.Stop();
            Console.WriteLine(stopwatch.ElapsedMilliseconds);
            Console.ReadKey();
            detector.BitmapSequence[1].Save(@"C:\Users\Muhamed\Desktop\Coding\Images\gray.png");
        }
    }
}

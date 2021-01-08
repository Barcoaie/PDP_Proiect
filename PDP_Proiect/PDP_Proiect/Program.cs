using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDP_Proiect
{
    class Program
    {
        public static int nr_threads = 1;
        static void Main(string[] args)
        {
            Image image = new Image(@"D:\Faculta\3rd Year\PDP\PDP_Proiect\PDP_Proiect\PDP_Proiect\img.png");
            Hough hough = new Hough(image);
            hough.putLines();
            image.writeToFile(@"D:\Faculta\3rd Year\PDP\PDP_Proiect\PDP_Proiect\PDP_Proiect\final.png");
        }
    }
}

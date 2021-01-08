using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PDP_Proiect
{
    class Image
    {
        public Image(string path)
        {
            Bitmap bmp = null;
            try
            {
                bmp = new Bitmap(path);
                height = bmp.Height;
                width = bmp.Width;
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
            RGBtoGray(bmp);
        }

        private void RGBtoGray(Bitmap bmp)
        {
            int elemPerThr = (width * height) / Program.nr_threads;
            rValues = new int[height][];
            gValues = new int[height][];
            bValues = new int[height][];
            gray = new int[height][];
            for (int i = 0; i < rValues.Length; i++)
            {
                rValues[i] = new int[width];
                gValues[i] = new int[width];
                bValues[i] = new int[width];
                gray[i] = new int[width];
            }

            int ord = 1;
            List<Task<bool>> tasks = new List<Task<bool>>();
            for (int i = 0; i < Program.nr_threads; i++)
            {
                if (i + 1 == Program.nr_threads)
                {
                    elemPerThr += (width * height) % Program.nr_threads;
                }
                Pair<int, int> startXY = getXY(width, ord);
                int finalElemPerThr = elemPerThr;
                tasks.Add(Task.Factory.StartNew(()=> {
                    processRGB(startXY, finalElemPerThr, bmp);
                    return true;
                }));
                ord += elemPerThr;
            }

            Task.WaitAll(tasks.ToArray());
        }

        private void processRGB(Pair<int, int> startXY, int elems, Bitmap bmp)
        {
            int row = startXY.First;
            int col = startXY.Second;
            int done = 0;

            while (done < elems && row < height)
            {
                if (col == width)
                {
                    col = 0;
                    row++;
                    if (row == height) break;
                }
                Color rgb = bmp.GetPixel(col,row);
                rValues[row][col] = rgb.R;
                gValues[row][col] = rgb.G;
                bValues[row][col] = rgb.B;
                gray[row][col] = (int)(rValues[row][col] * 0.299 + gValues[row][col] * 0.587 + bValues[row][col] * 0.114);
                gray[row][col] = gray[row][col] > 255 ? 255 : gray[row][col];
                done++;
                col++;
            }
        }

        private Pair<int, int> getXY(int width, int ord)
        {
            if (ord % width == 0)
            {
                return new Pair<int, int>(ord / width - 1, width - 1);
            }
            return new Pair<int, int>(ord / width, ord % width - 1);
        }

        public void setPoint(Pair<int,int> XY, int r, int g, int b)
        {
            if (XY.First < 0 || XY.First > height || XY.Second < 0 || XY.Second > width)
            {
                return;
            }
            rValues[XY.First][XY.Second] = r;
            gValues[XY.First][XY.Second] = g;
            bValues[XY.First][XY.Second] = b;
        }

        public int[][] doSobel()
        {
            sobel = new int[height][];
            for (int i = 0; i < sobel.Length; i++)
            {
                sobel[i] = new int[width];
            }

            for (int i = 0; i < height; i++)
            {
                sobel[i][0] = gray[i][0];
                sobel[i][width - 1] = gray[i][width - 1];
            }
            for(int i = 0; i < width; i++)
            {
                sobel[0][i] = gray[0][i];
                sobel[height - 1][i] = gray[height - 1][i];
            }

            List<Task<bool>> tasks = new List<Task<bool>>();
            int elemPerThr = (width * height) / Program.nr_threads;
            int ord = 1;
            for(int i = 0; i < Program.nr_threads; i++)
            {
                if (i + 1 == Program.nr_threads)
                {
                    elemPerThr += (width * height) % Program.nr_threads;
                }
                int finalElemPerThr = elemPerThr;
                int finalOrd = ord;
                Pair<int, int> XY = getXY(width, finalOrd);
                tasks.Add(Task.Factory.StartNew(() => {
                    doSobelWerk(XY, finalElemPerThr);
                    return true;
                }));
                ord += elemPerThr;
            }
            Task.WaitAll(tasks.ToArray());

            gray = sobel;
            return sobel;
        }

        private void doSobelWerk(Pair<int,int> XY, int elems)
        {
            int row = XY.First;
            int col = XY.Second;
            int done = 0;
            while (done < elems && row < height)
            {
                if(col == width)
                {
                    col = 0;
                    row++;
                    if (row == height) break;
                }
                if (row != 0 && row != height - 1 && col != 0 && col != width - 1)
                {
                    int value00 = gray[row - 1][col - 1];
                    int value01 = gray[row - 1][col];
                    int value02 = gray[row - 1][col + 1];
                    int value10 = gray[row][col - 1];
                    int value11 = gray[row][col];
                    int value12 = gray[row][col + 1];
                    int value20 = gray[row + 1][col - 1];
                    int value21 = gray[row + 1][col];
                    int value22 = gray[row + 1][col + 1];

                    int gx = ((-1 * value00) + (0 * value01) + (1 * value02))
                        + ((-2 * value10) + (0 * value11) + (2 * value12))
                        + ((-1 * value20) + (0 * value21) + (1 * value22));

                    int gy = ((-1 * value00) + (-2 * value01) + (-1 * value02))
                        + ((0 * value10) + (0 * value11) + (0 * value12))
                        + ((1 * value20) + (2 * value21) + (1 * value22));
                    double gvalue = Math.Sqrt(gx * gx + gy * gy);
                    int g = (int)gvalue;
                    lock(sobelLock)
                    {
                        if(maxGradient < g)
                        {
                            maxGradient = g;
                        }
                    }
                    sobel[row][col] = g;
                    if (g > 255) sobel[row][col] = 255;
                    if (g < 0) sobel[row][col] = 0;

                    if (sobel[row][col] < 128) sobel[row][col] = 0;
                    else sobel[row][col] = 255;
                }
                col++;
                done++;
            }
        }
        
        public void writeToFile(string path)
        {
            int elemPerThr = (width * height) / Program.nr_threads;
            int ord = 1;
            List<Task<bool>> tasks = new List<Task<bool>>();
            Bitmap bitmap = new Bitmap(width, height);
            for (int i = 0; i < Program.nr_threads; i++)
            {
                if (i + 1 == Program.nr_threads)
                {
                    elemPerThr += (width * height) % Program.nr_threads;
                }
                Pair<int, int> XY = getXY(width, ord);
                int finalElemPerThr = elemPerThr;
                tasks.Add(Task.Factory.StartNew(() =>
                {
                    writeColouredToFile(XY, finalElemPerThr, ref bitmap);
                    return true;
                }));
                ord += elemPerThr;
            }

            bitmap.Save(path);
        }

        public void writeColouredToFile(Pair<int,int> XY, int elems, ref Bitmap bmp)
        {
            int row = XY.First;
            int col = XY.Second;
            int done = 0;
            while (done < elems && row < height)
            {
                //Bitmap bitmap = new Bitmap(bmp);
                if (col == width)
                {
                    col = 0;
                    row++;
                    if (row == height) break;
                }
                Color rgb = new Color();
                rgb = Color.FromArgb(rValues[row][col],gValues[row][col],bValues[row][col]);
                bmp.SetPixel(col, row, rgb);
                done++;
                col++;
                //bmp = bitmap;
            }
        }

        public int maxGradient = -1;
        private readonly object sobelLock = new object();
        private int[][] sobel { get; set; }
        public int height { get; set; }
        public int width { get; set; }
        private int[][] rValues { get; set; }
        private int[][] gValues { get; set; }
        private int[][] bValues { get; set; }
        private int[][] gray { get; set; }
    }
}

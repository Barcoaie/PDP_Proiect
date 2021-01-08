using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDP_Proiect
{
    class Hough
    {
        public Hough(Image img)
        {
            this.image = img;
            int rValue = (int)Math.Sqrt(image.width * image.width + image.height * image.height);
            houghArr = new int[180][];
            for (int i = 0; i < 180; i++)
            {
                houghArr[i] = new int[2 * rValue];
            }
            for (int i = 0; i < 180; i++)
                for (int j = 0; j < 2 * rValue; j++)
                {
                    houghArr[i][j] = 0;
                }
            filteredImg = image.doSobel();
            executeTrans();
        }

        private void executeTransTask(Pair<int, int> startXY, int nr_elems, int[][] img)
        {
            int row = startXY.First;
            int col = startXY.Second;
            int done = 0;
            int initialR = (int)Math.Sqrt(image.width * image.width + image.height * image.height);
            while (done < nr_elems && row < img.Length)
            {
                if(col == img[0].Length)
                {
                    col = 0;
                    row++;
                    if (row == img.Length) break;
                }
                if (img[row][col] != 0)
                {
                    for (int unghi = 0; unghi < 180; unghi++) {
                        int r = (int)(row * Math.Cos((Math.PI * unghi) / 180) + col * Math.Sin((Math.PI * unghi) / 180));
                        lock (houghLock) {
                            houghArr[unghi][r + initialR] += 1;
                            if (houghArr[unghi][r + initialR] > globalMax)
                            {
                                globalMax = houghArr[unghi][r + initialR];
                            }
                        }
                    }
                }
                done++;
                col++;
            }
        }

        private void executeTrans()
        {
            int initialR = (int)Math.Sqrt(image.width * image.width + image.height * image.height);
            int elemPerThr = (image.height * image.width) / Program.nr_threads;
            int ord = 1;
            List<Task<bool>> tasks = new List<Task<bool>>();

            for (int task = 0; task < Program.nr_threads; task++)
            {
                if (task + 1 == Program.nr_threads)
                {
                    elemPerThr += (image.height * image.width) % Program.nr_threads;
                    Pair<int, int> start = getXY(ord, image.width);
                    int finalElemPerThr = elemPerThr;
                    tasks.Add(Task.Factory.StartNew(()=> {
                        executeTransTask(start, finalElemPerThr, filteredImg);
                        return true;
                    }));
                }
            }

            Task.WaitAll(tasks.ToArray());

            houghNoTh = houghArr;
            for (int x = 0; x < 180; x++)
            {
                for (int y = 0; y < 2 * initialR; y++)
                {
                    if (houghArr[x][y] < THRESHOLD * globalMax)
                        houghArr[x][y] = 0;
                }
            }

            int neighbourhoodSize = 4;
            for (int t = 0; t < 180; t++)
            {
            loop:
                for (int r = neighbourhoodSize; r < 2 * initialR; r++)
                {
                    if(houghArr[t][r] > THRESHOLD * globalMax)
                    {
                        int peak = houghArr[t][r];

                        for (int dx = -neighbourhoodSize; dx <= neighbourhoodSize; dx++)
                        {
                            for (int dy = -neighbourhoodSize; dy <= neighbourhoodSize; dy++)
                            {
                                int dt = t + dx;
                                int dr = r + dy;
                                if (dt < 0) dt += 180;
                                else if (dt >= 180) dt -= 180;
                                if (houghArr[dt][dr] > peak)
                                {
                                    houghArr[t][r] = 0;
                                    goto loop;
                                }
                            }
                        }
                    }
                }
            }
        }

        private Pair<int, int> getXY(int row, int col)
        {
            if (row % col == 0)
            {
                return new Pair<int, int>(row / col - 1, col - 1);
            }
            return new Pair<int, int>(row / col, row % col - 1);
        }

        public List<Pair<int,int>> getPoints()
        {
            List<Pair<int, int>> points = new List<Pair<int, int>>();
            int ord = 1;
            int elemPerThr = (180 * houghArr[0].Length) / Program.nr_threads;
            List<Task<List<Pair<int, int>>>> tasks = new List<Task<List<Pair<int, int>>>>();
            for(int task = 0; task < Program.nr_threads; task++)
            {
                if (task + 1 == Program.nr_threads)
                {
                    elemPerThr += (180 * houghArr[0].Length) % Program.nr_threads;
                }
                Pair<int, int> XY = getXY(ord, houghArr[0].Length);
                int finalElemPerThr = elemPerThr;
                tasks.Add(Task.Factory.StartNew(() => {
                    return createPoints(XY, finalElemPerThr, houghArr);
                }));
                ord += elemPerThr;
            }
            Task.WaitAll(tasks.ToArray());

            for (int i = 0; i < tasks.Capacity; i++)
            {
                points.AddRange(tasks[0].Result);
            }

            return points;
        }
        
        public List<Pair<int,int>> createPoints(Pair<int,int> XY, int elems, int[][] img)
        {
            List<Pair<int, int>> points = new List<Pair<int, int>>();

            int row = XY.First;
            int col = XY.Second;
            int done = 0;
            int initialR = (int)Math.Sqrt(image.width * image.width + image.height * image.height);

            while (done < elems && row < img.Length)
            {
                if(col == img[0].Length)
                {
                    col = 0;
                    row++;
                    if (row == img.Length) break;
                }
                if (img[row][col] != 0)
                {
                    for(int i = 0; i < filteredImg.Length; i++)
                    {
                        for(int j = 0; j < filteredImg[0].Length; j++)
                        {
                            if(j == (int)((-Math.Cos((Math.PI * row) / 180) / Math.Sin((Math.PI * row) / 180)) * i + (col - initialR) / Math.Sin((Math.PI * row) / 180)))
                            {
                                points.Add(new Pair<int, int>(i, j));
                            }
                        }
                    }
                }
                done++;
                col++;
            }

            return points;
        }

        public  void putLines()
        {
            List<Pair<int, int>> points = getPoints();
            for (int i = 0; i < points.Capacity; i++)
            {
                image.setPoint(points[i], 124, 252, 0);
            }
        }

        private readonly object houghLock = new object();
        private int[][] houghArr;
        private int[][] houghNoTh;
        private int[][] filteredImg;
        private static double THRESHOLD = 0.3;
        private int globalMax = -1;
        private Image image { get; set; }
    }
}

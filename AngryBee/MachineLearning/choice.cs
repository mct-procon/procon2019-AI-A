using System;
using System.Collections.Generic;
using System.Text;
using MCTProcon29Protocol;
using AngryBee.Boards;
using System.IO;

namespace AngryBee.MachineLearning
{
    public class choice
    {
        public const int SampleNum = 50;
        public double[,] DNAs = new double[SampleNum, 5];
        private double[,] NextDNAs = new double[SampleNum, 5];
        public int[] Probability = new int[SampleNum];
        public int tested = 0;
        public const int ButtleTimes = 2;
        public int ButtleNum = ButtleTimes;
        public int[] WinPoint = new int[SampleNum];

        public void init()
        {
            for(int i = 0; i < SampleNum; i++)
            {
                Probability[i] = 100;
                WinPoint[i] = 1;
            }
        }

        public void ReadFile()
        {
            int i = 0;
            string line;
            System.IO.StreamReader file = new System.IO.StreamReader(@"C:\Users\samin\OneDrive\ドキュメント\procon2019\machine_learning\test.txt");
            while((line = file.ReadLine()) != null)
            {
                string[] str = line.Split(' ');
                for(int j = 0; j < 5; j++)
                {
                    DNAs[i, j] = double.Parse(str[j]);
                }
                i++;
            }
            file.Close();
        }

        private static int lower_bound<T>(T[] arr, int start, int end, T value, IComparer<T> comparer)
        {
            int low = start;
            int high = end;
            int mid;
            while (low < high)
            {
                mid = ((high - low) >> 1) + low;
                if (comparer.Compare(arr[mid], value) < 0)
                    low = mid + 1;
                else
                    high = mid;
            }
            return low;
        }
        //引数省略のオーバーロード
        private static int lower_bound<T>(T[] arr, T value) where T : IComparable
        {
            return lower_bound(arr, 0, arr.Length, value, Comparer<T>.Default);
        }

        public void NextGenerate()
        {
            Random rand = new System.Random();
            int[] sum = new int[SampleNum + 1];

            for(int i = 0; i < SampleNum; i++)
            {
                sum[i + 1] = sum[i] + Probability[i];
            }

            for(int i = 0; i < SampleNum; i++)
            {
                int r = rand.Next(50);

                //突然変異
                if(i == SampleNum - 1 || r == 0)
                {
                    int choice = rand.Next(sum[SampleNum]) + 1;
                    int locate = lower_bound(sum, choice) - 1;
                    int change_loc = rand.Next(5);
                    int change_num = rand.Next(2000);
                    
                    for(int j = 0; j < 5; j++)
                    {
                        NextDNAs[i, j] = DNAs[locate, j];
                    }
                    NextDNAs[i, change_loc] = change_num - 1000;
                }
                //交叉
                else if(r < 30)
                {
                    int choice = rand.Next(sum[SampleNum]) + 1;
                    int locate = lower_bound(sum, choice) - 1;
                    choice = rand.Next(sum[SampleNum]) + 1;
                    int locate2 = lower_bound(sum, choice) - 1;

                    for(int j = 0; j < 5; j++)
                    {
                        //そのまま
                        if(rand.Next(2) == 0)
                        {
                            NextDNAs[i, j] = DNAs[locate, j];
                            NextDNAs[i + 1, j] = DNAs[locate2, j];
                        }
                        //入れ替え
                        else
                        {
                            NextDNAs[i, j] = DNAs[locate2, j];
                            NextDNAs[i + 1, j] = DNAs[locate, j];
                        }
                    }
                    i++;
                }
                //そのまま
                else
                {
                    int choice = rand.Next(sum[SampleNum]) + 1;
                    int locate = lower_bound(sum, choice) - 1;

                    for(int j = 0; j < 5; j++)
                    {
                        NextDNAs[i, j] = DNAs[locate, j];
                    }
                }
            }
        }

        public void WriteFile()
        {
            NextGenerate();

            System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\samin\OneDrive\ドキュメント\procon2019\machine_learning\test.txt", false);
            file.Close();
            file = new System.IO.StreamWriter(@"C:\Users\samin\OneDrive\ドキュメント\procon2019\machine_learning\test.txt", true);
            for (int i = 0; i < SampleNum; i++)
            {
                for(int j = 0; j < 5; j++)
                {
                    file.Write("{0} ", NextDNAs[i,j]);
                }
                file.Write("\n");
            }
            file.Close();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using MCTProcon29Protocol;
using AngryBee.Boards;

namespace AngryBee.PointEvaluator
{
    class Distance : Base
    {
        public static int Avg = 10;
        public override int Calculate(sbyte[,] ScoreBoard, in ColoredBoardSmallBigger MePainted, int Turn, Player Me, Player Enemy, double[,] DNAs, int tested)
        {

            ColoredBoardSmallBigger checker = new ColoredBoardSmallBigger(MePainted.Width, MePainted.Height);   //!checker == 領域
            double result = 0;
            uint width = MePainted.Width;
            uint height = MePainted.Height;
            for (uint x = 0; x < width; ++x)
                for (uint y = 0; y < height; ++y)
                {
                    if (MePainted[x, y])
                    {
                        if (ScoreBoard[x, y] <= Avg) result += ScoreBoard[x, y];
                        else result += ScoreBoard[x, y];
                        checker[x, y] = true;
                    }
                }

            BadSpaceFill(ref checker, width, height);

            double NotChecker = 0.0;
            for (uint x = 0; x < width; ++x)
                for (uint y = 0; y < height; ++y)
                    if (!checker[x, y])
                        NotChecker += Math.Abs(ScoreBoard[x, y]);
            result += NotChecker * Turn * DNAs[tested, 0] / 500.0;

            //差分計算
            double DistanceScore = 0;
            for (uint x = 0; x < width; ++x)
                for(uint y = 0; y < height; ++y)
                {
                    if (MePainted[x, y] || !checker[x, y]) continue;
                    if (ScoreBoard[x, y] <= Avg) continue;
                    double distMe1 = Math.Max(Math.Abs(Me.Agent1.X - x), Math.Abs(Me.Agent1.Y - y));
                    double distMe2 = Math.Max(Math.Abs(Me.Agent2.X - x), Math.Abs(Me.Agent2.Y - y));
                    
                    DistanceScore += (ScoreBoard[x, y] / Math.Min(distMe1, distMe2));
                }
            //Console.WriteLine("result:{0}, DistanceScore:{1}", result, DistanceScore);

            return (int)(result * DNAs[tested, 1] + DistanceScore);
        }

        public int Calculate2(sbyte[,] ScoreBoard, in ColoredBoardSmallBigger MePainted, int Turn, Player Me, Player Enemy)
        {
            ColoredBoardSmallBigger checker = new ColoredBoardSmallBigger(MePainted.Width, MePainted.Height);   //!checker == 領域
            double result = 0;
            uint width = MePainted.Width;
            uint height = MePainted.Height;
            for (uint x = 0; x < width; ++x)
                for (uint y = 0; y < height; ++y)
                {
                    if (MePainted[x, y])
                    {
                        result += ScoreBoard[x, y];
                        checker[x, y] = true;
                    }
                }

            BadSpaceFill(ref checker, width, height);

            double NotChecker = 0.0;
            for (uint x = 0; x < width; ++x)
                for (uint y = 0; y < height; ++y)
                    if (!checker[x, y])
                        NotChecker += Math.Abs(ScoreBoard[x, y]);
            result += NotChecker * Turn / 50;
            
            //差分計算
            double DistanceScore = 0;
            for (uint x = 0; x < width; ++x)
                for (uint y = 0; y < height; ++y)
                {
                    if (MePainted[x, y] || !checker[x, y]) continue;
                    if (ScoreBoard[x, y] <= Avg) continue;
                    double distMe1 = Math.Max(Math.Abs(Me.Agent1.X - x), Math.Abs(Me.Agent1.Y - y));
                    double distMe2 = Math.Max(Math.Abs(Me.Agent2.X - x), Math.Abs(Me.Agent2.Y - y));

                    DistanceScore += (ScoreBoard[x, y] / Math.Min(distMe1, distMe2));
                }

            Console.WriteLine("result:{0}, DistanceScore:{1}", result, DistanceScore);

            return (int)(result * 2000 + DistanceScore);
        }

        //囲いを見つける
        public unsafe void BadSpaceFill(ref ColoredBoardSmallBigger Checker, uint width, uint height)
        {
            unchecked
            {
                Point* myStack = stackalloc Point[12 * 12];

                Point point;
                uint x, y, searchTo = 0, myStackSize = 0;

                searchTo = height - 1;
                for (x = 0; x < width; x++)
                {
                    if (!Checker[x, 0])
                    {
                        myStack[myStackSize++] = new Point(x, 0);
                        Checker[x, 0] = true;
                    }
                    if (!Checker[x, searchTo])
                    {
                        myStack[myStackSize++] = new Point(x, searchTo);
                        Checker[x, searchTo] = true;
                    }
                }

                searchTo = width - 1;
                for (y = 0; y < height; y++)
                {
                    if (!Checker[0, y])
                    {
                        myStack[myStackSize++] = new Point(0, y);
                        Checker[0, y] = true;
                    }
                    if (!Checker[searchTo, y])
                    {
                        myStack[myStackSize++] = new Point(searchTo, y);
                        Checker[searchTo, y] = true;
                    }
                }

                while (myStackSize > 0)
                {
                    point = myStack[--myStackSize];
                    x = point.X;
                    y = point.Y;

                    //左方向
                    searchTo = x - 1;
                    if (searchTo < width && !Checker[searchTo, y])
                    {
                        myStack[myStackSize++] = new Point(searchTo, y);
                        Checker[searchTo, y] = true;
                    }

                    //下方向
                    searchTo = y + 1;
                    if (searchTo < height && !Checker[x, searchTo])
                    {
                        myStack[myStackSize++] = new Point(x, searchTo);
                        Checker[x, searchTo] = true;
                    }

                    //右方向
                    searchTo = x + 1;
                    if (searchTo < width && !Checker[searchTo, y])
                    {
                        myStack[myStackSize++] = new Point(searchTo, y);
                        Checker[searchTo, y] = true;
                    }

                    //上方向
                    searchTo = y - 1;
                    if (searchTo < height && !Checker[x, searchTo])
                    {
                        myStack[myStackSize++] = new Point(x, searchTo);
                        Checker[x, searchTo] = true;
                    }
                }
            }
        }
    }
}

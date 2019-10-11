﻿using System;
using System.Collections.Generic;
using System.Text;
using MCTProcon30Protocol;
using AngryBee.Boards;

namespace AngryBee.PointEvaluator
{
    //塗られているマスの重心を用いて、分散を計算する。
    class AgentDistance : Base
    {
        const float DistanceRate = 30.0f;
        const float SurroundRate = 0.8f;

        private struct PointFloat
        {
            public float x;
            public float y;
            public PointFloat(float x, float y)
            {
                this.x = x;
                this.y = y;
            }
        }
        public override int Calculate(sbyte[,] ScoreBoard, in ColoredBoardNormalSmaller Painted, int Turn, Unsafe8Array<Point> Me, Unsafe8Array<Point> Enemy, int AgentsCount)
        {
            ColoredBoardNormalSmaller checker = new ColoredBoardNormalSmaller(Painted.Width, Painted.Height);
            int result = 0;
            int checkedCount = 0;
            byte width = (byte)Painted.Width;
            byte height = (byte)Painted.Height;
            for (uint x = 0; x < width; ++x)
                for (uint y = 0; y < height; ++y)
                {
                    if (Painted[x, y])
                    {
                        result += ScoreBoard[x, y];
                        checker[x, y] = true;
                        checkedCount++;
                    }
                }

            BadSpaceFill(ref checker, width, height);

            for (uint x = 0; x < width; ++x)
                for (uint y = 0; y < height; ++y)
                    if (!checker[x, y])
                        result = result + (int)(Math.Abs(ScoreBoard[x, y]) * SurroundRate);

            double dist = 0;
            for(int i = 0; i < AgentsCount; ++i)
            {
                int mindist = 100;
                for(int j = i+1; j < AgentsCount; ++j)
                {
                    int tmp = Math.Max(Math.Abs((Me[i].X - Me[j].X)), Math.Abs((Me[i].Y - Me[j].Y)));
                    mindist = Math.Min(tmp, mindist);
                }
                dist += mindist;
            }

            return result + checkedCount + (int)(dist * DistanceRate) / AgentsCount;
        }

        public unsafe void BadSpaceFill(ref ColoredBoardNormalSmaller Checker, byte width, byte height)
        {
            unchecked
            {
                Point* myStack = stackalloc Point[20 * 20];

                Point point;
                byte x, y, searchTo = 0, myStackSize = 0;

                searchTo = (byte)(height - 1);
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

                searchTo = (byte)(width - 1);
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
                    searchTo = (byte)(x - 1);
                    if (searchTo < width && !Checker[searchTo, y])
                    {
                        myStack[myStackSize++] = new Point(searchTo, y);
                        Checker[searchTo, y] = true;
                    }

                    //下方向
                    searchTo = (byte)(y + 1);
                    if (searchTo < height && !Checker[x, searchTo])
                    {
                        myStack[myStackSize++] = new Point(x, searchTo);
                        Checker[x, searchTo] = true;
                    }

                    //右方向
                    searchTo = (byte)(x + 1);
                    if (searchTo < width && !Checker[searchTo, y])
                    {
                        myStack[myStackSize++] = new Point(searchTo, y);
                        Checker[searchTo, y] = true;
                    }

                    //上方向
                    searchTo = (byte)(y - 1);
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

﻿using System;
using System.Collections.Generic;
using System.Text;
using AngryBee.Boards;
using System.Runtime.Intrinsics.X86;
using MCTProcon30Protocol;

namespace AngryBee.PointEvaluator
{
	public class AreaCount : Base
	{
		public override int Calculate(sbyte[,] ScoreBoard, in ColoredBoardNormalSmaller Painted, int Turn, Player Me, Player Enemy)
		{
			ColoredBoardNormalSmaller checker = new ColoredBoardNormalSmaller(Painted.Width, Painted.Height);
			int result = 0;
			uint width = Painted.Width;
			uint height = Painted.Height;
			for (uint x = 0; x < width; ++x)
				for (uint y = 0; y < height; ++y)
				{
					if (Painted[x, y])
					{
						result += ScoreBoard[x, y];
						checker[x, y] = true;
					}
				}

			BadSpaceFill(ref checker, width, height);

			for (uint x = 0; x < width; ++x)
				for (uint y = 0; y < height; ++y)
					if (!checker[x, y])
						result += Math.Abs(ScoreBoard[x, y]);

			//囲った領域の個数を数える(BadSpaceFill(外側の塗りつぶし)が終わったあとの2次元配列checkerを渡す）
			int count = CalcAreaCount(checker);

			return result + count * 4;
		}

		//uint[] myStack = new uint[1024];	//x, yの順で入れる. y, xの順で取り出す. width * height以上のサイズにする.
		public unsafe void BadSpaceFill(ref ColoredBoardNormalSmaller Checker, uint width, uint height)
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

		[Obsolete("too slow")]
		public int Calculate(sbyte[,] ScoreBoard, in ColoredBoardNormalSmaller Painted, ref ColoredBoardNormalSmaller Checker, uint x, uint y, uint width, uint height)
		{
			unchecked
			{
				if (Checker[x, y]) return int.MinValue;

				Checker[x, y] = true;

				uint Right = x + 1u;
				uint Left = x - 1u;

				if (Right >= width || Left >= width)
					return int.MinValue;

				uint Bottom = y + 1u;
				uint Top = y - 1u;

				if (Top >= height || Bottom >= height)
					return int.MinValue;

				int result = 0;

				if (!Painted[x, Top])
				{
					int cache = Calculate(ScoreBoard, Painted, ref Checker, x, Top, width, height);
					if (cache == int.MinValue)
						return int.MinValue;
					else
						result += cache;
				}

				if (!Painted[x, Bottom])
				{
					int cache = Calculate(ScoreBoard, Painted, ref Checker, x, Bottom, width, height);
					if (cache == int.MinValue)
						return int.MinValue;
					else
						result += cache;
				}

				if (!Painted[Left, y])
				{
					int cache = Calculate(ScoreBoard, Painted, ref Checker, Left, y, width, height);
					if (cache == int.MinValue)
						return int.MinValue;
					else
						result += cache;
				}

				if (!Painted[Right, y])
				{
					int cache = Calculate(ScoreBoard, Painted, ref Checker, Right, y, width, height);
					if (cache == int.MinValue)
						return int.MinValue;
					else
						result += cache;
				}

				result += Math.Abs(ScoreBoard[x, y]);

				return result;
			}
		}

		private int CalcAreaCount(ColoredBoardNormalSmaller Checker)
		{
			uint i, j;
			uint height = Checker.Height;
			uint width = Checker.Width;
			int cnt = 0;

			//ラベリング処理
			for (i = 0; i < height; i++)
			{
				for (j = 0; j < width; j++)
				{
					if (Checker[new Point(j, i)]) continue;

					unsafe
					{
						Point* myStack = stackalloc Point[12 * 12];
						Point point;
						uint x, y, searchTo = 0, myStackSize = 0;

						myStack[myStackSize++] = new Point(j, i);
						Checker[new Point(j, i)] = true;

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
					cnt++;
				}
			}
			return cnt;
		}
	}
}

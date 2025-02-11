﻿using System;
using System.Collections.Generic;
using System.Text;
using MCTProcon30Protocol.Methods;
using MCTProcon30Protocol;
using AngryBee.Boards;

namespace AngryBee.Search
{
    public class SearchState
    {
		public ColoredBoardNormalSmaller MeBoard;
		public ColoredBoardNormalSmaller EnemyBoard;
		public Unsafe8Array<Point> Me;
		public Unsafe8Array<Point> Enemy;

        private SearchState() { }

        public SearchState(in ColoredBoardNormalSmaller MeBoard, in ColoredBoardNormalSmaller EnemyBoard, in Unsafe8Array<Point> Me, in Unsafe8Array<Point> Enemy)
		{
			this.MeBoard = MeBoard;
			this.EnemyBoard = EnemyBoard;
			this.Me = Me;
			this.Enemy = Enemy;
        }

        //全ての指示可能な方向を求めて, (way1[i], way2[i])に入れる。(Meが動くとする)
        public Ways MakeMoves(int AgentsCount, sbyte[,] ScoreBoard) => new Ways(this, AgentsCount, ScoreBoard);

        public SearchState GetNextState(int AgentsCount, Unsafe8Array<Way> ways)
        {
            var ss = new SearchState();
            ss.MeBoard = this.MeBoard;
            ss.EnemyBoard = this.EnemyBoard;
            ss.Me = this.Me;
            ss.Enemy = this.Enemy;
            for (int i = 0; i < AgentsCount; ++i)
            {
                if (ways[i].Direction == new VelocityPoint()) continue;
                var l = ways[i].Locate;
                if (ss.EnemyBoard[l]) // タイル除去
                    ss.EnemyBoard[l] = false;
                else
                {
                    ss.MeBoard[l] = true;
                    ss.Me[i] = l;
                }
            }
            return ss;
        }
        public SearchState ChangeTurn()
        {
            var ss = new SearchState();
            ss.MeBoard = this.EnemyBoard;
            ss.EnemyBoard = this.MeBoard;
            ss.Me = this.Enemy;
            ss.Enemy = this.Me;

            return ss;
        }

        //タイルスコア最大の手を返す（MakeMoves -> SortMovesで0番目に来る手を返す）探索延長を高速化するために使用。
        public Unsafe8Array<VelocityPoint> MakeGreedyMove(sbyte[,] ScoreBoard, VelocityPoint[] WayEnumrator, int AgentsCount)
        {
            int i, j;
            int[] Score = { -100, -100, -100, -100, -100, -100, -100, -100 };
            Unsafe8Array<VelocityPoint> ways = new Unsafe8Array<VelocityPoint>();

            //自分2人が被るかのチェックをしないで、最大の組み合わせを探す
            for (i = 0; i < AgentsCount; ++i)
            {
                for (j = 0; j < WayEnumrator.Length; j++)
                {
                    Point next = Me[i] + WayEnumrator[j];
                    if (next.X >= MeBoard.Width || next.Y >= MeBoard.Height) continue;
                    bool b = false;
                    for(int k = 0; k < AgentsCount; ++k)
                    {
                        if (next == Enemy[k])
                        {
                            b = true;
                            break;
                        }
                    }
                    if (b) continue;
                    int score = (MeBoard[next] == true) ? 0 : ScoreBoard[next.X, next.Y];
                    if (Score[i] < score) { Score[i] = score; ways[i] = WayEnumrator[j]; }
                }
            }


            for(i = 0; i < AgentsCount; ++i)
            {
                if (Score[i] <= -100) break;
                for(j = i+1; j < AgentsCount; ++j)
                {
                    if (Me[i] + ways[i] == Me[j] + ways[j]) break;
                }
                if (j != AgentsCount) break;
            }
            if (i == AgentsCount) return ways;

            //真面目に探索する
            int maxScore = -100;
            for (i = 0; i < (WayEnumrator.Length << (AgentsCount * 3)); ++i)
            {
                Unsafe8Array<Point> next = new Unsafe8Array<Point>();
                int score = 0;
                for (j = 0; j < AgentsCount; ++j)
                {
                    int way = (i >> (j * 3)) % WayEnumrator.Length;
                    next[j] = Me[j] + WayEnumrator[way];
                    if (next[j].X >= MeBoard.Width || next[j].Y >= MeBoard.Height) continue;
                    bool b = false;
                    for(int k = 0; k < AgentsCount; ++k)
                    {
                        if (Enemy[k] == next[j])
                        {
                            b = true;
                            break;
                        }
                    }
                    if (b) continue;
                    score += (MeBoard[next[j]] == true) ? 0 : ScoreBoard[next[j].X, next[j].Y];
                }
                if(maxScore < score)
                {
                    maxScore = score;
                    for (j = 0; j < AgentsCount; ++j)
                    {
                        int way = (i >> (j * 3)) % WayEnumrator.Length;
                        ways[j] = WayEnumrator[way];
                    }
                }
            }
            return ways;
        }

        //Search Stateを更新する (MeとEnemyの入れ替えも忘れずに）（呼び出し時の前提：Validな動きである）
        public void Move(Unsafe8Array<VelocityPoint> way, int AgentsCount)
        {
            Unsafe8Array<Point> next = new Unsafe8Array<Point>();
            for(int i = 0; i < AgentsCount; ++i)
            {
                next[i] = Me[i] + way[i];
            }


            for(int i = 0; i < AgentsCount; ++i)
            {
                if (EnemyBoard[next[i]])  //タイル除去
                {
                    EnemyBoard[next[i]] = false;
                }
                else  //移動
                {
                    MeBoard[next[i]] = true;
                    Me.Agent1 = next[i];
                }
            }

            //MeとEnemyの入れ替え（手番の入れ替え）
            Swap(ref MeBoard, ref EnemyBoard);
            Swap(ref Me, ref Enemy);
        }

        //内容が等しいか？
        public bool Equals(SearchState st, int agentCount)
		{
#if DEBUG
            if (MeBoard.Height != st.MeBoard.Height) return false;
			if (MeBoard.Width != st.MeBoard.Width) return false;
#endif
            for (byte i = 0; i < MeBoard.Height; i++) for (byte j = 0; j < MeBoard.Width; j++) if (MeBoard[new Point(j, i)] != st.MeBoard[new Point(j, i)]) return false;

#if DEBUG
            if (EnemyBoard.Height != st.EnemyBoard.Height) return false;
			if (EnemyBoard.Width != st.EnemyBoard.Width) return false;
#endif
            for (uint i = 0; i < EnemyBoard.Height; i++)
				for (uint j = 0; j < EnemyBoard.Width; j++)
					if (EnemyBoard[new Point((byte)j, (byte)i)] != st.EnemyBoard[new Point((byte)j, (byte)i)]) return false;

			if (!Unsafe8Array<Point>.Equals(Me, st.Me, agentCount)) return false;
			if (!Unsafe8Array<Point>.Equals(Enemy, st.Enemy, agentCount)) return false;
            return true;
		}

        //Swap関数
        private static void Swap<T>(ref T a, ref T b)
        {
            var t = a;
            a = b;
            b = t;
        }
    }
}

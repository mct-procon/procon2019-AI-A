﻿using AngryBee.Boards;
using System;
using System.Collections.Generic;
using System.Text;
using MCTProcon29Protocol.Methods;
using MCTProcon29Protocol;
using AngryBee.Search;

namespace AngryBee.AI
{
    public class TestAI : MCTProcon29Protocol.AIFramework.AIBase
    {
        PointEvaluator.Base PointEvaluator_AreaCount = new PointEvaluator.AreaCount();
        PointEvaluator.Base PointEvaluator_Normal = new PointEvaluator.Normal();
        VelocityPoint[] WayEnumerator = { (0, -1), (1, -1), (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1) };
        ObjectPool<Ways> WaysPool = new ObjectPool<Ways>();

        private struct DP
        {
            public int Score;
            public VelocityPoint Agent1Way;
            public VelocityPoint Agent2Way;

            public void UpdateScore(int score, VelocityPoint a1, VelocityPoint a2)
            {
                if (Score < score)
                {
                    Agent1Way = a1;
                    Agent2Way = a2;
                }
            }
        }
        private DP[] dp = new DP[50];

        //public int ends = 0;

        public int StartDepth { get; set; } = 1;
        public int GreedyMaxDepth { get; } = 0;      //評価関数を呼び出す前に, 最大で深さいくつ分まで貪欲するか？
		public List<SearchState> historyStates { get; } = null;
		public List<Decided> historyDecides { get; } = null;
		public Decided ngMove = null;
		public int KyogoTurn { get; } = 100;

		public TestAI(int startDepth = 1, int greedyMaxDepth = 0, int KyogoTurn = 100)
        {
            for (int i = 0; i < 50; ++i)
                dp[i] = new DP();
            StartDepth = startDepth;
            GreedyMaxDepth = greedyMaxDepth;
			historyStates = new List<SearchState>();
			historyDecides = new List<Decided>();
			this.KyogoTurn = KyogoTurn;
		}

        //1ターン = 深さ2
        protected override void Solve()
        {
			int i;
            for (i = 0; i < 50; ++i)
                dp[i].Score = int.MinValue;
            int deepness = StartDepth;
            int maxDepth = (TurnCount - CurrentTurn) * 2;
            PointEvaluator.Base evaluator = (TurnCount / 3 * 2) < CurrentTurn ? PointEvaluator_Normal : PointEvaluator_AreaCount;
            SearchState state = new SearchState(MyBoard, EnemyBoard, new Player(MyAgent1, MyAgent2), new Player(EnemyAgent1, EnemyAgent2), WaysPool);

			//競合判定
			int K = KyogoTurn;  //Kは1以上の整数
			ngMove = null;
			for (i = 0; i < K; i++)
			{
				if (historyStates.Count < K) { break; }
				int id = historyStates.Count - 1 - i;
				if (id < 0 || state.Equals(historyStates[id]) == false) break;
				if (i + 1 < K && id > 0 && historyDecides[id].Equals(historyDecides[id - 1]) == false) break;
			}
			if (i == K)
			{
				int score = PointEvaluator_Normal.Calculate(ScoreBoard, state.MeBoard, 0, state.Me, state.Enemy) - PointEvaluator_Normal.Calculate(ScoreBoard, state.EnemyBoard, 0, state.Me, state.Enemy);
				if (score <= 0)
				{
					ngMove = historyDecides[historyDecides.Count - 1];
					Log("[SOLVER] conclusion!, you cannot move to ", ngMove);
				}
			}

			//反復深化
            for (; deepness <= maxDepth; deepness++)
            {
                NegaMax(deepness, state, int.MinValue + 1, int.MaxValue, 0, evaluator, Math.Min(maxDepth - deepness, GreedyMaxDepth));
                if (CancellationToken.IsCancellationRequested == false)
                    SolverResult = new Decided(dp[0].Agent1Way, dp[0].Agent2Way);
                else
                    break;
                Log("[SOLVER] deepness = {0}", deepness);
            }

			//履歴の更新
			historyStates.Add(state);
			historyDecides.Add(SolverResult);
        }

        //Meが動くとする。「Meのスコア - Enemyのスコア」の最大値を返す。
        private int NegaMax(int deepness, SearchState state, int alpha, int beta, int count, PointEvaluator.Base evaluator, int greedyDepth)
        {
            if (deepness == 0)
            {
				//深さgreedyDepth分だけ貪欲をしてから、評価関数を呼び出す
				for (int i = 0; i < greedyDepth; i++)
				{
					Ways moves = state.MakeMoves(WayEnumerator);
					SortMoves(ScoreBoard, state, moves, dp.Length - 1);
					state.Move(moves[0].Agent1Way, moves[0].Agent2Way);
				}
                int eval = evaluator.Calculate(ScoreBoard, state.MeBoard, 0, state.Me, state.Enemy) - evaluator.Calculate(ScoreBoard, state.EnemyBoard, 0, state.Enemy, state.Me);
				if (greedyDepth % 2 == 1) { return -eval; }
				return eval;
			}

            Ways ways = state.MakeMoves(WayEnumerator);
            SortMoves(ScoreBoard, state, ways, count);

            for (int i = 0; i < ways.Count; i++)
            {
                if (CancellationToken.IsCancellationRequested == true) { return alpha; }    //何を返しても良いのでとにかく返す
				if (count == 0 && ngMove != null && new Decided(ways[i].Agent1Way, ways[i].Agent2Way).Equals(ngMove) == true) { continue; }	//競合手は指さない

				SearchState backup = state;
                state.Move(ways[i].Agent1Way, ways[i].Agent2Way);
                int res = -NegaMax(deepness - 1, state, -beta, -alpha, count + 1, evaluator, greedyDepth);
                if (alpha < res)
                {
                    alpha = res;
					dp[count].UpdateScore(alpha, ways[i].Agent1Way, ways[i].Agent2Way);
                    if (alpha >= beta) return beta; //βcut
                }
                state = backup;
            }
            ways.Erase();
            WaysPool.Return(ways);
            return alpha;
        }

        //遷移順を決める.  「この関数においては」MeBoard…手番プレイヤのボード, Me…手番プレイヤ、とします。
        //引数: stateは手番プレイヤが手を打つ前の探索状態、(way1[i], way2[i])はi番目の合法手（移動量）です。
        //以下のルールで優先順を決めます.
        //ルール1. Killer手（優先したい手）があれば、それを優先する
        //ルール2. 次のmoveで得られる「タイルポイント」の合計値が大きい移動（の組み合わせ）を優先する。
        //ルール2では, タイル除去によっても「タイルポイント」が得られるとして計算する。
        private void SortMoves(sbyte[,] ScoreBoard, SearchState state, Ways way, int deep)
        {
            var Killer = dp[deep].Score == int.MinValue ? new Player(new Point(114, 514), new Point(114, 514)) : new Player(state.Me.Agent1 + dp[deep].Agent1Way, state.Me.Agent2 + dp[deep].Agent2Way);

            for (int i = 0; i < way.Count; i++)
            {
                int score = 0;
                Point next1 = state.Me.Agent1 + way[i].Agent1Way;
                Point next2 = state.Me.Agent2 + way[i].Agent2Way;

                if (Killer.Agent1 == next1 && Killer.Agent2 == next2) { score = 100; }

                if (state.EnemyBoard[next1]) { score += ScoreBoard[next1.X, next1.Y]; }     //タイル除去によって有利になる
                else if (!state.MeBoard[next1]) { score += ScoreBoard[next1.X, next1.Y]; }  //移動でMeの陣地が増えて有利になる
                if (state.EnemyBoard[next2]) { score += ScoreBoard[next2.X, next2.Y]; }
                else if (!state.MeBoard[next2]) { score += ScoreBoard[next2.X, next2.Y]; }
                way[i].Point = score;
            }
            way.Sort();
        }

        protected override int CalculateTimerMiliSconds(int miliseconds)
        {
            return miliseconds - 1000;
        }

        protected override void EndGame(GameEnd end)
        {
        }
    }
}

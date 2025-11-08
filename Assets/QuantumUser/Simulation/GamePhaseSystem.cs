namespace Quantum
{
    using Photon.Deterministic;
    using System;

    /// <summary>
    /// 게임 페이즈 관리 시스템
    /// 0=Lobby, 1=CharSelect, 2=OrderSetup, 3=Battle, 4=Result
    /// </summary>
    public unsafe class GamePhaseSystem : SystemMainThread
    {
        public enum Phase
        {
            Lobby = 0,
            CharacterSelect = 1,
            OrderSetup = 2,
            Battle = 3,
            Result = 4
        }

        public override void OnInit(Frame f)
        {
            // 게임 시작 시 초기화
            var globals = f.Globals;
            globals->CurrentPhase = (int)Phase.Lobby;
            globals->SelectTurn = 0;
            globals->SelectTimer = FP._0;
            globals->CurrentRound = 0;
            globals->PlayerAScore = 0;
            globals->PlayerBScore = 0;
        }

        public override void Update(Frame f)
        {
            var globals = f.Globals;
            var phase = (Phase)globals->CurrentPhase;

            switch (phase)
            {
                case Phase.Lobby:
                    UpdateLobby(f, globals);
                    break;

                case Phase.CharacterSelect:
                    UpdateCharacterSelect(f, globals);
                    break;

                case Phase.OrderSetup:
                    UpdateOrderSetup(f, globals);
                    break;

                case Phase.Battle:
                    UpdateBattle(f, globals);
                    break;

                case Phase.Result:
                    UpdateResult(f, globals);
                    break;
            }
        }

        void UpdateLobby(Frame f, _globals_* globals)
        {
            // 플레이어 2명이 접속하면 캐릭터 선택 페이즈로 전환
            int playerCount = f.PlayerCount;
            if (playerCount >= 2)
            {
                ChangePhase(f, globals, Phase.CharacterSelect);
            }
        }

        void UpdateCharacterSelect(Frame f, _globals_* globals)
        {
            // CharacterSelectSystem에서 처리
            // 6턴 완료 시 OrderSetup으로 전환
        }

        void UpdateOrderSetup(Frame f, _globals_* globals)
        {
            // OrderSetupSystem에서 처리
            // 양쪽 플레이어가 순서 제출하면 Battle로 전환
        }

        void UpdateBattle(Frame f, _globals_* globals)
        {
            // BattleSystem에서 처리
            // 라운드 종료 시 다음 라운드 또는 Result로 전환
        }

        void UpdateResult(Frame f, _globals_* globals)
        {
            // 결과 화면 표시
            // 일정 시간 후 다시 CharacterSelect로 (재대결)
        }

        public static void ChangePhase(Frame f, _globals_* globals, Phase newPhase)
        {
            var oldPhase = globals->CurrentPhase;
            globals->CurrentPhase = (int)newPhase;

            Log.Info($"🎮 Phase Changed: {(Phase)oldPhase} → {newPhase}");
            f.Signals.OnPhaseChanged((int)newPhase);

            // 페이즈 전환 시 초기화 작업
            switch (newPhase)
            {
                case Phase.CharacterSelect:
                    globals->SelectTurn = 1;
                    globals->SelectTimer = FP._3;  // 3초
                    break;

                case Phase.OrderSetup:
                    // 전투 순서 지정 시작
                    break;

                case Phase.Battle:
                    globals->CurrentRound = 1;
                    globals->BattleTimer = FP.FromFloat_UNSAFE(60);  // 60초
                    break;

                case Phase.Result:
                    // 최종 결과
                    break;
            }
        }
    }
}

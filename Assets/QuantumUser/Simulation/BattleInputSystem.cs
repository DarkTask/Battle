namespace Quantum
{
    using Photon.Deterministic;

    /// <summary>
    /// Input 처리 시스템
    /// </summary>
    public unsafe class BattleInputSystem : SystemMainThreadFilter<BattleInputSystem.Filter>
    {
        public struct Filter
        {
            public EntityRef Entity;
            public PlayerGameData* PlayerData;
        }

        public override void Update(Frame f, ref Filter filter)
        {
            var input = f.GetPlayerInput(filter.PlayerData->PlayerRef);

            // 캐릭터 선택 Input
            if (input->SelectedChampionId > 0)
            {
                CharacterSelectSystem.SelectChampion(f, filter.PlayerData->PlayerRef, input->SelectedChampionId - 1);
            }

            // 전투 순서 제출 Input
            if (input->OrderSubmit.WasPressed)
            {
                Log.Info($"📥 OrderSubmit Input received: Player={filter.PlayerData->PlayerRef}, Slots=[{input->OrderSlot1}, {input->OrderSlot2}, {input->OrderSlot3}]");
                OrderSetupSystem.SubmitOrder(f, filter.PlayerData->PlayerRef,
                    input->OrderSlot1, input->OrderSlot2, input->OrderSlot3);
            }
        }
    }
}

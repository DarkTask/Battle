namespace Quantum
{
    using Photon.Deterministic;
    using UnityEngine;
    using System;

    /// <summary>
    /// 게임 전체 설정 (Quantum Asset)
    /// </summary>
    [Serializable]
    public partial class BattleGameConfig : AssetObject
    {
        public const int TOTAL_CHAMPIONS = 8;  // UI에 표시되는 챔피언 수 (0~7)
        public const int MAX_SELECTION = 3;
        public const int TOTAL_ROUNDS = 4;  // 1~3: 1v1, 4: 3v3

        // Asset 필드
        public AssetRef<EntityPrototype>[] ChampionPrototypes;
        public FP SelectTimeLimit = FP._3;
        public FP BattleTimeLimit = FP.FromFloat_UNSAFE(60);
        public FPVector3[] SpawnPositions;

        /// <summary>
        /// 챔피언 ID로 프로토타입 가져오기
        /// </summary>
        public AssetRef<EntityPrototype> GetChampionPrototype(int championId)
        {
            if (championId < 0 || championId >= ChampionPrototypes.Length)
            {
                Debug.LogError($"Invalid championId: {championId}");
                return default;
            }
            return ChampionPrototypes[championId];
        }

        /// <summary>
        /// 스폰 위치 가져오기 (TeamId: 0=A, 1=B, SlotIndex: 0~2)
        /// </summary>
        public FPVector3 GetSpawnPosition(int teamId, int slotIndex)
        {
            int index = teamId * 3 + slotIndex;
            if (index < 0 || index >= SpawnPositions.Length)
            {
                Debug.LogError($"Invalid spawn index: teamId={teamId}, slotIndex={slotIndex}");
                return FPVector3.Zero;
            }
            return SpawnPositions[index];
        }
    }
}

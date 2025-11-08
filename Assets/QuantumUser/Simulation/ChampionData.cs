namespace Quantum
{
    using Photon.Deterministic;
    using System;

    /// <summary>
    /// 챔피언 데이터 Asset
    /// </summary>
    [Serializable]
    public partial class ChampionData : AssetObject
    {
        public FP Strength;
        public FP Dexterity;
        public FP Constitution;
        public AssetRef<EntityPrototype> Prefab;

        /// <summary>
        /// 스탯 기반 파생 값 계산
        /// </summary>
        public FP CalculateMaxHealth()
        {
            return Constitution * FP._10;  // Constitution * 10
        }

        public FP CalculateAttackPower()
        {
            return Strength * FP._2;  // Strength * 2
        }

        public FP CalculateAttackSpeed()
        {
            return FP._1 + (Dexterity / FP._10);  // 1 + (Dexterity / 10)
        }

        public FP CalculateMoveSpeed()
        {
            return FP._3 + (Dexterity / FP.FromFloat_UNSAFE(20));  // 3 + (Dexterity / 20)
        }
    }
}

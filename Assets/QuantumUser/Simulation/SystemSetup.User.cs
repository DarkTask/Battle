namespace Quantum
{
    using System;
    using System.Collections.Generic;

    public static partial class DeterministicSystemSetup
    {
        static partial void AddSystemsUser(ICollection<SystemBase> systems, RuntimeConfig gameConfig, SimulationConfig simulationConfig, SystemsConfig systemsConfig)
        {
            // The system collection is already filled with systems coming from the SystemsConfig.
            // Add or remove systems to the collection: systems.Add(new SystemFoo());

            // Battle Game Systems
            systems.Add(new PlayerManagementSystem());
            systems.Add(new GamePhaseSystem());
            systems.Add(new CharacterSelectSystem());
            systems.Add(new OrderSetupSystem());
            systems.Add(new BattleInputSystem());
            systems.Add(new BattleSystem());
            systems.Add(new SimpleAISystem());
        }
    }
}
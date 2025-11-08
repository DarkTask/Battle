#!/bin/bash

# CharacterSelectSystem.cs: line 78
sed -i '78s/playerData->SelectedChampions\[playerData->SelectedCount\] = championId;/playerData->SelectedChampions.Add(championId);/' "C:\Git\Battle\Assets\QuantumUser\Simulation\CharacterSelectSystem.cs"

# CharacterSelectSystem.cs: line 190
sed -i '190s/if (data->SelectedChampions\[i\] == championId)/if (data->SelectedChampions.Get(i) == championId)/' "C:\Git\Battle\Assets\QuantumUser\Simulation\CharacterSelectSystem.cs"

# OrderSetupSystem.cs: lines 59-61
sed -i '59s/data->BattleOrder\[0\] = slot1;/data->BattleOrder.Set(0, slot1);/' "C:\Git\Battle\Assets\QuantumUser\Simulation\OrderSetupSystem.cs"
sed -i '60s/data->BattleOrder\[1\] = slot2;/data->BattleOrder.Set(1, slot2);/' "C:\Git\Battle\Assets\QuantumUser\Simulation\OrderSetupSystem.cs"
sed -i '61s/data->BattleOrder\[2\] = slot3;/data->BattleOrder.Set(2, slot3);/' "C:\Git\Battle\Assets\QuantumUser\Simulation\OrderSetupSystem.cs"

# BattleSystem.cs: lines 107-108, 127-128
sed -i '107s/int championIndex = data->BattleOrder\[slotIndex\];/int championIndex = data->BattleOrder.Get(slotIndex);/' "C:\Git\Battle\Assets\QuantumUser\Simulation\BattleSystem.cs"
sed -i '108s/int championId = data->SelectedChampions\[championIndex\];/int championId = data->SelectedChampions.Get(championIndex);/' "C:\Git\Battle\Assets\QuantumUser\Simulation\BattleSystem.cs"
sed -i '127s/int championIndex = data->BattleOrder\[i\];/int championIndex = data->BattleOrder.Get(i);/' "C:\Git\Battle\Assets\QuantumUser\Simulation\BattleSystem.cs"
sed -i '128s/int championId = data->SelectedChampions\[championIndex\];/int championId = data->SelectedChampions.Get(championIndex);/' "C:\Git\Battle\Assets\QuantumUser\Simulation\BattleSystem.cs"

echo "QList indexing fixed!"

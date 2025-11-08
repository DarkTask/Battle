#!/bin/bash

# CharacterSelectSystem.cs: line 78 - Add
sed -i '78s/playerData->SelectedChampions\.Add(championId);/f.ResolveList(playerData->SelectedChampions).Add(championId);/' "C:\Git\Battle\Assets\QuantumUser\Simulation\CharacterSelectSystem.cs"

# CharacterSelectSystem.cs: line 189-190 - Get
sed -i '189,190s/data->SelectedChampions\.Get(i)/f.ResolveList(data->SelectedChampions)[i]/' "C:\Git\Battle\Assets\QuantumUser\Simulation\CharacterSelectSystem.cs"

# OrderSetupSystem.cs: lines 59-61 - Set
sed -i '59s/data->BattleOrder\.Set(0, slot1);/f.ResolveList(data->BattleOrder)[0] = slot1;/' "C:\Git\Battle\Assets\QuantumUser\Simulation\OrderSetupSystem.cs"
sed -i '60s/data->BattleOrder\.Set(1, slot2);/f.ResolveList(data->BattleOrder)[1] = slot2;/' "C:\Git\Battle\Assets\QuantumUser\Simulation\OrderSetupSystem.cs"
sed -i '61s/data->BattleOrder\.Set(2, slot3);/f.ResolveList(data->BattleOrder)[2] = slot3;/' "C:\Git\Battle\Assets\QuantumUser\Simulation\OrderSetupSystem.cs"

# BattleSystem.cs: lines 107-108, 127-128 - Get
sed -i '107s/data->BattleOrder\.Get(slotIndex)/f.ResolveList(data->BattleOrder)[slotIndex]/' "C:\Git\Battle\Assets\QuantumUser\Simulation\BattleSystem.cs"
sed -i '108s/data->SelectedChampions\.Get(championIndex)/f.ResolveList(data->SelectedChampions)[championIndex]/' "C:\Git\Battle\Assets\QuantumUser\Simulation\BattleSystem.cs"
sed -i '127s/data->BattleOrder\.Get(i)/f.ResolveList(data->BattleOrder)[i]/' "C:\Git\Battle\Assets\QuantumUser\Simulation\BattleSystem.cs"
sed -i '128s/data->SelectedChampions\.Get(championIndex)/f.ResolveList(data->SelectedChampions)[championIndex]/' "C:\Git\Battle\Assets\QuantumUser\Simulation\BattleSystem.cs"

echo "QListPtr fixed with ResolveList!"

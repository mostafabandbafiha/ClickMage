// IHarvestable.cs
using UnityEngine;

public interface IHarvestable
{
    bool CanHarvest();
    bool TryHarvest(IHarvester harvester);
    Vector3 GetInteractPosition();
}

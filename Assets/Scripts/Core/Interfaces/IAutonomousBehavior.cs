// IAutonomousBehavior.cs
using UnityEngine;

public interface IAutonomousBehavior
{
    string BehaviorName { get; }
    float CalculatePriority(BaseCharacter character);
    bool CanExecute(BaseCharacter character);
    void Execute(BaseCharacter character);
}

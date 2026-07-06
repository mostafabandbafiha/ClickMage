// ConditionNode.cs
using System;

/// <summary>
/// Wraps a condition check. Returns true if condition passes, false otherwise.
/// Does not issue commands — just evaluates state.
/// </summary>
public class ConditionNode<T> : IBehaviorNode<T> where T : BaseCharacter
{
    private readonly Func<T, bool> _condition;
    private readonly string _debugName;

    public ConditionNode(Func<T, bool> condition, string debugName = "Condition")
    {
        _condition = condition;
        _debugName = debugName;
    }

    public bool Execute(T owner)
    {
        bool result = _condition(owner);
        return result;
    }
}

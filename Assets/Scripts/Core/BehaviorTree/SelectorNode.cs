// SelectorNode.cs
using System.Collections.Generic;

/// <summary>
/// Tries each child in order until one succeeds.
/// Returns true on first success, false if all fail.
/// Use for priority-based decisions (try A, if not then B, if not then C...).
/// </summary>
public class SelectorNode<T> : IBehaviorNode<T> where T : BaseCharacter
{
    private readonly List<IBehaviorNode<T>> _children;

    public SelectorNode(params IBehaviorNode<T>[] children)
    {
        _children = new List<IBehaviorNode<T>>(children);
    }

    public bool Execute(T owner)
    {
        foreach (var child in _children)
        {
            if (child.Execute(owner))
                return true; // First success wins
        }

        return false; // All children failed
    }
}

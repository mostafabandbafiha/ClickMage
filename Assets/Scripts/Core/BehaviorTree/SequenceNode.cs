// SequenceNode.cs
using System.Collections.Generic;

/// <summary>
/// Executes each child in order. Stops on first failure.
/// Returns true only if ALL children succeed.
/// Use for multi-step tasks (do A, then B, then C).
/// </summary>
public class SequenceNode<T> : IBehaviorNode<T> where T : BaseCharacter
{
    private readonly List<IBehaviorNode<T>> _children;

    public SequenceNode(params IBehaviorNode<T>[] children)
    {
        _children = new List<IBehaviorNode<T>>(children);
    }

    public bool Execute(T owner)
    {
        foreach (var child in _children)
        {
            if (!child.Execute(owner))
                return false; // First failure aborts
        }

        return true; // All succeeded
    }
}

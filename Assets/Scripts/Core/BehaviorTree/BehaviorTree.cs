// BehaviorTree.cs
using UnityEngine;

public class BehaviorTree<T> where T : BaseCharacter
{
    private readonly IBehaviorNode<T> _rootNode;

    public BehaviorTree(IBehaviorNode<T> rootNode)
    {
        _rootNode = rootNode;
    }

    /// <summary>
    /// Evaluates the tree. Called periodically when character is autonomous and idle.
    /// </summary>
    public void Tick(T owner)
    {
        if (_rootNode == null)
        {
            Debug.LogWarning($"[BehaviorTree] {owner.name} has no root node.");
            return;
        }

        _rootNode.Execute(owner);
    }
}

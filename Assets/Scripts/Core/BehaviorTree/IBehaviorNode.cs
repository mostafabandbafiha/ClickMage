// IBehaviorNode.cs
public interface IBehaviorNode<T> where T : BaseCharacter
{
    /// <summary>
    /// Evaluates and executes this node.
    /// Returns true if the node succeeded, false otherwise.
    /// </summary>
    bool Execute(T owner);
}

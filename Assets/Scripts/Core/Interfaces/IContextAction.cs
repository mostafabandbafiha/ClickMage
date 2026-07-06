namespace ClickMage.Interaction
{
    public interface IContextAction
    {
        string ActionLabel { get; }
        bool IsAvailable(BaseCharacter character);
        void Execute(BaseCharacter character);
    }
}

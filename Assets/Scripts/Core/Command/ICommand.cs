public interface ICommand<TCharacter> where TCharacter : BaseCharacter
{
    void Start(TCharacter character);
    void Tick(TCharacter character, float deltaTime);
    bool IsComplete { get; }
    void Cancel();
}

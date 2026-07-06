public interface ICommandable<TCharacter> where TCharacter : BaseCharacter
{
    void GiveCommand(ICommand<TCharacter> command);
    void QueueCommand(ICommand<TCharacter> command);
    void ClearCommands();
}

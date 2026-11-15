namespace CozyGlade.Core
{
    public interface ICommand
    {
        void Do();
        void Undo();
    }
}

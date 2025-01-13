namespace TASMod.Views
{
    public interface IView
    {
        void Draw();
        void Update();

        void Enter();
        void Exit();
    }
}

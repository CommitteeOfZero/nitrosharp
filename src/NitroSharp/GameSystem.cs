namespace NitroSharp
{
    internal abstract class GameSystem
    {
        private readonly Game.Presenter _presenter;
        protected float DeltaTime;

        //protected GameSystem()
        //{
        //}

        protected GameSystem(Game.Presenter presenter)
        {
            _presenter = presenter;
        }

        public virtual void Update(float deltaTime)
        {
            DeltaTime = deltaTime;
        }

        protected void PostMessage(Game.Message message)
        {
            _presenter.PostMessage(message);
        }
    }
}

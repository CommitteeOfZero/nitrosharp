namespace NitroSharp.Animation
{
    internal struct AnimationProcessorOutput
    {
        public uint BlockingAnimationCount;
    }

    internal sealed class AnimationProcessor : GameSystem
    {
        private readonly World _world;

        public AnimationProcessor(Game.Presenter presenter) : base(presenter)
        {
            _world = presenter.World;
        }

        public AnimationProcessorOutput ProcessAnimations(float deltaTime)
        {
            uint blockingCount = 0;
            foreach (PropertyAnimation anim in _world.AttachedAnimations)
            {
                if (!_world.IsEntityAlive(anim.Entity))
                {
                    _world.DeactivateAnimation(anim);
                    PostMessage(new Game.AnimationCompletedMessage
                    {
                        Animation = anim
                    });
                    //if (anim.WaitingThread != null)
                    //{
                    //    _interpreter.ResumeThread(anim.WaitingThread);
                    //}
                }
                else
                {
                    if (anim.Update(_world, deltaTime))
                    {
                        if (anim.IsBlocking)
                        {
                            blockingCount++;
                        }
                    }
                    else
                    {
                        PostMessage(new Game.AnimationCompletedMessage
                        {
                            Animation = anim
                        });
                    }
                }
            }

            return new AnimationProcessorOutput
            {
                BlockingAnimationCount = blockingCount
            };
        }
    }
}

using NitroSharp.NsScript.Execution;

namespace NitroSharp.Animation
{
    internal struct AnimationProcessorOutput
    {
        public uint BlockingAnimationCount;
    }

    internal sealed class AnimationProcessor
    {
        private readonly World _world;
        private readonly NsScriptInterpreter _interpreter;

        public AnimationProcessor(World world, NsScriptInterpreter interpreter)
        {
            _world = world;
            _interpreter = interpreter;
        }

        public AnimationProcessorOutput ProcessAnimations(float deltaTime)
        {
            uint blockingCount = 0;
            foreach (PropertyAnimation anim in _world.AttachedAnimations)
            {
                if (!_world.IsEntityAlive(anim.Entity))
                {
                    _world.DeactivateAnimation(anim);
                    if (anim.WaitingThread != null)
                    {
                        _interpreter.ResumeThread(anim.WaitingThread);
                    }
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
                        if (anim.WaitingThread != null)
                        {
                            _interpreter.ResumeThread(anim.WaitingThread);
                        }
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

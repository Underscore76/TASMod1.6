using TASMod.Scripting;

namespace TASMod
{
    public enum LaunchState
    {
        None,
        Launched,
        WindowInitialized,
        Loaded,
        LuaBoot,
        Finalized
    }

    public class LaunchManager
    {
        public LaunchState LaunchState = LaunchState.None;

        public bool Update()
        {
            switch (LaunchState)
            {
                case LaunchState.None:
                    LaunchState = LaunchState.Launched;
                    return true;
                case LaunchState.Launched:
                    LaunchState = LaunchState.WindowInitialized;
                    return true;
                case LaunchState.WindowInitialized:
                    LaunchState = LaunchState.Loaded;
                    return true;
                case LaunchState.Loaded:
                    Controller.Reset();
                    LaunchState = LaunchState.LuaBoot;
                    return false;
                case LaunchState.LuaBoot:
                    LaunchState = LaunchState.Finalized;
                    if (LuaEngine.LuaState == null)
                    {
                        LuaEngine.Reload();
                    }
                    LuaEngine.Boot();
                    return false;
                case LaunchState.Finalized:
                default:
                    return false;
            }
        }
    }
}
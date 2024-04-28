namespace TASMod
{
    public enum LaunchState
    {
        None,
        Launched,
        WindowInitialized,
        Loaded,
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
                    LaunchState = LaunchState.Finalized;
                    return false;
                case LaunchState.Finalized:
                default:
                    return false;
            }
        }
    }
}
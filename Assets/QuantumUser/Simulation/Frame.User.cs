namespace Quantum
{
    public unsafe partial class Frame
    {
        /// <summary>
        /// Helper: Get Globals pointer (uses Frame.Global property)
        /// </summary>
        public _globals_* Globals => (_globals_*)_globals;

#if UNITY_ENGINE

#endif
    }
}
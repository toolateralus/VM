using System;   

namespace VM
{
    public static class Notifications
    {
        /// <summary>
        /// TODO: Remove this, and put it somewhere better. we abandoned this, it was coupled tightly with the UI.
        /// </summary>
        /// <param name="e"></param>
        internal static void Exception(Exception e)
        {
            IO.OSTREAM(e.Message + "\n" + e.InnerException + "\n" + e.Source);  
        }
    }
}

using System.Reflection;

namespace Visprech.Cmd
{
    internal static class ConsoleMessages
    {
        internal static string AppHeader =
            @$"
===================================
Video Speech Checker (visprech)    
  version:{Assembly.GetExecutingAssembly().GetName().Version:-10}{string.Format("{0,-15}"," ")}   
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~";

        internal static string AnyKeyToExit =
            "Press any key to exit... ";
    }
}
 
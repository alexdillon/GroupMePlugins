using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace Gallery
{
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [ComVisible(true)]
    public class ObjectForScriptingHelper
    {
        public ObjectForScriptingHelper(MainWindow mainWindow)
        {
            this.MainWindow = mainWindow;
        }

        private MainWindow MainWindow { get; }

        public void OpenContextView(string id)
        {
            this.MainWindow.OpenContextView(id);
        }
    }
}

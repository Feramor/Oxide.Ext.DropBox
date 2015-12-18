using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//Oxide
using Oxide.Core;
using Oxide.Core.Extensions;
using Oxide.Core.Libraries;
using System.Reflection;

namespace Oxide.Ext.DropBox
{
    public class DropBoxExtension : Extension
    {
        public DropBoxExtension(ExtensionManager manager) : base(manager)
        {

        }

        public override string Name => "DropBox";

        public override VersionNumber Version => new VersionNumber(1, 0, 0);

        public override string Author => "Feramor";

        private Libraries.DropBox _DropBox;

        public override void Load()
        {
            LoadAssembly("Common_Logging", EmbDlls.Common_Logging);
            LoadAssembly("ICSharpCode_SharpZipLib", EmbDlls.ICSharpCode_SharpZipLib);
            LoadAssembly("Spring_Rest", EmbDlls.Spring_Rest);
            LoadAssembly("Spring_Social_Core", EmbDlls.Spring_Social_Core);
            LoadAssembly("Spring_Social_Dropbox", EmbDlls.Spring_Social_Dropbox);
            Manager.RegisterLibrary("DropBox", _DropBox = new Libraries.DropBox());
        }

        public override void LoadPluginWatchers(string plugindir)
        {
            _DropBox?.Initialize();
        }

        public override void OnModLoad()
        {
            
        }

        public override void OnShutdown()
        {
            _DropBox?.Shutdown();
        }
        private void LoadAssembly(string Name,byte[] RawAssemblyByte)
        {
            try
            {
                Assembly.Load(RawAssemblyByte);
            }
            catch
            {
                Interface.Oxide.LogError("[DropBox] Failed to load an Assembly ({0}).", Name);
            }
        }
    }
}

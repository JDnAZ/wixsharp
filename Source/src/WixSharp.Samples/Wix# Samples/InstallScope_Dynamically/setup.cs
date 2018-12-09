//css_dir ..\..\;
//css_ref Wix_bin\SDK\WixToolset.Dtf.WindowsInstaller.dll;
//css_ref System.Core.dll;
//css_ref System.Xml.dll;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using WixSharp;
using WixSharp.Forms;

class Script
{
    static public void Main()
    {
        var project = new ManagedProject("MyProduct",
                    new Dir(@"C:\My Company\My Product",
                        new File("readme.txt")));
        project.ManagedUI = new ManagedUI();

        project.ManagedUI.InstallDialogs.Add(Dialogs.Progress)
                                        .Add(Dialogs.Exit);

        project.ManagedUI.ModifyDialogs.Add(Dialogs.Progress)
                                        .Add(Dialogs.Exit);

        project.UIInitialized += (SetupEventArgs e) =>
        {
            if (e.IsInstalling && !e.IsUpgrading)
            {
                e.Session["ALLUSERS"] = "2";
                if (MessageBox.Show("Install for 'All Users'?", e.ProductName, MessageBoxButtons.YesNo) == DialogResult.Yes)
                    e.Session["MSIINSTALLPERUSER"] = "0"; // per-machine
                else
                    e.Session["MSIINSTALLPERUSER"] = "1"; // per-user
            }
        };

        project.BuildMsi();
    }
}
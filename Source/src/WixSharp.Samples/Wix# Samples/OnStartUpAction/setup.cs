//css_dir ..\..\;
//css_ref Wix_bin\SDK\WixToolset.Dtf.WindowsInstaller.dll;
//css_ref System.Core.dll;
using System;
using System.Xml;
using System.Xml.Linq;
using System.Windows.Forms;
using WixSharp;
using WixToolset.Dtf.WindowsInstaller;

public class Script
{
    static public void Main()
    {
        var project =
            new Project("MyProduct",

                new Dir(@"%ProgramFiles%\My Company\My Product",
                    new WixSharp.File(@"readme.txt")),

                new ManagedAction(Script.OnSetupStartup,
                                    Return.check,
                                    When.Before,
                                    Step.LaunchConditions,
                                    Condition.NOT_Installed,
                                    Sequence.InstallUISequence));

        project.UI = WUI.WixUI_InstallDir;
        project.GUID = new Guid("6f330b47-2577-43ad-9095-1861bb25889b");

        Compiler.BuildMsi(project);
    }

    [CustomAction]
    public static ActionResult OnSetupStartup(Session session)
    {
        MessageBox.Show("OnSetupStartup");

        return ActionResult.Success;
    }
}
//css_dir ..\..\;
//css_ref Wix_bin\SDK\WixToolset.Dtf.WindowsInstaller.dll;
//css_ref System.Core.dll;
using WixSharp;
using System.Xml;
using System.Xml.Linq;
using WixToolset.Dtf.WindowsInstaller;

class Script
{
    static public void Main()
    {
        var project =
            new Project("Setup",
                new Dir(@"%ProgramFiles%\RunAppTest",
                    new File("readme.txt")),
                new ManagedAction("MyAction"));

        project.UI = WUI.WixUI_ProgressOnly;

        Compiler.BuildMsi(project);
    }
}

public class CustonActions
{
    [CustomAction]
    public static ActionResult MyAction(Session session)
    {
        System.Diagnostics.Process.Start("Notepad.exe", session["INSTALLDIR"] + @"\readme.txt");
        return ActionResult.Success;
    }
}
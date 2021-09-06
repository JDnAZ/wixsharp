using Microsoft.Deployment.WindowsInstaller;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WixSharp;
using WixSharp.UI.Forms;

namespace WixSharp.UI.WPF
{
    /// <summary>
    /// Returns an array of AssemblyName that definses referenced assemblies required at runtime for WixSharp WPF dialogs.
    /// <para>Typically it is Caliburn.Micro assemblies and their dependencies and WixSharp.UI assembly.</para>
    /// <para>This method is to be used by WixSharp compiler only.</para>
    /// </summary>
    public static class DependencyDescriptor
    {
        /// <summary>
        /// Gets the referenced assemblies.
        /// </summary>
        /// <returns></returns>
        public static System.Reflection.AssemblyName[] GetRefAssemblies()
            => new[]
                {
                    System.Reflection.Assembly.Load("System.Windows.Interactivity").GetName(),
                    System.Reflection.Assembly.Load("Caliburn.Micro.Platform").GetName(),
                    System.Reflection.Assembly.Load("Caliburn.Micro.Platform.Core").GetName(),
                    System.Reflection.Assembly.Load("Caliburn.Micro").GetName(),
                    System.Reflection.Assembly.Load("WixSharp.UI").GetName()
                };
    }

    /// <summary>
    /// WPF related generic method extensions
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// </summary>
        /// <param name="src">The source.</param>
        /// <returns></returns>
        public static BitmapImage ToImageSource(this Bitmap src)
        {
            var ms = new MemoryStream();
            src.Save(ms, ImageFormat.Bmp);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }

        /// <summary>
        /// Localizes the specified WPF element and its children.
        /// <para>The localization is performed according
        /// <see cref="InstallerRuntime.Localize(string)"/> localization behavior.</para>
        /// </summary>
        /// <returns></returns>
        public static InstallerRuntime Localize(this InstallerRuntime runtime, DependencyObject parent)
        {
            string translate(string text)
                => runtime.Localize(text.Trim('[', ']'))
                          .Replace("&", "")
                          .LocalizeWith(runtime.Localize); // trim buttons text "&Next"
            bool isLocalizable(string text)
                => text.StartsWith("[") && text.EndsWith("]");

            parent
                .GetChildrenOfType<TextBlock>()
                .Where(x => isLocalizable(x.Text))
                .ForEach(x => x.Text = translate(x.Text));

            parent
                .GetChildrenOfType<Button>()
                .Where(x => isLocalizable(x.Content.ToString()))
                .ForEach(x => x.Content = translate(x.Content.ToString()));

            return runtime;
        }

        /// <summary>
        /// Gets the <see cref="DependencyObject"/> children of the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="depObj">The <see cref="DependencyObject"/> instance.</param>
        /// <returns></returns>
        public static IEnumerable<T> GetChildrenOfType<T>(this DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                        yield return (T)child;

                    foreach (T childOfChild in GetChildrenOfType<T>(child))
                        yield return childOfChild;
                }
            }
        }
    }

    class WpfDialogMock : UI.WPF.WpfDialog { } // this private class is needed for testing

    /// <summary>
    /// Interface that marks WPF content (e.g. UserControl) as a content that can be embedded in
    /// the custom dialog.
    /// </summary>
    /// <example>The following is an example of adding a UserControl ('CustomDialogPanel') as a
    /// content of the custom dialog. Note, your control must implement <see cref="IWpfDialogContent"/>
    /// interface so WixSharp recognizes it as an embeddable custom dialog content.<code>
    /// project.ManagedUI.InstallDialogs.Add&gt;WelcomeDialog&gt;()
    ///                                 .Add&gt;FeaturesDialog&gt;()
    ///                                 .Add&gt;CustomDialogWith&lt;CustomDialogPanel&gt;&gt;()
    /// . . .
    /// public partial class CustomDialogPanel : UserControl, IWpfDialogContent
    /// {
    ///     public CustomDialogPanel()
    ///     {
    ///         InitializeComponent();
    ///     }
    ///
    ///     public void Init(CustomDialogBase parent)
    ///     {
    ///         ISession session = parent?.ManagedFormHost.Runtime.Session;
    ///         . . .
    ///
    /// </code>
    /// </example>
    /// <seealso cref="WixSharp.IDialog" />
    public interface IWpfDialogContent : IDialog
    {
        /// <summary>
        /// Initializes the instance of <see cref="IWpfDialogContent"/> and passes the
        /// reference to the parent dialog. It is a good place to adjust layout including parent dialog
        /// element (e.g. disable "Next" button). Or do localization
        /// </summary>
        /// <param name="parentDialog">The parent dialog.</param>
        void Init(CustomDialogBase parentDialog);
    }

    /// <summary>
    /// A base class for WPF custom dialogs.
    /// </summary>
    /// <seealso cref="System.Windows.Controls.UserControl" />
    /// <seealso cref="WixSharp.IManagedDialog" />
    public class WpfDialog : UserControl, IManagedDialog
    {
        string dialogTitle;

        /// <summary>
        /// Gets or sets the dialog title.
        /// </summary>
        /// <value>
        /// The dialog title.
        /// </value>
        public string DialogTitle
        {
            get
            {
                return dialogTitle;
            }

            set
            {
                dialogTitle = value;
                if (ManagedFormHost != null)
                {
                    ManagedFormHost.Text = value;
                    ManagedFormHost.Localize();
                }
            }
        }

        /// <summary>
        /// Gets or sets the reference to the dialog host (e.g. <see cref="ManagedForm"/>).
        /// </summary>
        /// <value>
        /// The host.
        /// </value>
        public IManagedDialog Host { get; set; }

        /// <summary>
        /// Gets or sets the reference to the dialog host.
        /// <para>This property is the same as <see cref="WpfDialog.Host"/> except it returns already typecasted host instance</para>
        /// </summary>
        /// <value>
        /// The managed form host.
        /// </value>
        public ManagedForm ManagedFormHost { get => (ManagedForm)Host; }

        /// <summary>
        /// Gets or sets the UI shell (main UI window). This property is set the ManagedUI runtime (IManagedUI).
        /// On the other hand it is consumed (accessed) by the UI dialog (IManagedDialog).
        /// </summary>
        /// <value>
        /// The shell.
        /// </value>
        public IManagedUIShell Shell { get; set; }

        /// <summary>
        /// Called when MSI execution is complete.
        /// </summary>
        public void Localize(DependencyObject element = null)
        {
            var root = (element ?? this.Content as DependencyObject);

            // resolve and translate all elements with translatable content ("[<localization_key>]")
            if (root != null)
                this.ManagedFormHost.Runtime.Localize(root);
        }

        /// <summary>
        /// Called when MSI execution is complete.
        /// </summary>
        virtual public void OnExecuteComplete()
        {
        }

        /// <summary>
        /// Called when MSI execute started.
        /// </summary>
        virtual public void OnExecuteStarted()
        {
        }

        /// <summary>
        /// Called when MSI execution progress is changed.
        /// </summary>
        /// <param name="progressPercentage">The progress percentage.</param>
        virtual public void OnProgress(int progressPercentage)
        {
        }

        /// <summary>
        /// Processes information and progress messages sent to the user interface.
        /// <para> This method directly mapped to the
        /// <see cref="T:Microsoft.Deployment.WindowsInstaller.IEmbeddedUI.ProcessMessage" />.</para>
        /// </summary>
        /// <param name="messageType">Type of the message.</param>
        /// <param name="messageRecord">The message record.</param>
        /// <param name="buttons">The buttons.</param>
        /// <param name="icon">The icon.</param>
        /// <param name="defaultButton">The default button.</param>
        /// <returns></returns>
        virtual public MessageResult ProcessMessage(InstallMessage messageType, Record messageRecord, MessageButtons buttons, MessageIcon icon, MessageDefaultButton defaultButton)
            => MessageResult.OK;
    }

    /// <summary>
    /// A managed dialog (WinForm) class that is capable of hosting WPF content. This class it to be use by WixSharp runtime only and not
    /// intended to be a part of any user code (setup definition).
    /// <para>Note WixSharp uses this class via Reflection with <see cref="UIShell.CreateDefaultWpfDialgHost"/></para>
    /// </summary>
    /// <seealso cref="WixSharp.UI.Forms.ManagedForm" />
    /// <seealso cref="WixSharp.IManagedDialog" />
    /// <seealso cref="WixSharp.IWpfDialogHost" />
    public class WpfDialogHost : ManagedForm, IManagedDialog, IWpfDialogHost
    {
        IWpfDialog content;

        /// <summary>
        /// Sets the content of the dialog.
        /// </summary>
        /// <param name="content">The content.</param>
        public void SetDialogContent(IWpfDialog content)
        {
            this.content = content;

            this.Load += (s, _e) =>
            {
                content.Host = this;
                var host = new System.Windows.Forms.Integration.ElementHost();
                host.Dock = System.Windows.Forms.DockStyle.Fill;
                host.Child = (UserControl)content;

                if (content is WpfDialog wpfDialog)
                {
                    wpfDialog.Localize();
                    this.Text = wpfDialog.DialogTitle;
                }
                this.Localize();

                this.Controls.Add(host);
                content.Init();
            };
        }

        /// <summary>
        /// Called when Shell is changed. It is a good place to initialize the dialog to reflect the MSI session
        /// (e.g. localize the view).
        /// </summary>
        override protected void OnShellChanged()
        {
            if (content is IManagedDialog)
                (this.content as IManagedDialog).Shell = this.Shell;
        }

        public override void OnExecuteStarted()
        {
            if (content is IManagedDialog)
            {
                (content as IManagedDialog).OnExecuteStarted();
            }
        }

        public override void OnExecuteComplete()
        {
            if (content is IManagedDialog)
            {
                (content as IManagedDialog).OnExecuteComplete();
            }
        }

        public override void OnProgress(int progressPercentage)
        {
            if (content is IManagedDialog)
            {
                try
                {
                    (content as IManagedDialog).OnProgress(progressPercentage);
                }
                catch (Exception error)
                {
                    Runtime.Session.Log(error.Message);
                }
            }
        }

        public override MessageResult ProcessMessage(InstallMessage messageType, Record messageRecord, MessageButtons buttons, MessageIcon icon, MessageDefaultButton defaultButton)
        {
            if (content is IManagedDialog)
            {
                try
                {
                    return (content as IManagedDialog).ProcessMessage(messageType, messageRecord, buttons, icon, defaultButton);
                }
                catch (Exception error)
                {
                    Runtime.Session.Log(error.Message);
                }
            }
            return base.ProcessMessage(messageType, messageRecord, buttons, icon, defaultButton);
        }
    }
}
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Text.RegularExpressions;
using Task = System.Threading.Tasks.Task;



namespace RemoveCommentsPlugin
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class RemoveCommentsCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("d530e7d7-f039-4a74-95f9-4480e162f474");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveCommentsCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private RemoveCommentsCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static RemoveCommentsCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in RemoveCommentsCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new RemoveCommentsCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            // Получаем DTE2 из текущего контекста пакета
            DTE2 dte = (DTE2)Package.GetGlobalService(typeof(DTE));

            if (dte != null && dte.ActiveDocument != null)
            {
                Document activeDocument = dte.ActiveDocument;
                TextDocument textDocument = (TextDocument)activeDocument.Object("TextDocument");
                EditPoint startPoint = textDocument.StartPoint.CreateEditPoint();
                EditPoint endPoint = textDocument.EndPoint.CreateEditPoint();

                // Удаление всех комментариев с помощью регулярного выражения
                string code = startPoint.GetText(endPoint);
                string uncommentedCode = Regex.Replace(code, @"//.*?$|/\*.*?\*/", "", RegexOptions.Singleline | RegexOptions.Multiline);
                startPoint.ReplaceText(endPoint, uncommentedCode, (int)vsEPReplaceTextOptions.vsEPReplaceTextAutoformat);

            }
        }
    }
}
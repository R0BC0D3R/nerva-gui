using Eto.Forms;
using Eto.Drawing;
using Nerva.Desktop.Content;
using Nerva.Desktop.Helpers;

namespace Nerva.Desktop
{
    public partial class MainForm : Form
	{	
		#region Status Bar controls
		
		Label lblDaemonStatus = new Label { Text = "Daemon Offline" };
		Label lblWalletStatus = new Label { Text = "Wallet Offline" };
		Label lblVersion = new Label { Text = "Version: 0.0.0.0" };
		Label lblTaskList = new Label { Text = "Tasks: 0", Tag = -1 };

		DaemonPage daemonPage = new DaemonPage();
		BalancesPage balancesPage = new BalancesPage();
		TransfersPage transfersPage = new TransfersPage();

		#endregion

		public void ConstructLayout()
		{
			Title = $"NERVA Desktop Wallet and Miner {Version.LONG_VERSION}";
			ClientSize = new Size(640, 480);

			// Set Icon but only if found. Otherwise, app will not work correctly
			string iconFile = GlobalMethods.GetAppIcon();
			if(!string.IsNullOrEmpty(iconFile))
			{				
				Icon = new Icon(iconFile);
			}

			daemonPage.ConstructLayout();
			balancesPage.ConstructLayout();
			transfersPage.ConstructLayout();

			TabControl tabs = new TabControl
			{
				Pages = {
					new TabPage { Text = "Daemon", Content = daemonPage.MainControl },
					new TabPage { Text = "Balances", Content = balancesPage.MainControl },
					new TabPage { Text = "Transfers", Content = transfersPage.MainControl }
				}
			};

			TableLayout statusBar = new TableLayout
			{
				Padding = 5,
				Rows = {
					new TableRow (
						new TableCell(lblDaemonStatus, true),
						new TableCell(lblTaskList)),
					new TableRow (
						new TableCell(lblWalletStatus, true),
						new TableCell(lblVersion))
				}
			};

			Content = new TableLayout
			{
				Rows = {
					new TableRow (
						new TableCell(tabs, true)) { ScaleHeight = true },
					new TableRow (
						new TableCell(statusBar, true))
				}
			};

			// File
			var file_Preferences = new Command { MenuText = "Preferences", ToolBarText = "Preferences" };	
			file_Preferences.Executed += file_Preferences_Clicked;

			var quitCommand = new Command { MenuText = "Quit", Shortcut = Application.Instance.CommonModifier | Keys.Q };
			quitCommand.Executed += quit_Clicked;


			// Daemon
			var daemon_ToggleMining = new Command { MenuText = "Toggle Miner", ToolBarText = "Toggle Miner" };			
			daemon_ToggleMining.Executed += daemon_ToggleMining_Clicked;

			var daemon_Restart = new Command { MenuText = "Restart", ToolBarText = "Restart" };
			daemon_Restart.Executed += daemon_Restart_Clicked;


			// Wallet
			var wallet_New = new Command { MenuText = "New", ToolBarText = "New" };
			wallet_New.Executed += wallet_New_Clicked;

			var wallet_Open = new Command { MenuText = "Open", ToolBarText = "Open" };
			wallet_Open.Executed += wallet_Open_Clicked;

			var wallet_Stop = new Command { MenuText = "Close", ToolBarText = "Close wallet" };
			wallet_Stop.Executed += wallet_Stop_Clicked;

			var wallet_Import = new Command { MenuText = "Import", ToolBarText = "Import" };
			wallet_Import.Executed += wallet_Import_Clicked;

			var wallet_Store = new Command { MenuText = "Save", ToolBarText = "Save" };
			wallet_Store.Executed += wallet_Store_Clicked;
			
			var wallet_Account_Create = new Command { MenuText = "Create Account", ToolBarText = "Create Account" };
			wallet_Account_Create.Executed += wallet_Account_Create_Clicked;

			var wallet_RescanSpent = new Command { MenuText = "Spent Outputs", ToolBarText = "Spent Outputs" };
			wallet_RescanSpent.Executed += wallet_RescanSpent_Clicked;

			var wallet_RescanBlockchain = new Command { MenuText = "Blockchain", ToolBarText = "Blockchain" };
			wallet_RescanBlockchain.Executed += wallet_RescanBlockchain_Clicked;

			var wallet_Keys_View = new Command { MenuText = "View Keys", ToolBarText = "View Keys" };
			wallet_Keys_View.Executed += wallet_Keys_View_Clicked;


			// Help
			var debugFolderCommand = new Command { MenuText = "Debug Folder" };
			debugFolderCommand.Executed += debugFolderCommand_Clicked;

			var file_UpdateCheck = new Command { MenuText = "Check for Updates", ToolBarText = "Check for Updates" };	
			file_UpdateCheck.Executed += file_UpdateCheck_Clicked;

			var discordCommand = new Command { MenuText = "Discord" };
			discordCommand.Executed += discord_Clicked;

			var twitterCommand = new Command { MenuText = "Twitter" };
			twitterCommand.Executed += twitter_Clicked;

			var redditCommand = new Command { MenuText = "Reddit" };
			redditCommand.Executed += reddit_Clicked;

			var aboutCommand = new Command { MenuText = "About..." };
			aboutCommand.Executed += about_Clicked;

			// create menu
			Menu = new MenuBar
			{
				Items =
				{
					// File submenu
					new ButtonMenuItem
					{ 
						Text = "&File",
						Items =
						{ 
							file_Preferences
						}
					},
					new ButtonMenuItem
					{
						Text = "&Daemon",
						Items =
						{
							daemon_ToggleMining,
							daemon_Restart
						}
					},
					new ButtonMenuItem
					{
						Text = "&Wallet",
						Items =
						{
							wallet_New,
							wallet_Open,
							wallet_Import,
							wallet_Stop,
							new SeparatorMenuItem(),
							wallet_Store,
							wallet_Account_Create,
							new ButtonMenuItem
							{
								Text = "Rescan",
								Items =
								{
									wallet_RescanSpent,
									wallet_RescanBlockchain
								}
							},
							wallet_Keys_View
						}
					},
					new ButtonMenuItem
					{
						Text = "&Help",
						Items =
						{
							debugFolderCommand,
							file_UpdateCheck,
							new SeparatorMenuItem(),
							discordCommand,
							redditCommand,
							twitterCommand
						}
					}
				},
				QuitItem = quitCommand,
				AboutItem = aboutCommand
			};
		}
	}
}

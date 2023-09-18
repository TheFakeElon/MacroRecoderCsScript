using System;
using System.IO;
using System.Windows;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Scripting;

namespace MacroRecoderCsScript
{
	/// <summary>
	/// App.xaml の相互作用ロジック
	/// </summary>
	public partial class App : Application
	{
		private async void Application_StartupAsync( object sender, StartupEventArgs e )
		{
			try {
				// when no arguments are specified, execute main window 
				if( e.Args.Length == 0 ) {
					AppEnvironment.GetInstance().IsConsoleMode = false;
					var window = new MainWindow();
					window.Show();
					return;
				}
				else if( e.Args.Length >= 2 ) {
					Usage();
				}

				Regex scriptArgPattern = new Regex( "-script=(?<scriptPath>.+) -loop=(?<execLoops>.+)" );
				Match argsChecker = scriptArgPattern.Match( e.Args[ 0 ] );

				if( argsChecker.Success ) {
					string filePath = argsChecker.Groups[ "scriptPath" ].Value;
					int loops;
					try
					{
						loops = int.Parse(argsChecker.Groups["execLoops"].Value);
					}
					catch(Exception)
					{
						CommonUtil.WriteToConsole("No valid loop parameter specified, script will only be executed once.");
						loops = 0;
					}
					if( File.Exists( filePath ) ){
						AppEnvironment.GetInstance().DpiSetting();
						await ScriptExecuter.ExecuteAsync( filePath, loops );
					}
					else {
						CommonUtil.WriteToConsole( "[File Error]" + Environment.NewLine + "'" + filePath + "' is not found." );
					}
				}
				else {
					Usage();
				}
			}
			catch( CompilationErrorException ex ) {
				CommonUtil.WriteToConsole( "[Compile Error]" + Environment.NewLine + ex.Message );
			}
			catch( Exception ex ) {
				CommonUtil.HandleException( ex );
			}

			Current.Shutdown();
		}

		private void Usage()
		{
			CommonUtil.WriteToConsole( "Usage: MacroRecoderCsScript <option>" + Environment.NewLine +
										"[option]" + Environment.NewLine +
										"-script=<scirpt path>: Command line mode and only execute script" );
		}
	}
}

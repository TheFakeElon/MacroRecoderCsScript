﻿using System;
using System.IO;
using System.Threading;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using Microsoft.Win32;
using System.Windows.Input;
using Microsoft.CodeAnalysis.Scripting;
using System.Windows.Controls;
using System.Linq;

namespace MacroRecoderCsScript
{
	class MainWindowViewModel : INotifyPropertyChanged
	{
		private ButtonState buttonState;
		private ScriptRecorder recorder;
		private int loopCount;
		private string scriptPath;
		private string errorMessage;

		
		private static readonly PropertyChangedEventArgs loopCountChangedEventArgs = new PropertyChangedEventArgs( nameof( LoopCount ) );
		private static readonly PropertyChangedEventArgs scriptPathChangedEventArgs = new PropertyChangedEventArgs( nameof( ScriptPath ) );
		private static readonly PropertyChangedEventArgs errorMessageChangedEventArgs = new PropertyChangedEventArgs( nameof( ErrorMessage ) );

		private CancellationTokenSource cancelTokenSrc;
		public event PropertyChangedEventHandler PropertyChanged;

		public DelegateCommand RecordCommand { get; set; }
		public DelegateCommand StopCommand { get; set; }
		public DelegateCommand BrowseCommand { get; set; }
		public AsyncDelegateCommand PlayCommand { get; set; }

		public Dispatcher WinDispacher { get; set; }

		public string LoopCount
		{
			get { return loopCount.ToString(); }
			set
			{
				if(!int.TryParse(value, out int num) || loopCount == num) return;
				loopCount = Math.Max(num, -1);
				PropertyChanged?.Invoke( this, loopCountChangedEventArgs );
			}
		}
		public string ScriptPath
		{
			get { return scriptPath; }
			set {
				if( scriptPath == value ) {
					return;
				}

				scriptPath = value;
				PropertyChanged?.Invoke( this, scriptPathChangedEventArgs );
			}
		}

		public string ErrorMessage
		{
			get { return errorMessage; }
			set {
				if( errorMessage == value ) {
					return;
				}

				errorMessage = value;
				PropertyChanged?.Invoke( this, errorMessageChangedEventArgs );
			}
		}

		public MainWindowViewModel()
		{
			buttonState = new ButtonState();
			recorder = new ScriptRecorder(this);

			RecordCommand = new DelegateCommand( RecordCmd_Execute, RecordCmd_CanExecute );
			StopCommand = new DelegateCommand( StopCmd_Execute, StopCmd_CanExecute );
			BrowseCommand = new DelegateCommand( BrowseCmd_Execute );
			PlayCommand = new AsyncDelegateCommand( PlayCmd_ExecuteAsync, PlayCmd_CanExecute );

			ErrorMessage = "[Message]" + Environment.NewLine + "If expected error occur, view to this text box.";
		}

		private bool RecordCmd_CanExecute()
		{
			return !buttonState.IsRecording && !buttonState.IsPlaying;
		}

		private bool PlayCmd_CanExecute()
		{
			return !buttonState.IsRecording && !buttonState.IsPlaying;
		}

		private bool StopCmd_CanExecute()
		{
			return buttonState.IsRecording || buttonState.IsPlaying;
		}

		private void RecordCmd_Execute()
		{
			buttonState.IsRecording = true;
			recorder.StartRecording();
		}

		private async Task PlayCmd_ExecuteAsync()
		{
			if( !File.Exists( scriptPath ) ) {
				ErrorMessage = "[File Error]" + Environment.NewLine + "'" + scriptPath + "' is not found.";
				return;
			}

			ErrorMessage = "";

			try {
				buttonState.IsPlaying = true;
				cancelTokenSrc = new CancellationTokenSource();
				AppEnvironment.GetInstance().CancelToken = cancelTokenSrc.Token;

				WinDispacher?.Invoke( new Action( CommandManager.InvalidateRequerySuggested ) );
				await ScriptExecuter.ExecuteAsync( ScriptPath, loopCount );
			}
			catch( CompilationErrorException ex ) {
				ErrorMessage = "[Compile Error]" + Environment.NewLine + ex.Message;
			}
			catch( TaskCanceledException ) {
				ErrorMessage = "[Message]Script was cancelled.";
			}
			catch( Exception ) {
				throw;
			}

			buttonState.IsPlaying = false;
			WinDispacher?.Invoke( new Action( CommandManager.InvalidateRequerySuggested ) );
		}

		private void StopCmd_Execute()
		{
			if( buttonState.IsRecording ) {
				recorder.EndRecording();
				buttonState.IsRecording = false;
				SaveMacroScript();
			}
			else if( buttonState.IsPlaying ) {
				cancelTokenSrc.Cancel();
			}
		}

		private void BrowseCmd_Execute()
		{
			var dialog = new OpenFileDialog()
			{
				Title = "Open macro script",
				Filter = "Script File(*.csx)|*.csx|All files (*.*) |*.*",
				FilterIndex = 1
			};

			if( dialog.ShowDialog() == true ) {
				ScriptPath = dialog.FileName;
			}
		}

		private void SaveMacroScript()
		{
			var dialog = new SaveFileDialog()
			{
				Title = "Save macro script",
				Filter = "Script File(*.csx)|*.csx|All files (*.*) |*.*",
				FilterIndex = 1
			};

			if( dialog.ShowDialog() == true ) {
				using( var fs = dialog.OpenFile() ) {
					using( var sw = new StreamWriter( fs ) ) {
						sw.Write( recorder.Record );
					}
				}
			}
		}
	}
}

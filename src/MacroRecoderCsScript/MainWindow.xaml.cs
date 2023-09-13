using System.Windows;
using System.Windows.Input;
using System.Linq;

namespace MacroRecoderCsScript
{
	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : Window
	{
		private MainWindowViewModel userModel = new MainWindowViewModel();
		public MainWindow()
		{
			DataContext = userModel;
			InitializeComponent();

			AppEnvironment.GetInstance().DpiSetting();
			userModel.WinDispacher = Application.Current.Dispatcher;
		}
        private void TextNumberValidation(object sender, TextCompositionEventArgs e)
        {
            e.Handled = e.Text.All(x => !char.IsDigit(x));
        }
    }
}

using Windows.ApplicationModel;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using RRemote.ViewModels;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RRemote
{
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            Application.Current.Suspending += Current_Suspending;
            vm = new RemoteViewModel();
            DataContext = vm;
            InitializeComponent();
        }

        public RemoteViewModel vm { get; set; }

        private void Current_Suspending(object sender, SuspendingEventArgs e)
        {
            vm.Suspend();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            vm.Resume();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            vm.Suspend();
        }

        private void VirtualKeyboard_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;
            var txt = VirtualKeyboard.Text;
            switch (e.Key)
            {
                case VirtualKey.Enter:
                    txt = "Enter";
                    break;
                case VirtualKey.Back:
                    txt = "Backspace";
                    break;
                case VirtualKey.Up:
                case VirtualKey.Down:
                case VirtualKey.Left:
                case VirtualKey.Right:
                    txt = e.Key.ToString();
                    break;
            }

            _ = vm.PressKeyboardKey(txt);
            VirtualKeyboard.Text = "";
        }
    }
}
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SnDbSizeTesterApp.Profiles;

namespace SnDbSizeTesterApp
{
    /// <summary>
    /// Interaction logic for ProfileWindow.xaml
    /// </summary>
    public partial class ProfileWindow : Window
    {
        private enum WorkingState { Initial, Running, Paused }

        private WorkingState _workingState = WorkingState.Initial;

        public Profile Profile { get; }

        public ProfileWindow(Profile profile)
        {
            InitializeComponent();
            Profile = profile;
            this.DataContext = profile;
            UiSetTitle("");
            ActionCountLabel.Content = "0";
        }

        private void ControlButton_Click(object sender, RoutedEventArgs e)
        {
            switch (_workingState)
            {
                case WorkingState.Initial:
                    ControlButton.Content = "Pause";
                    _workingState = WorkingState.Running;
                    UiSetTitle("(running)");
#pragma warning disable 4014
                    RunAsync();
#pragma warning restore 4014
                    break;
                case WorkingState.Running:
                    ControlButton.Content = "Continue";
                    _workingState = WorkingState.Paused;
                    UiSetTitle("(paused)");
                    break;
                case WorkingState.Paused:
                    ControlButton.Content = "Pause";
                    _workingState = WorkingState.Running;
                    UiSetTitle("(running)");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            //
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _workingState = WorkingState.Initial;
        }

        private int _actionCount;
        private async Task RunAsync()
        {
            while (true)
            {
                if (_workingState == WorkingState.Running)
                {
                    await Profile.Action(CancellationToken.None).ConfigureAwait(false);
#pragma warning disable CS4014
                    Dispatcher.InvokeAsync(() => { ActionCountLabel.Content = ++_actionCount; });
#pragma warning restore CS4014
                }

                if (!Profile.Recurring || _workingState == WorkingState.Initial)
                    break;

                var delay = Profile.WaitMilliseconds;
                if (delay > 0)
                {
                    await Task.Delay(delay).ConfigureAwait(false);
                    if (!Profile.Recurring || _workingState == WorkingState.Initial)
                        break;
                }
            }

            await Dispatcher.InvokeAsync(() =>
            {
                ControlButton.Content = "Start";
                _workingState = WorkingState.Initial;
                UiSetTitle(" (done)");
            });
        }

        private void UiSetTitle(string msg)
        {
            this.Title = $"{Profile.Name}-{Profile.Id} {msg}";
        }
    }
}

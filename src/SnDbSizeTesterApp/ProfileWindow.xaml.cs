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

        private Profile _profile;

        public ProfileWindow(Profile profile)
        {
            InitializeComponent();
            _profile = profile;
            this.DataContext = profile;
            this.Title = _profile.Name;
        }


        private void ControlButton_Click(object sender, RoutedEventArgs e)
        {
            switch (_workingState)
            {
                case WorkingState.Initial:
                    ControlButton.Content = "Pause";
                    _workingState = WorkingState.Running;
                    this.Title = _profile.Name + " (running)";
#pragma warning disable 4014
                    RunAsync();
#pragma warning restore 4014
                    break;
                case WorkingState.Running:
                    ControlButton.Content = "Continue";
                    _workingState = WorkingState.Paused;
                    this.Title = _profile.Name + " (paused)";
                    break;
                case WorkingState.Paused:
                    ControlButton.Content = "Pause";
                    _workingState = WorkingState.Running;
                    this.Title = _profile.Name + " (running)";
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

        private async Task RunAsync()
        {
            while (true)
            {
                if(_workingState == WorkingState.Running)
                    await _profile.Action(CancellationToken.None).ConfigureAwait(false);

                if (!_profile.Recurring || _workingState == WorkingState.Initial)
                    break;

                var delay = _profile.WaitMilliseconds;
                if (delay > 0)
                {
                    await Task.Delay(delay).ConfigureAwait(false);
                    if (!_profile.Recurring || _workingState == WorkingState.Initial)
                        break;
                }
            }

            await Dispatcher.InvokeAsync(() =>
            {
                ControlButton.Content = "Start";
                _workingState = WorkingState.Initial;
                this.Title = _profile.Name + " (done)";
            });
        }
    }
}

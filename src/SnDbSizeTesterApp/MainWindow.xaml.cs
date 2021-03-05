using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using OxyPlot;
using OxyPlot.Series;
using SenseNet.Client;
using SenseNet.Client.Authentication;
using SnDbSizeTesterApp.Models;
using SnDbSizeTesterApp.Profiles;

namespace SnDbSizeTesterApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ServiceProvider _serviceProvider;
        internal ServiceProvider ServiceProvider
        {
            set => _serviceProvider = value;
        }

        private MainViewModel _chartViewModel;

        private string _connectionString;

        private DispatcherTimer _dispatcherTimer;
        public MainWindow()
        {
            InitializeComponent();

            _dispatcherTimer = new DispatcherTimer();
            _dispatcherTimer.Interval = TimeSpan.FromSeconds(2);
            _dispatcherTimer.Tick += DispatcherTimer_Tick;

            PlanLabel.Content = "Plan: ?";
            LogTextBox.Text = "";

            _chartViewModel =  new MainViewModel();
            this.DataContext = _chartViewModel;
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
#pragma warning disable CS4014
            RefreshBarsAsync();
#pragma warning restore CS4014
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            ConnectToRepositoryAsync(UrlComboBox.Text);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        private async Task ConnectToRepositoryAsync(string url)
        {
            await LogTextBox.Dispatcher.InvokeAsync(() =>
            {
                LogTextBox.Clear();
                UIPrint("Connecting...");
                ConnectButton.IsEnabled = false;
                _dispatcherTimer.Stop();
            });

            /* ------------------------------------------------------------- */

            var connPrms = new ConnectionParameters {Url = url, ClientId = "", Secret = ""};
            var window = new LoginWindow(connPrms);
            var result = window.ShowDialog();
            if (!result.HasValue || !result.Value)
            {
                await LogTextBox.Dispatcher.InvokeAsync(() =>
                {
                    UIPrint("Cancelled.");
                    ConnectButton.IsEnabled = true;
                    _dispatcherTimer.Start();
                });
                return;
            }

            _connectionString = connPrms.ConnectionString;

            var server = new ServerContext {Url = url};

            if (string.IsNullOrEmpty(connPrms.ClientId))
            {
                server.Username = "builtin\\admin";
                server.Password = "admin";
            }
            else
            {
                // request and set the access token
                var tokenStore = _serviceProvider.GetService<ITokenStore>();
                server.Authentication.AccessToken =
                    await tokenStore.GetTokenAsync(server, connPrms.ClientId, connPrms.Secret).ConfigureAwait(false);
            }
            ClientContext.Current.AddServer(server);

            /* ------------------------------------------------------------- */

            var dashboardDataSrc = await RESTCaller.GetResponseStringAsync("/Root", "GetDashboardData")
                .ConfigureAwait(false);
            DashboardData dashboardData = null;
            using (var reader = new JsonTextReader(new StringReader(dashboardDataSrc)))
                dashboardData = JsonSerializer.Create().Deserialize<DashboardData>(reader);

            var dbInfo = await GetDatabaseInfoAsync().ConfigureAwait(false);

            await LogTextBox.Dispatcher.InvokeAsync(() =>
            {
                UIPrint("Ok.");
                PlanLabel.Content = dashboardData.Subscription.Plan.Name;
                UIResizeBars(dbInfo, dashboardData);
                ConnectButton.IsEnabled = true;
                _dispatcherTimer.Start();
            });

        }

        private void UIResizeBars(DatabaseInfo dbInfo, DashboardData dashboardData)
        {
            var dataLimit = dbInfo.Database.DataSize;
            var logLimit = dbInfo.Database.LogSize;

            DataBar.Width = 400.0;
            LogBar.Width = logLimit * 400.0 / dataLimit;
            LogPeakBar.Width = LogBar.Width;

            DataBarLabel.Content = $"Data ({dataLimit / 1024.0} MB)";
            LogBarLabel.Content = $"TLog ({logLimit / 1024.0} MB)";

            DataBar.Maximum = 100.0;
            LogBar.Maximum = 100.0;
            LogPeakBar.Maximum = 100.0;

            LogPeakBar.Value = 0.0;

            ContentBar.Maximum = dashboardData.Subscription.Plan.Limitations.ContentCount;
        }


        private Task[] RefreshBarsAsync()
        {
            return new[]
            {
                RefreshBarsByDatabaseInfoAsync(),
                RefreshBarsByDatabaseUsageAsync()
            };
        }

        private async Task RefreshBarsByDatabaseInfoAsync()
        {
            var dbInfo = await GetDatabaseInfoAsync().ConfigureAwait(false);

            _chartViewModel.Advance(new[]
                {dbInfo.Database.DataPercent, dbInfo.Database.UsedLogPercent, GetTempDbAllocatedPercent()});

#pragma warning disable CS4014
            Dispatcher.InvokeAsync(() =>
            {
                DataBar.Value = dbInfo.Database.DataPercent;
                LogBar.Value = dbInfo.Database.UsedLogPercent;
                if (LogBar.Value > LogPeakBar.Value)
                    LogPeakBar.Value = LogBar.Value;
            });
#pragma warning restore CS4014
        }

        private int _refreshBarsByDatabaseUsageCallCounter;
        private async Task RefreshBarsByDatabaseUsageAsync()
        {
            if (++_refreshBarsByDatabaseUsageCallCounter % 3 != 1)
                return;

            var dbUsage = await GetDatabaseUsageAsync().ConfigureAwait(false);
            var contentCount = dbUsage.Content.Count;
#pragma warning disable CS4014
            Dispatcher.InvokeAsync(() =>
            {
                ContentBar.Value = contentCount;
                ContentBarLabel.Content = $"Content: {contentCount}";
            });
#pragma warning restore CS4014
        }


        private async Task<DatabaseInfo> GetDatabaseInfoAsync()
        {
            var databaseInfoSrc = await RESTCaller.GetResponseStringAsync("/Root", "GetDatabaseInfo")
                .ConfigureAwait(false);

            DatabaseInfo dbInfo = null;
            using (var reader = new JsonTextReader(new StringReader(databaseInfoSrc)))
                dbInfo = JsonSerializer.Create().Deserialize<DatabaseInfo>(reader);

            return dbInfo;
        }
        private async Task<DatabaseUsage> GetDatabaseUsageAsync()
        {
            var databaseInfoSrc = await RESTCaller.GetResponseStringAsync(new ODataRequest
                {
                    ActionName = "GetDatabaseUsage",
                    Path = "/Root",
                    Parameters = { { "force", "true" } }
                }).ConfigureAwait(false);

            DatabaseUsage result = null;
            using (var reader = new JsonTextReader(new StringReader(databaseInfoSrc)))
                result = JsonSerializer.Create().Deserialize<DatabaseUsage>(reader);

            return result;
        }

        private void UIPrint(string line)
        {
            LogTextBox.Text += line + Environment.NewLine;
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button) sender;
            CreateProfile(button.Content.ToString());
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ForgetAllProfiles();
        }

        private void ProfileWindow_Closed(object sender, EventArgs e)
        {
            ForgetProfile((Window) sender);
        }
        private void CloseAllProfilesButton_Click(object sender, RoutedEventArgs e)
        {
            ForgetAllProfiles();
        }


        private List<Window> _activeProfiles = new List<Window>();
        private void CreateProfile(string name)
        {
            Profile profile;
            switch (name)
            {
                case "Uploader": profile = new UploaderProfile(); break;
                case "Cleaner": profile = new CleanerProfile(); break;
                case "Editor": profile = new EditorProfile(); break;
                case "Approver": profile = new ApproverProfile(); break;
                //case "Profile5": profile = new Profile5(); break;
                //case "Profile6": profile = new Profile6(); break;
                default:
                    throw new ArgumentException("Unknown profile type: " + name);
            }
            profile._logAction = Log;

            var window = new ProfileWindow(profile);
            _activeProfiles.Add(window);
            window.Closed += ProfileWindow_Closed;
            window.Show();
        }
        private void ForgetProfile(Window window)
        {
            _activeProfiles.Remove(window);
        }
        private void ForgetAllProfiles()
        {
            foreach (var window in _activeProfiles.ToArray())
                window.Close();
        }

        private Task Uploader(CancellationToken cancel)
        {
            LogTextBox.Dispatcher.InvokeAsync(() =>
            {
                LogTextBox.Text += "/";
            });
            return Task.CompletedTask;
        }
        private Task Cleaner(CancellationToken cancel)
        {
            LogTextBox.Dispatcher.InvokeAsync(() =>
            {
                LogTextBox.Text += "\\";
            });
            return Task.CompletedTask;
        }
        private Task Editor(CancellationToken cancel)
        {
            LogTextBox.Dispatcher.InvokeAsync(() =>
            {
                LogTextBox.Text += "_";
            });
            return Task.CompletedTask;
        }
        private Task Approver(CancellationToken cancel)
        {
            LogTextBox.Dispatcher.InvokeAsync(() =>
            {
                LogTextBox.Text += "-";
            });
            return Task.CompletedTask;
        }

        private void Log(string msg)
        {
            Print($"{ DateTime.Now:yyyy-MM-dd HH:mm:ss.ffff}\t{msg}\r\n");
        }
        private void Print(string text)
        {
            LogTextBox.Dispatcher.InvokeAsync(() =>
            {
                LogTextBox.Text += text;
                LogTextBox.ScrollToEnd();
            });
        }

        private void DbTestButton_Click(object sender, RoutedEventArgs e)
        {
            Print($"TempDb Size: {GetTempDbSizeInMB()}\r\n");
            Print($"TempDb allocated: {GetTempDbAllocatedPercent()}%\r\n");
        }
        private double GetTempDbAllocatedPercent()
        {
            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand(@"SELECT CONVERT(real, 
FORMAT(SUM(allocated_extent_page_count) * 100.0 / (SUM(unallocated_extent_page_count) + SUM(allocated_extent_page_count)), 'N2')) [Temp_P]
	FROM tempdb.sys.dm_db_file_space_usage;
", cn))
                {
                    return Convert.ToDouble(cmd.ExecuteScalar());
                }
            }
        }
        private double GetTempDbSizeInMB()
        {
            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand(
                    @"SELECT SUM(size)/128 AS [Total database size (MB)] FROM tempdb.sys.database_files", cn))
                {
                    return Convert.ToDouble(cmd.ExecuteScalar());
                }
            }
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            LogTextBox.Clear();
        }

        private void DbActionButton_Click(object sender, RoutedEventArgs e)
        {
            Log("Emergency action is running...");
            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand(
                    @"USE tempdb
DBCC SHRINKFILE (N'tempdev', 0, TRUNCATEONLY)", cn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            Log("Emergency action finished.");
        }
    }
}

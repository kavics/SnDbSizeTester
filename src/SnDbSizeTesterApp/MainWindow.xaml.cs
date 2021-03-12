using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using SenseNet.Client;
using SenseNet.Client.Authentication;
using SenseNet.Diagnostics;
using SnDbSizeTesterApp.Models;
using SnDbSizeTesterApp.Profiles;
using Path = System.IO.Path;

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
            DatabaseServerTextBox.Text = "";
            VolumeInfoTextBox.Text = "";
            LogTextBox.Text = "";

            _chartViewModel =  new MainViewModel();
            this.DataContext = _chartViewModel;

            InitializeChartDataFile();
        }

        private int _dispatcherTimerTickCount = 0;
        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
#pragma warning disable CS4014
            RefreshBarsAsync();
            if((++_dispatcherTimerTickCount % 5) == 0)
                PreventDatabaseSizeOverflowAsync();
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
                Log("Connecting...");
                ConnectButton.IsEnabled = false;
                _dispatcherTimer.Stop();
            });

            /* ------------------------------------------------------------- */

            var connPrms = new ConnectionParameters {Url = url, ClientId = "", Secret = ""};
            var window = new LoginWindow(connPrms);
            window.Owner = this;
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
                UiResizeBars(dbInfo, dashboardData);
                ConnectButton.IsEnabled = true;
                _dispatcherTimer.Start();
            });
        }
        private void UiResizeBars(DatabaseInfo dbInfo, DashboardData dashboardData)
        {
            var dataLimit = dbInfo.Database.DataSize;
            var logLimit = dbInfo.Database.LogSize;

            //DataBar.Width = 400.0;
            //LogBar.Width = logLimit * 400.0 / dataLimit;
            //LogPeakBar.Width = LogBar.Width;

            DataBarLabel.Content = $"Data ({dataLimit / 1024.0} MB)";
            LogBarLabel.Content = $"TLog ({logLimit / 1024.0} MB)";

            DataBar.Maximum = 100.0;
            LogBar.Maximum = 100.0;
            LogPeakBar.Maximum = 100.0;

            LogPeakBar.Value = 0.0;

            _contentMaxCount = dashboardData.Subscription.Plan.Limitations.ContentCount;
            ContentBar.Maximum = _contentMaxCount;
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

            var dataPercent = dbInfo.Database.DataPercent;
            var logPercent = dbInfo.Database.UsedLogPercent;
            var tempPercent = await GetTempDbAllocatedPercentAsync().ConfigureAwait(false);
            _chartViewModel.Advance(new[]
                {dataPercent, logPercent, tempPercent, _contentPercent});

#pragma warning disable CS4014
            WriteChartDataToFile(dataPercent, logPercent, tempPercent, _contentPercent);

            Dispatcher.InvokeAsync(() =>
            {
                DataBar.Value = dataPercent;
                LogBar.Value = logPercent;
                if (LogBar.Value > LogPeakBar.Value)
                    LogPeakBar.Value = LogBar.Value;
            });
#pragma warning restore CS4014
        }

        private string _chartDataFilePath;
        private void InitializeChartDataFile()
        {
            var asm = Assembly.GetExecutingAssembly();
            var asmPath = asm.Location;
            var dir = Path.Combine(Path.GetDirectoryName(asmPath), "ChartData");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            _chartDataFilePath = Path.Combine(dir, "current-chart.txt");
            using (var writer = new StreamWriter(_chartDataFilePath, false))
                writer.WriteLine("Data\tLog\tTemp\tContent");
        }
        private async Task WriteChartDataToFile(float dataPercent, float logPercent, double tempPercent, double contentPercent)
        {
            using (var writer = new StreamWriter(_chartDataFilePath, true))
                await writer.WriteLineAsync($"{dataPercent}\t{logPercent}\t{tempPercent}\t{contentPercent}");
        }

        private int _refreshBarsByDatabaseUsageCallCounter;
        private int _contentMaxCount;
        private double _contentPercent = 0.0d;
        private async Task RefreshBarsByDatabaseUsageAsync()
        {
            if (++_refreshBarsByDatabaseUsageCallCounter % 3 != 1)
                return;

            var dbUsage = await GetDatabaseUsageAsync().ConfigureAwait(false);
            var contentCount = dbUsage.Content.Count;
            _contentPercent = contentCount * 100.0d / _contentMaxCount;

#pragma warning disable CS4014
            Dispatcher.InvokeAsync(() =>
            {
                ContentBar.Value = contentCount;
                ContentBarLabel.Content = $"Content: {contentCount} / {_contentMaxCount}";
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
            Log(line);
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
            ForgetProfile((ProfileWindow) sender);
        }
        private void CloseAllProfilesButton_Click(object sender, RoutedEventArgs e)
        {
            ForgetAllProfiles();
        }

        private List<ProfileWindow> _activeProfileWindows = new List<ProfileWindow>();
        private void CreateProfile(string name)
        {
            Profile profile;
            switch (name)
            {
                case "Uploader": profile = new UploaderProfile(); break;
                case "Cleaner": profile = new CleanerProfile(); break;
                case "Organizer": profile = new OrganizerProfile(); break;
                case "Approver": profile = new ApproverProfile(); break;
                //case "Profile5": profile = new Profile5(); break;
                //case "Profile6": profile = new Profile6(); break;
                default:
                    throw new ArgumentException("Unknown profile type: " + name);
            }

            profile.Id = GetProfileId(profile);
            profile._logAction = Log;
            profile._logErrorAction = LogError;

            var window = new ProfileWindow(profile);
            window.Closed += ProfileWindow_Closed;
            window.Owner = this;
            _activeProfileWindows.Add(window);
            window.Show();
        }
        private int GetProfileId(Profile profile)
        {
            var ids = _activeProfileWindows.Where(x => x.Profile.Name == profile.Name)
                .Select(x => x.Profile.Id).ToArray();
            if (ids.Length == 0)
                return 1;

            var max = ids.Max();
            for (var id = 1; id < max; id++)
                if (!ids.Contains(id))
                    return id;
            return max + 1;
        }
        private void ForgetProfile(ProfileWindow window)
        {
            _activeProfileWindows.Remove(window);
        }
        private void ForgetAllProfiles()
        {
            foreach (var window in _activeProfileWindows.ToArray())
                window.Close();
        }

        private void Log(string msg)
        {
            Print(false, true, msg);
        }
        private void LogError(Exception e)
        {
            SnTrace.WriteError(e.ToString());
            Print(true, false, e.Message);
        }
        private void Print(bool toDisplay, bool toTrace, string text)
        {
            if(toTrace)
                SnTrace.Write(text);
            if(toDisplay)
            {
                LogTextBox.Dispatcher.InvokeAsync(() =>
                {
                    LogTextBox.Text += text + "\r\n";
                    LogTextBox.ScrollToEnd();
                });
            }
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            LogTextBox.Clear();
        }

        private void DbActionButton_Click(object sender, RoutedEventArgs e)
        {
#pragma warning disable 4014
            EmergencyActionAsync();
#pragma warning restore 4014
        }

        private async Task PreventDatabaseSizeOverflowAsync()
        {
            Log("---- Checking DB size...");
            var volumeInfo = await GetDatabaseVolumeInfoAsync();
            ViewTempDbInfo(volumeInfo);
            Log($"---- Volume size: {volumeInfo.TempDbSizeKb / 1024.0:F3} MB, fill: {volumeInfo.TempDbFillPercent:F1}%," +
                $" peak: {_tempDbSizePeak:F3}, Disk: {volumeInfo.DiskSizeBytes / 1024.0 / 1024.0:F2} " +
                $"({volumeInfo.DiskFreeBytes * 100.0 / volumeInfo.DiskSizeBytes:F1}%)");
            if (volumeInfo.TempDbSizeKb/1024.0d < 30000.0d || volumeInfo.TempDbFillPercent < 50.0d)
                return;

            await EmergencyActionAsync().ConfigureAwait(false);
            volumeInfo = await GetDatabaseVolumeInfoAsync();
            ViewTempDbInfo(volumeInfo);
            Log($"---- Volume size: {volumeInfo.TempDbSizeKb / 1024.0:F3} MB, fill: {volumeInfo.TempDbFillPercent:F1}%," +
                $" peak: {_tempDbSizePeak:F3}, Disk: {volumeInfo.DiskSizeBytes / 1024.0 / 1024.0:F2} " +
                $"({volumeInfo.DiskFreeBytes * 100.0 / volumeInfo.DiskSizeBytes:F1}%)");
        }

        private double _tempDbSizePeak = 0.0d;
        private void ViewTempDbInfo(DatabaseVolumeInfo volumeInfo)
        {
            if (volumeInfo.TempDbSizeKb / 1024.0 > _tempDbSizePeak)
                _tempDbSizePeak = volumeInfo.TempDbSizeKb / 1024.0;

            Dispatcher.InvokeAsync(() =>
            {
                DatabaseServerTextBox.Text = volumeInfo.ServerName;
                VolumeInfoTextBox.Text = $"size: {volumeInfo.TempDbSizeKb/1024.0:F3} MB, fill: {volumeInfo.TempDbFillPercent:F1}%, peak: {_tempDbSizePeak:F3}, Disk: {volumeInfo.DiskSizeBytes/1024.0/1024.0:F2} ({volumeInfo.DiskFreeBytes*100.0/volumeInfo.DiskSizeBytes:F1}%)";
            });
        }

        private async Task<double> GetTempDbAllocatedPercentAsync()
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    cn.Open();
                    using (var cmd = new SqlCommand(@"SELECT CONVERT(real, 
FORMAT(SUM(allocated_extent_page_count) * 100.0 / (SUM(unallocated_extent_page_count) + SUM(allocated_extent_page_count)), 'N2')) [Temp_P]
	FROM tempdb.sys.dm_db_file_space_usage;
", cn))
                    {
                        return Convert.ToDouble(await cmd.ExecuteScalarAsync());
                    }
                }
            }
            catch (Exception e)
            {
                LogError(e);
                throw;
            }
        }

        private string _getDatabaseVolumeInfoScript = @"-- GetDatabaseVolumeInfo
USE [tempdb]
DECLARE @DiskSize bigint DECLARE @DiskFree bigint
SELECT @DiskSize = total_bytes, @DiskFree = available_bytes from sys.dm_os_volume_stats(DB_ID(), 1)
SELECT
	@@SERVERNAME [ServerName], DB_ID() [DbId], DB_NAME() [DbName], 
	(SELECT SUM(allocated_extent_page_count) * 100.0 / (SUM(unallocated_extent_page_count) + SUM(allocated_extent_page_count))
		FROM tempdb.sys.dm_db_file_space_usage) [TempDbFill_Percent],
	(SELECT SUM(size) * 8 FROM tempdb.sys.database_files) [TempDbSize_KB],
	@DiskSize [DiskSize_B], @DiskFree [DiskFree_B]
";
        private async Task<DatabaseVolumeInfo> GetDatabaseVolumeInfoAsync()
        {
            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    cn.Open();
                    using (var cmd = new SqlCommand(_getDatabaseVolumeInfoScript, cn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            await reader.ReadAsync();
                            return new DatabaseVolumeInfo
                            {
                                ServerName = reader.GetString(reader.GetOrdinal("ServerName")),
                                DatabaseName = reader.GetString(reader.GetOrdinal("DbName")),
                                TempDbFillPercent = Convert.ToDouble(reader.GetDecimal(reader.GetOrdinal("TempDbFill_Percent"))),
                                TempDbSizeKb = reader.GetInt32(reader.GetOrdinal("TempDbSize_KB")),
                                DiskSizeBytes = reader.GetInt64(reader.GetOrdinal("DiskSize_B")),
                                DiskFreeBytes = reader.GetInt64(reader.GetOrdinal("DiskFree_B")),
                            };
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogError(e);
                throw;
            }
        }
        private async Task EmergencyActionAsync()
        {
            Log("---- Emergency action is running...");
            using (var cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand(
                    @"USE tempdb
CHECKPOINT;
DBCC FREEPROCCACHE -- clean cache
DBCC DROPCLEANBUFFERS -- clean buffers
DBCC FREESYSTEMCACHE ('ALL') -- clean system cache
DBCC FREESESSIONCACHE -- clean session cache
DBCC SHRINKFILE ('tempdev') -- shrink db file
DBCC SHRINKFILE ('templog') -- shrink log file
", cn))
                {
                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
            Log("---- Emergency action finished.");
        }
    }
}

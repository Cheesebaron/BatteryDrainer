using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Content.Res;
using Android.Net.Wifi;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Support.V7.App;
using DK.Ostebaronen.Droid.BatteryDrainer.Receivers;

namespace DK.Ostebaronen.Droid.BatteryDrainer
{
    [Activity(Label = "BatteryDrainer", MainLauncher = true, Icon = "@drawable/ic_launcher", 
        Theme = "@style/Theme.Batterydrainer", ConfigurationChanges=Android.Content.PM.ConfigChanges.Orientation | 
        Android.Content.PM.ConfigChanges.ScreenSize)]
	public class MainActivity : ActionBarActivity
    {
        private ToggleButton _startStopButton;
        private CheckBox _cpuLoadCheckBox, _bluetoothCheckBox, _wifiCheckBox, _brightnessCheckBox, 
            _vibrateCheckBox;
        private TextView _batteryTemp, _batteryLevel, _batteryHealth, _batteryVoltage;
        private BatteryReceiver _batteryReceiver;
        private PowerManager _powerManager;
        private PowerManager.WakeLock _wakeLock;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.main);

            SetupViews();

            _batteryReceiver = new BatteryReceiver(_batteryTemp, _batteryLevel, _batteryHealth, _batteryVoltage);
            _powerManager = (PowerManager) GetSystemService(PowerService);
            _wakeLock = _powerManager.NewWakeLock(WakeLockFlags.Full, "BatteryDrainer");

            _startStopButton.Click += (s, e) =>
            {
                if (_startStopButton.Checked) //on
                {
                    Start();
                }
                else //off
                {
                    Stop();
                }
            };
        }

        private async void Start()
        {
            if (_brightnessCheckBox.Checked)
            {
                _wakeLock.Acquire();
                IncreaseBrightness();
            }

            if (_bluetoothCheckBox.Checked)
                await StartBluetooth();

            if (_cpuLoadCheckBox.Checked)
                StartCPULoad();

            if (_vibrateCheckBox.Checked)
                await StartVibrate();

            if (_wifiCheckBox.Checked)
                StartWifi();
        }

        private void Stop()
        {
            if (_wakeLock.IsHeld)
                _wakeLock.Release();

            if (_oldBrightness > 0f)
                RestoreBrightness();

            StopBluetooth();

            StopCPULoad();

            StopVibrate();

            StopWifi();

            _startStopButton.Checked = false;
        }

        protected override void OnResume()
        {
            base.OnResume();
            if (_batteryReceiver != null)
                RegisterReceiver(_batteryReceiver, new IntentFilter(Intent.ActionBatteryChanged));
        }

        protected override void OnPause()
        {
            base.OnPause();
            if (_batteryReceiver != null)
            {
                try { UnregisterReceiver(_batteryReceiver); }
                catch { }
            }
            Stop();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.main, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Resource.Id.about)
            {
                StartActivity(typeof(AboutActivity));
                return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        public override void OnConfigurationChanged(Configuration newConfig)
        {
            UnregisterReceiver(_batteryReceiver);

            var bright = _brightnessCheckBox.Checked;
            var wifi = _wifiCheckBox.Checked;
            var cpu = _cpuLoadCheckBox.Checked;
            var vibrate = _vibrateCheckBox.Checked;
            var bt = _bluetoothCheckBox.Checked;
            var startStop = _startStopButton.Checked;

            base.OnConfigurationChanged(newConfig);

            SetContentView(Resource.Layout.main);

            SetupViews();

            _batteryReceiver = new BatteryReceiver(_batteryTemp, _batteryLevel, _batteryHealth, _batteryVoltage);
            RegisterReceiver(_batteryReceiver, new IntentFilter(Intent.ActionBatteryChanged));
            
            _startStopButton.Checked = startStop;
            _startStopButton.Click += (s, e) =>
            {
                if (_startStopButton.Checked) //on
                {
                    Start();
                }
                else //off
                {
                    Stop();
                }
            };

            _brightnessCheckBox.Checked = bright;
            _wifiCheckBox.Checked = wifi;
            _cpuLoadCheckBox.Checked = cpu;
            _vibrateCheckBox.Checked = vibrate;
            _bluetoothCheckBox.Checked = bt;
        }

        private void SetupViews()
        {
            _cpuLoadCheckBox = FindViewById<CheckBox>(Resource.Id.cpu_checkbox);
            _bluetoothCheckBox = FindViewById<CheckBox>(Resource.Id.bt_checkbox);
            _wifiCheckBox = FindViewById<CheckBox>(Resource.Id.wifi_checkbox);
            _brightnessCheckBox = FindViewById<CheckBox>(Resource.Id.bright_checkbox);
            _vibrateCheckBox = FindViewById<CheckBox>(Resource.Id.vibrate_checkbox);

            _batteryTemp = FindViewById<TextView>(Resource.Id.battery_temp_text);
            _batteryLevel = FindViewById<TextView>(Resource.Id.battery_level_text);
            _batteryHealth = FindViewById<TextView>(Resource.Id.battery_health_text);
            _batteryVoltage = FindViewById<TextView>(Resource.Id.battery_voltage_text);
            _startStopButton = FindViewById<ToggleButton>(Resource.Id.start_stop_button);
        }

        #region Brightness
        private float _oldBrightness = float.MinValue;
        private void IncreaseBrightness()
        {
            var layout = Window.Attributes;
            _oldBrightness = layout.ScreenBrightness;
            layout.ScreenBrightness = 1f;
            Window.Attributes = layout;
        }

        private void RestoreBrightness()
        {
            if (_oldBrightness < 0f) return;

            var layout = Window.Attributes;
            layout.ScreenBrightness = _oldBrightness;
            Window.Attributes = layout;
        }
        #endregion

        #region Bluetooth
        private bool _bluetoothWasEnabled;
        private CancellationTokenSource _bluetoothTokenSource;
        private async Task StartBluetooth()
        {
            var adapter = BluetoothAdapter.DefaultAdapter;
            _bluetoothWasEnabled = adapter.IsEnabled;
            if (!adapter.IsEnabled)
                adapter.Enable();

            _bluetoothTokenSource = new CancellationTokenSource();
            try
            {
                await Task.Run(() =>
                {
                
                        var ct = _bluetoothTokenSource.Token;
                        ct.ThrowIfCancellationRequested();

                        do
                        {
                            if (ct.IsCancellationRequested)
                                ct.ThrowIfCancellationRequested();

                            adapter.StartDiscovery();
                        } while (!adapter.IsDiscovering && adapter.IsEnabled);
                
                }, _bluetoothTokenSource.Token);
            }
            catch (TaskCanceledException)
            {
                adapter.CancelDiscovery();
                if (!_bluetoothWasEnabled)
                    adapter.Disable();
            }
        }

        private void StopBluetooth()
        {
            if (_bluetoothTokenSource != null)
                _bluetoothTokenSource.Cancel();
        }
        #endregion

        #region CPU
        private List<Thread> _threads;
        private static bool _loadCPU;
        private void StartCPULoad()
        {
            _loadCPU = true;
            var cpus = System.Environment.ProcessorCount;
            _threads = new List<Thread>();
            for (var i = 0; i < cpus; i++)
            {
                var t = new Thread(CPUKill);
                t.Start(100);
                _threads.Add(t);
            }
        }

        private void StopCPULoad()
        {
            if (_threads == null) return;

            _loadCPU = false;

            foreach (var t in _threads)
            {
                t.Interrupt();
                t.Abort();
            }
                
            _threads = null;
        }

        public static void CPUKill(object cpuUsage)
        {
            Parallel.For(0, 1, i =>
            {
                var watch = new Stopwatch();
                watch.Start();
                while (_loadCPU)
                {
                    if (watch.ElapsedMilliseconds <= (int) cpuUsage) continue;

                    Thread.Sleep(100 - (int) cpuUsage);
                    watch.Reset();
                    watch.Start();
                }
            });
        }
        #endregion

        #region Vibrate
        private bool _vibrate;
        private Task StartVibrate()
        {
            _vibrate = true;
            return Task.Run(() =>
            {
                var vibrator = (Vibrator)GetSystemService(VibratorService);
                if (!vibrator.HasVibrator) return;

                var watch = new Stopwatch();
                watch.Start();

                while (_vibrate)
                {
                    if (watch.ElapsedMilliseconds <= 3000) continue;
                    vibrator.Vibrate(3000);
                    watch.Reset();
                    watch.Start();
                }
            });
        }

        private void StopVibrate()
        {
            _vibrate = false;
        }
        #endregion

        #region Wifi
        private WifiReceiver _wifiReceiver;
        private bool? _wasWifiEnabled;
        private bool _wifiScanning;

        private void StartWifi()
        {
            _wifiScanning = true;

            var wifiManager = (WifiManager) GetSystemService(WifiService);

            _wasWifiEnabled = wifiManager.IsWifiEnabled;

            if (!wifiManager.IsWifiEnabled)
                wifiManager.SetWifiEnabled(true);

            _wifiReceiver = new WifiReceiver(wifiManager) {WifiScanned = () =>
            {
                wifiManager.StartScan();

                if (!_wifiScanning)
                {
                    try { UnregisterReceiver(_wifiReceiver); }
                    catch { }
                }
            }};
            RegisterReceiver(_wifiReceiver, new IntentFilter(WifiManager.ScanResultsAvailableAction));
            wifiManager.StartScan();
        }

        private void StopWifi()
        {
            _wifiScanning = false;

            var wifiManager = (WifiManager) GetSystemService(WifiService);

            if (_wasWifiEnabled.HasValue && !_wasWifiEnabled.Value)
                wifiManager.SetWifiEnabled(false);
        }
        #endregion
    }
}

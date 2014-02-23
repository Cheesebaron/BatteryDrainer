using System;
using Android.Content;
using Android.Net.Wifi;

namespace DK.Ostebaronen.Droid.BatteryDrainer.Receivers
{
    public class WifiReceiver : BroadcastReceiver
    {
        public Action WifiScanned;
        private readonly WifiManager _wifiManager;

        public WifiReceiver(WifiManager manager)
        {
            _wifiManager = manager;
        }

        public override void OnReceive(Context context, Intent intent)
        {
            if (_wifiManager == null) return;

            var results = _wifiManager.ScanResults;
            if (results.Count <= 0) return;

            if (WifiScanned != null)
                WifiScanned.Invoke();
        }
    }
}
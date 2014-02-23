using Android.Content;
using Android.OS;
using Android.Widget;

namespace DK.Ostebaronen.Droid.BatteryDrainer.Receivers
{
    public class BatteryReceiver : BroadcastReceiver
    {
        private readonly TextView _batteryTemp, _batteryLevel, _batteryHealth, _batteryVoltage;

        public BatteryReceiver(TextView batteryTemp, TextView batteryLevel,
            TextView batteryHealth, TextView batteryVoltage)
        {
            _batteryTemp = batteryTemp;
            _batteryLevel = batteryLevel;
            _batteryHealth = batteryHealth;
            _batteryVoltage = batteryVoltage;
        }

        public override void OnReceive(Context context, Intent intent)
        {
            var level = intent.GetIntExtra(BatteryManager.ExtraLevel, -1);
            var scale = intent.GetIntExtra(BatteryManager.ExtraScale, -1);

            var batteryPct = level / (float)scale;

            _batteryLevel.Text = string.Format("{000} %", batteryPct * 100);
            _batteryHealth.Text = ((BatteryHealth)intent.GetIntExtra(BatteryManager.ExtraHealth, 0)).ToString();
            _batteryTemp.Text = string.Format("{0:0.00} C", intent.GetIntExtra(BatteryManager.ExtraTemperature, 0) / 10.0);
            _batteryVoltage.Text = intent.GetIntExtra(BatteryManager.ExtraVoltage, 0) + " mV";
        }
    }
}
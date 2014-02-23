using Android.App;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Text.Method;
using Android.Views;
using Android.Widget;

namespace DK.Ostebaronen.Droid.BatteryDrainer
{
    [Activity(Label = "About", Icon = "@drawable/ic_launcher",
        Theme = "@style/Theme.Batterydrainer")]
    [MetaData("android.support.PARENT_ACTIVITY", Value = "dk.ostebaronen.droid.batterydrainer.MainActivity")]
	public class AboutActivity : ActionBarActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.about);

            ActionBar.SetDisplayHomeAsUpEnabled(true);

            var legal = FindViewById<TextView>(Resource.Id.legal_text);
            legal.MovementMethod = new LinkMovementMethod();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Resource.Id.home)
            {
                NavUtils.NavigateUpFromSameTask(this);
                return true;
            }
                
            return base.OnOptionsItemSelected(item);
        }
    }
}
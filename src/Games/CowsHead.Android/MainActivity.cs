using Android.App;
using Android.Content.PM;
using Android.OS;
using NitroSharp;
using NitroSharp.Launcher;
using System.IO;
using Veldrid;

namespace CowsHead.Android
{
    [Activity(
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.KeyboardHidden | ConfigChanges.Orientation | ConfigChanges.ScreenSize,
        ScreenOrientation = ScreenOrientation.Landscape,
        Theme = "@android:style/Theme.Light.NoTitleBar.Fullscreen"
        )]
    public class MainActivity : Activity
    {
        private const string ConfigFileName = "Game.json";

        private MainView _view;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Directory.SetCurrentDirectory(CacheDir.AbsolutePath);
            CopyAssetsIfNecessary();

            var config = ConfigurationReader.Read(ConfigFileName);
            config.PreferredBackend = GraphicsBackend.OpenGLES;
            _view = new MainView(this);
            var game = new Game(_view, config);
            SetContentView(_view);
            var task = game.Run(useDedicatedThread: true);
        }

        private void CopyAssetsIfNecessary()
        {
            void copy(string relativePath)
            {
                using (Stream asset = Assets.Open(relativePath))
                using (Stream file = File.Create(relativePath))
                {
                    asset.CopyTo(file);
                }
            }

            if (!Directory.Exists("Fonts"))
            {
                Directory.CreateDirectory("Fonts");
                foreach (string font in KnownFonts.Enumerate())
                {
                    copy(font);
                }
            }

            copy(ConfigFileName);
        }

        protected override void OnPause()
        {
            base.OnPause();
            _view.OnPause();
        }

        protected override void OnResume()
        {
            base.OnResume();
            _view.OnResume();
        }
    }
}


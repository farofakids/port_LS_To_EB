namespace MoonDraven
{
    using System;

    using EloBuddy;
    using EloBuddy.SDK.Events;

    internal class Program
    {
        #region Methods

        private static void GameOnOnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.BaseSkinName == "Draven")
            {
                new MoonDraven().Load();
            }
        }

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += GameOnOnGameLoad;
        }

        #endregion
    }
}
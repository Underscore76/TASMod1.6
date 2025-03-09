using System;
using StardewValley;

namespace TASMod.Views
{
    public enum TASView
    {
        Base,
        Map
    }

    public class ViewController
    {
        public TASView CurrentView = TASView.Base;

        public MapView MapView;
        public BaseView BaseView;

        public ViewController()
        {
            BaseView = new BaseView();
            MapView = new MapView();
        }

        public void NextView()
        {
            switch (CurrentView)
            {
                case TASView.Base:
                    SetView(TASView.Map);
                    break;
                case TASView.Map:
                    SetView(TASView.Base);
                    break;
            }
        }

        public void Reset()
        {
            SetView(TASView.Base);
        }

        public void SetView(TASView view)
        {
            if (CurrentView == view)
                return;

            switch (CurrentView)
            {
                case TASView.Base:
                    // baseView.Exit();
                    break;
                case TASView.Map:
                    MapView.Exit();
                    break;
            }

            CurrentView = view;

            switch (CurrentView)
            {
                case TASView.Base:
                    // baseView.Enter();
                    break;
                case TASView.Map:
                    MapView.Enter();
                    break;
            }
        }

        public void ViewLocation(GameLocation location)
        {
            SetView(TASView.Map);
            MapView.SetLocation(location);
        }

        public void Update()
        {
            switch (CurrentView)
            {
                case TASView.Map:
                    MapView.Update();
                    break;
                default:
                    break;
            }
        }

        public void Draw()
        {
            switch (CurrentView)
            {
                case TASView.Map:
                    MapView.Draw();
                    break;
                default:
                    break;
            }
        }
    }
}

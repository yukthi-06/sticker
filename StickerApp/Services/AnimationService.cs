using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;

namespace StickerApp.Services
{
    public class AnimationService
    {
        private readonly LoggerService _logger;
        private readonly JsonStorageService _storage;

        public AnimationService(LoggerService logger, JsonStorageService storage)
        {
            _logger = logger;
            _storage = storage;
        }

        public void ApplyFadeIn(FrameworkElement element, double durationMs = 300)
        {
            if (!_storage.Settings.AnimationsEnabled)
            {
                element.Opacity = 1.0;
                return;
            }

            element.Opacity = 0.0;
            var anim = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(durationMs)
            };

            Storyboard.SetTarget(anim, element);
            Storyboard.SetTargetProperty(anim, "Opacity");

            var sb = new Storyboard();
            sb.Children.Add(anim);
            sb.Begin();
        }

        public void ApplyFadeOut(FrameworkElement element, Action onCompleted, double durationMs = 300)
        {
            if (!_storage.Settings.AnimationsEnabled)
            {
                onCompleted();
                return;
            }

            var anim = new DoubleAnimation
            {
                From = element.Opacity,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(durationMs)
            };

            Storyboard.SetTarget(anim, element);
            Storyboard.SetTargetProperty(anim, "Opacity");

            var sb = new Storyboard();
            sb.Children.Add(anim);
            sb.Completed += (s, e) => onCompleted();
            sb.Begin();
        }
    }
}

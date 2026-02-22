using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace InvoiceGenerator.ViewModels
{
    /// <summary>
    /// Base class for all ViewModels. Provides <see cref="INotifyPropertyChanged"/>
    /// via CommunityToolkit.Mvvm's <see cref="ObservableObject"/>.
    /// </summary>
    public abstract class ViewModelBase : ObservableObject
    {
        /// <summary>
        /// Formats a <see cref="TimeSpan"/> as MM:SS for user display.
        /// </summary>
        public static string FormatDuration(TimeSpan duration)
        {
            var safe = duration > TimeSpan.Zero ? duration : TimeSpan.Zero;
            var totalSeconds = (int)Math.Ceiling(safe.TotalSeconds);
            var minutes = totalSeconds / 60;
            var seconds = totalSeconds % 60;
            return $"{minutes:D2}:{seconds:D2}";
        }
    }
}

using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using System;
using System.Globalization;
using System.IO;

namespace SessionModManagerAvalonia.Classes
{
    public class StreamToBitmapConverter : IValueConverter
    {
        Bitmap? _image;

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Stream)
            {
                if (_image != null)
                {
                    // dispose preview image first
                    _image.Dispose();
                }

                if ((value as Stream).CanRead)
                {
                    using (var stream = (Stream)value)
                    {
                        _image = new Bitmap(stream);
                        return _image;
                    }
                }

            }

            // converter used for the wrong type
            return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}

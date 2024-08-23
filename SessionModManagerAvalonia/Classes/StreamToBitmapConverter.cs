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
            if (value is Stream && (value as Stream).CanRead)
            {
                if (_image != null)
                {
                    // dispose preview image first
                    _image.Dispose();
                }

                using (var stream = (Stream)value)
                {
                    try
                    {
                        _image = new Bitmap(stream);

                    }
                    catch (Exception ex)
                    {
                        return new BindingNotification(ex, BindingErrorType.Error);
                    }

                    return _image;
                }

            }

            if (value == null && _image != null)
            {
                _image.Dispose();
                _image = null;
            }

            return _image;
            // converter used for the wrong type
            //return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}

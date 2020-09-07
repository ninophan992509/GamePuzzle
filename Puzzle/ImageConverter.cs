using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Puzzle
{
    
        public class ImageConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo language)
            {
                try
                {
                    return new BitmapImage(new Uri((string)value));
                }
                catch
                {
                    return new BitmapImage();
                }
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo language)
            {
                throw new NotImplementedException();
            }
        }
    
}

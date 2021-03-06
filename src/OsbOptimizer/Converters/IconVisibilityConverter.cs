﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Milki.OsbOptimizer.Converters
{
    class IconVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var count = (int?)value;
            if (count == null)
                return Visibility.Hidden;
            return count == 0 ? Visibility.Hidden : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
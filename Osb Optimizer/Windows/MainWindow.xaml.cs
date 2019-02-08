﻿using System.Windows;
using Milky.OsbOptimizer.ViewModels;

namespace Milky.OsbOptimizer.Windows
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        internal MainViewModel ViewModel { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel = (MainViewModel)DataContext;
        }
    }
}

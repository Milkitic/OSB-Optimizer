﻿using Microsoft.Win32;
using Milky.OsbOptimizer.Windows;
using Milky.WpfApi;
using Milky.WpfApi.Commands;
using OSharp.Storyboard;
using OSharp.Storyboard.Events;
using OSharp.Storyboard.Management;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Input;
using ErrorEventArgs = OSharp.Storyboard.ErrorEventArgs;

namespace Milky.OsbOptimizer.ViewModels
{
    public class OptimizeViewModel : ViewModelBase, IDisposable
    {
        private float _progress;

        private bool _paused;
        private bool _isRunning;
        private bool _isFinished;

        private ObservableCollection<string> _situationOutput;
        private ObservableCollection<string> _errorOutput;

        private ElementGroup _group;
        private ElementCompressor _compressor;
        private int _tmpProgress;

        public string FilePath { get; }
        public string Name { get; }
        public Guid Guid { get; set; } = Guid.NewGuid();

        internal OptimizeViewModel()
        {

        }
        public OptimizeViewModel(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Path can not be empty.");
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Can not find file: \"{filePath}\"");
            }

            FilePath = filePath;
            Name = Path.GetFileNameWithoutExtension(filePath);
        }

        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                _isRunning = value;
                OnPropertyChanged();
            }
        }
        public bool IsFinished
        {
            get => _isFinished;
            private set
            {
                _isFinished = value;
                OnPropertyChanged();
            }
        }

        public bool Paused
        {
            get => _paused;
            private set
            {
                _paused = value;
                OnPropertyChanged();
            }
        }

        public float Progress
        {
            get => _progress;
            private set
            {
                _progress = value;
                if (_tmpProgress != (int)(_progress * 150))
                {
                    OnPropertyChanged();
                    _tmpProgress = (int)(_progress * 150);
                }
            }
        }

        public ObservableCollection<string> SituationOutput
        {
            get => _situationOutput;
            set
            {
                _situationOutput = value;
                OnPropertyChanged();
            }
        }

        public int? SituationCount => SituationOutput?.Count;

        public ObservableCollection<string> ErrorOutput
        {
            get => _errorOutput;
            set
            {
                _errorOutput = value;
                OnPropertyChanged();
            }
        }
        public int? ErrorCount => ErrorOutput?.Count;

        public ErrorEventArgs HoldingArg { get; set; }

        public ICommand StartCommand
        {
            get
            {
                return new DelegateCommand(async arg =>
                {
                    Dispose();
                    IsFinished = false;
                    IsRunning = true;
                    SituationOutput = new ObservableCollection<string>();
                    ErrorOutput = new ObservableCollection<string>();
                    _group = await ElementGroup.ParseFromFileAsync(FilePath);
                    _compressor = new ElementCompressor(_group)
                    {
                        ThreadCount = 2
                    };
                    var compressor = _compressor;
                    compressor.ErrorOccured += (object sender, ErrorEventArgs e) =>
                    {
                        if (sender is Element element)
                        {
                            ErrorOutput.Add(element + Environment.NewLine + e.Message);
                            OnPropertyChanged(nameof(ErrorCount));
                        }
                        //HoldingArg = e;
                        //HoldingText.AppendLine(e.Message);
                        //OnPropertyChanged(nameof(HoldingText));
                        //Paused = true;
                        //while (Paused)
                        //{
                        //    Thread.Sleep(1);
                        //}
                    };
                    compressor.SituationFound += (object sender, SituationEventArgs e) =>
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine($"Found new element on line {e.Element.RowInSource}:");
                        sb.AppendLine($"    {{{e.Element}}}");

                        if (e.Container != null)
                        {
                            sb.AppendLine($"  Inner container:");
                            sb.AppendLine($"    {{{e.Container}}}");
                        }

                        sb.AppendLine($"  Events involved:");
                        sb.AppendLine(string.Join(Environment.NewLine, e.Events.Select(k => $"    {k}")));
                        sb.AppendLine($"  Message:");
                        sb.AppendLine($"    {e.Message}");
                        Execute.OnUiThread(() => { SituationOutput.Add(sb.ToString()); },
                            MainWindow.SynchronizationContext);

                        OnPropertyChanged(nameof(SituationCount));
                    };
                    compressor.ProgressChanged += (object sender, ProgressEventArgs e) =>
                    {
                        //Thread.Sleep(1);
                        Progress = e.Progress / (float)e.TotalCount;
                    };
                    await _compressor.CompressAsync();
                    await _group.SaveOsbFileAsync(
                        Path.Combine(Path.GetDirectoryName(FilePath),
                            Path.GetFileNameWithoutExtension(FilePath) + ".osb"
                        )
                    );
                    IsRunning = false;
                    IsFinished = true;
                    compressor.ErrorOccured = null;
                    compressor.ProgressChanged = null;
                });
            }
        }

        public ICommand FixCommand
        {
            get
            {
                return new DelegateCommand(arg =>
                {
                    HoldingArg.Continue = true;
                    Paused = false;
                });
            }
        }

        public ICommand KeepCommand
        {
            get
            {
                return new DelegateCommand(arg =>
                {
                    HoldingArg.Continue = false;
                    Paused = false;
                });
            }
        }

        public ICommand RemoveCommand
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    var collection = (ObservableCollection<OptimizeViewModel>)obj;
                    this.Dispose();
                    collection.Remove(this);
                });

            }
        }
        public ICommand OpenInfoCommand
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    var window = new DetailWindow(this);
                    window.Show();
                });

            }
        }
        public void Dispose()
        {
            _group?.Dispose();
            _compressor?.Dispose();
            SituationOutput?.Clear();
            SituationOutput = null;
            ErrorOutput?.Clear();
            ErrorOutput = null;
            OnPropertyChanged(nameof(ErrorCount));
            OnPropertyChanged(nameof(SituationCount));
            HoldingArg = null;
        }
    }
}
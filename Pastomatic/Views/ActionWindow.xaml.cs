using Pastomatic.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Pastomatic.Views
{
    public partial class ActionWindow : Window
    {
        private readonly ActionWindowViewModel _viewModel;
        private readonly int _successDisplayMs;
        private readonly bool _closeOnFocusLoss;

        public ActionWindow(ActionWindowViewModel viewModel, int successDisplayMs, bool closeOnFocusLoss)
        {
            InitializeComponent();

            _viewModel = viewModel;
            _successDisplayMs = successDisplayMs;
            _closeOnFocusLoss = closeOnFocusLoss;

            DataContext = _viewModel;

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            _viewModel.RequestClose += (s, e) => Close();
            _viewModel.RequestDelayedClose += (s, e) => DelayedClose();

            if (_closeOnFocusLoss)
            {
                Deactivated += (s, e) =>
                {
                    if (_viewModel.State != PopupState.Processing)
                        Close();
                };
            }
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ActionWindowViewModel.State))
            {
                UpdateVisualState();
            }
        }

        private void UpdateVisualState()
        {
            PreviewPanel.Visibility = _viewModel.State == PopupState.Preview ? Visibility.Visible : Visibility.Collapsed;
            ProcessingPanel.Visibility = _viewModel.State == PopupState.Processing ? Visibility.Visible : Visibility.Collapsed;
            SuccessPanel.Visibility = _viewModel.State == PopupState.Success ? Visibility.Visible : Visibility.Collapsed;
            ErrorPanel.Visibility = _viewModel.State == PopupState.Error ? Visibility.Visible : Visibility.Collapsed;
        }

        private void DelayedClose()
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_successDisplayMs) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                Close();
            };
            timer.Start();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                _viewModel.CancelCommand.Execute(null);
                e.Handled = true;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel.Cleanup();
            base.OnClosed(e);
        }
    }
}

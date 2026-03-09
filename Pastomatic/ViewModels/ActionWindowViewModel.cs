using Microsoft.Extensions.Logging;
using Pastomatic.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Pastomatic.ViewModels
{
    public enum PopupState
    {
        Preview,
        Processing,
        Success,
        Error
    }

    public class ActionWindowViewModel : ViewModelBase
    {
        private readonly IClipboardImageService _clipboardService;
        private readonly IVisionLlmService _visionService;
        private readonly IImageSaveService _imageSaveService;
        private readonly ISystemTrayService _trayService;
        private readonly ILogger<ActionWindowViewModel> _logger;

        private BitmapSource? _previewImage;
        private byte[]? _pngBytes;
        private PopupState _state = PopupState.Preview;
        private string _statusText = "";
        private string _imageSizeText = "";
        private CancellationTokenSource? _cts;

        public BitmapSource? PreviewImage
        {
            get => _previewImage;
            set => SetProperty(ref _previewImage, value);
        }

        public PopupState State
        {
            get => _state;
            set => SetProperty(ref _state, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public string ImageSizeText
        {
            get => _imageSizeText;
            set => SetProperty(ref _imageSizeText, value);
        }

        public ICommand DescribeCommand { get; }
        public ICommand SaveAndCopyCommand { get; }
        public ICommand CancelCommand { get; }

        public event EventHandler? RequestClose;
        public event EventHandler? RequestDelayedClose;

        public ActionWindowViewModel(
            IClipboardImageService clipboardService,
            IVisionLlmService visionService,
            IImageSaveService imageSaveService,
            ISystemTrayService trayService,
            ILogger<ActionWindowViewModel> logger)
        {
            _clipboardService = clipboardService;
            _visionService = visionService;
            _imageSaveService = imageSaveService;
            _trayService = trayService;
            _logger = logger;

            DescribeCommand = new AsyncRelayCommand(DescribeWithLlmAsync);
            SaveAndCopyCommand = new RelayCommand(SaveAndCopyPath);
            CancelCommand = new RelayCommand(Cancel);
        }

        public void LoadClipboardImage(double maxMegapixels)
        {
            var original = _clipboardService.GetImage();
            if (original == null)
            {
                _logger.LogWarning("No image in clipboard");
                return;
            }

            var resized = _clipboardService.ResizeImage(original, maxMegapixels);
            _pngBytes = _clipboardService.EncodeAsPng(resized);

            PreviewImage = resized;
            ImageSizeText = $"{resized.PixelWidth} x {resized.PixelHeight}  |  {_pngBytes.Length / 1024} KB";
            State = PopupState.Preview;
        }

        private async Task DescribeWithLlmAsync()
        {
            if (_pngBytes == null) return;

            State = PopupState.Processing;
            StatusText = "Describing image...";
            _trayService.UpdateStatus(TrayIconStatus.Processing);
            _cts = new CancellationTokenSource();

            try
            {
                var description = await _visionService.DescribeImageAsync(_pngBytes, _cts.Token);
                _clipboardService.CopyTextToClipboard(description);

                State = PopupState.Success;
                StatusText = "Copied to clipboard!";
                _trayService.UpdateStatus(TrayIconStatus.Success);
                RequestDelayedClose?.Invoke(this, EventArgs.Empty);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Vision LLM request cancelled");
                State = PopupState.Preview;
                StatusText = "";
                _trayService.UpdateStatus(TrayIconStatus.Idle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Vision LLM request failed");
                State = PopupState.Error;
                StatusText = $"Error: {ex.Message}";
                _trayService.UpdateStatus(TrayIconStatus.Error);
            }
        }

        private void SaveAndCopyPath()
        {
            if (_pngBytes == null) return;

            try
            {
                _trayService.UpdateStatus(TrayIconStatus.Processing);

                var windowsPath = _imageSaveService.SaveImage(_pngBytes);
                var wslPath = _imageSaveService.ToWslPath(windowsPath);
                var clipboardText = _imageSaveService.FormatClipboardText(wslPath);

                _clipboardService.CopyTextToClipboard(clipboardText);

                State = PopupState.Success;
                StatusText = "Copied to clipboard!";
                _trayService.UpdateStatus(TrayIconStatus.Success);
                RequestDelayedClose?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save image");
                State = PopupState.Error;
                StatusText = $"Error: {ex.Message}";
                _trayService.UpdateStatus(TrayIconStatus.Error);
            }
        }

        private void Cancel()
        {
            _cts?.Cancel();
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        public void Cleanup()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;

namespace GraphicsEditor.UI.ViewModels;

/// <summary>
/// Main window view model that manages tabs and overall application state.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private CanvasViewModel _canvasViewModel;

    [ObservableProperty]
    private ImageViewerViewModel _imageViewerViewModel;

    [ObservableProperty]
    private ColorConverterViewModel _colorConverterViewModel;

    [ObservableProperty]
    private ThreeDViewModel _threeDViewModel;

    [ObservableProperty]
    private HsvConeViewModel _hsvConeViewModel;

    [ObservableProperty]
    private int _selectedTabIndex;

    public MainWindowViewModel()
    {
        _canvasViewModel = new CanvasViewModel();
        _imageViewerViewModel = new ImageViewerViewModel();
        _colorConverterViewModel = new ColorConverterViewModel();
        _threeDViewModel = new ThreeDViewModel();
        _hsvConeViewModel = new HsvConeViewModel();
        _selectedTabIndex = 0;
    }
}

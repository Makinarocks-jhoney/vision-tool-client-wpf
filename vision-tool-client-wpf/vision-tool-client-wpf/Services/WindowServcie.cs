using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

public sealed class WindowService : IWindowService
{
    static Window? GetWindowByDataContext(object vm)
        => Application.Current.Windows.Cast<Window>()
           .FirstOrDefault(w => ReferenceEquals(w.DataContext, vm));

    static Window? GetActiveWindow()
        => Application.Current.Windows.Cast<Window>()
           .FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow;

    static void OnUI(Action a)
    {
        var d = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        if (d.CheckAccess()) a(); else d.Invoke(a);
    }

    public void Close(object viewModel)
    {
        OnUI(() =>
        {
            var w = GetWindowByDataContext(viewModel);
            w?.Close();
        });
    }

    public void Show<TWindow, TViewModel>(TViewModel vm, Window? owner = null)
        where TWindow : Window, new()
    {
        OnUI(() =>
        {
            var w = new TWindow
            {
                DataContext = vm,
                Owner = owner ?? GetActiveWindow(),
                WindowStartupLocation = (owner is null) ? WindowStartupLocation.CenterScreen
                                                        : WindowStartupLocation.CenterOwner
            };
            w.Show();
        });
    }

    public bool? ShowDialog<TWindow, TViewModel>(TViewModel vm, Window? owner = null)
        where TWindow : Window, new()
    {
        bool? result = null;
        OnUI(() =>
        {
            var w = new TWindow
            {
                DataContext = vm,
                Owner = owner ?? GetActiveWindow(),
                WindowStartupLocation = (owner is null) ? WindowStartupLocation.CenterScreen
                                                        : WindowStartupLocation.CenterOwner
            };
            result = w.ShowDialog();
        });
        return result;
    }


}

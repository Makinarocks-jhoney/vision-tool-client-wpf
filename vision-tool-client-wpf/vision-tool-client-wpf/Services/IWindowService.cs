using System.Windows;

public interface IWindowService
{
    // 현재 ViewModel을 DataContext로 가진 창 닫기
    void Close(object viewModel);

    // 창 열기 (비모달)
    void Show<TWindow, TViewModel>(TViewModel vm, Window? owner = null)
        where TWindow : Window, new();

    // 창 열기 (모달) - DialogResult 반환
    bool? ShowDialog<TWindow, TViewModel>(TViewModel vm, Window? owner = null)
        where TWindow : Window, new();
}

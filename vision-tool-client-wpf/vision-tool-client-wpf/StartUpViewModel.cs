using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace vision_tool_client_wpf
{
    internal class StartUpViewModel
    {

        #region 프로퍼티
        private readonly IWindowService _windows;
        /*
        public int MyVariable
          {
              get { return _myVariable; }
              set { _myVariable = value; RaisePropertyChanged("MyVariable"); }
          }
          private int _myVariable;
          */
        #endregion
        #region 커맨드
        public RelayCommand<object> CommandCancel { get; private set; }
        public RelayCommand<object> CommandOK { get; private set; }
        
        #endregion

        #region 초기화
        public StartUpViewModel(IWindowService windows)
        {
            _windows = windows;
            InitData();
            InitCommand();
            InitEvent();
        }

        void InitData()
        {
            //기존정보가 있으면 불러오기
        }

        void InitCommand()
        {
            CommandCancel = new RelayCommand<object>((param) => OnCommandCancel(param));
            CommandOK = new RelayCommand<object>((param) => OnCommandOK(param));
            
        }

        

        void InitEvent()
        {

        }
        #endregion

        #region 이벤트

        private void OnCommandCancel(object param)
        {
            _windows.Close(this);
        }
        private void OnCommandOK(object? param)
        {
            try
            {
                MainWindow mainWindow = new MainWindow
                {
                    DataContext = Ioc.Default.GetRequiredService<MainWindowViewModel>()
                };
                mainWindow.Show();
                _windows.Close(this);
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        #endregion

    }
}

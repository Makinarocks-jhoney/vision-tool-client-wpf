using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using vision_tool_client_wpf.Settings;


namespace vision_tool_client_wpf
{
    internal sealed class StartUpViewModel:ObservableObject
    {

        #region 프로퍼티
        private readonly IWindowService _windows;

        private GlobalSettingInformation globalSetting { get; set; } = new GlobalSettingInformation();

        public bool IsLogoVisible
        {
            get { return _isLogoVisible; }
            set { _isLogoVisible = value; OnPropertyChanged("IsLogoVisible"); }
        }
        private bool _isLogoVisible = false;
        
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
                var vm = new MainWindowViewModel(_windows, globalSetting);
                MainWindow mainWindow = new MainWindow
                {
                    DataContext = vm
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

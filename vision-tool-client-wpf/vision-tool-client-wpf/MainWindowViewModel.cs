using CommunityToolkit.Mvvm.ComponentModel;
using Minio.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using vision_tool_client_wpf.Service;
using vision_tool_client_wpf.Settings;
using vision_tool_client_wpf.View;

namespace vision_tool_client_wpf
{
    internal sealed class MainWindowViewModel:ObservableObject
    {

        #region 프로퍼티
        private readonly IWindowService _windows;
        private MinIOService _minio { get; set; }
        public RealTimeInspectionViewModel RealTimeInspectionViewModel { get; } = new RealTimeInspectionViewModel();
        public GlobalSettingInformation GlobalSettingInformation { get; set; }

        #endregion
        #region 커맨드
        //public RelayCommand<object> MyCommand { get; private set; }
        #endregion

        #region 초기화
        public MainWindowViewModel(IWindowService windows, GlobalSettingInformation globalSettingInformation)
        {
            _windows = windows;
            GlobalSettingInformation = globalSettingInformation;
            InitData();
            InitCommand();
            InitEvent();
        }

        void InitData()
        {
            _minio = new MinIOService(GlobalSettingInformation.MinIOConfig);
            try
            {
                ConncectMinIO();
            }
            catch (Exception ex)
            {

                
            }
        }

        void InitCommand()
        {
            //MyCommand = new RelayCommand<object>((param) => OnMyCommand(param));
        }

        void InitEvent()
        {

        }
        #endregion

        #region 이벤트

        private async Task<bool> ConncectMinIO()
        {
            try
            {
                await _minio.ConnectAsync();
                await _minio.SeedIndexOnceAsync();
                return true;
            }
            catch (Exception ex)
            {

                return false;
            }
        }

        #endregion

    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vision_tool_client_wpf.View.DataSource
{
    class SearchbarViewModel:ObservableObject
    {

        #region 프로퍼티
        RealTimeInspectionViewModel _realTimeInspectionViewModel { get; set; }
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
        public RelayCommand<object> CommandOpenDataUpload { get; private set; }
        #endregion

        #region 초기화
        public SearchbarViewModel(RealTimeInspectionViewModel realTimeInspectionViewModel)
        {
            _realTimeInspectionViewModel = realTimeInspectionViewModel;
            InitData();
            InitCommand();
            InitEvent();
        }

        void InitData()
        {

        }

        void InitCommand()
        {
            CommandOpenDataUpload = new RelayCommand<object>((param) => OnCommandOpenDataUpload(param));
        }

        void InitEvent()
        {

        }
        #endregion

        #region 이벤트
        private void OnCommandOpenDataUpload(object param)
        {
            _realTimeInspectionViewModel.IsOpenDataUpload = !_realTimeInspectionViewModel.IsOpenDataUpload;
        }
        #endregion

    }
}

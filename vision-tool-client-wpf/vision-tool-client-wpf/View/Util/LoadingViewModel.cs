using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vision_tool_client_wpf.View.Util
{
    class LoadingViewModel
    {

        #region 프로퍼티
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
        //public RelayCommand<object> MyCommand { get; private set; }
        #endregion

        #region 초기화
        public LoadingViewModel()
        {
            InitData();
            InitCommand();
            InitEvent();
        }

        void InitData()
        {

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
        /*
        private void OnMyCommand(object param)
            {

            }
            */
        #endregion

    }
}

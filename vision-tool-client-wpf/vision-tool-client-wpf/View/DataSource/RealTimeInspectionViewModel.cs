using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vision_tool_client_wpf.View.DataSource;

namespace vision_tool_client_wpf.View
{
    internal sealed class RealTimeInspectionViewModel:ObservableObject
    {

        #region 프로퍼티
        public SearchbarViewModel SearchbarViewModel { get; set; }

        public bool IsOpenDataUpload
        {
            get { return _isOpenDataUpload; }
            set { _isOpenDataUpload = value; OnPropertyChanged("IsOpenDataUpload"); }
        }
        private bool _isOpenDataUpload = false;

        #endregion
        #region 커맨드
        
        public RelayCommand<object> CommandOpenFiles { get; private set; }
        #endregion

        #region 초기화
        public RealTimeInspectionViewModel()
        {
            InitData();
            InitCommand();
            InitEvent();
        }

        void InitData()
        {
            SearchbarViewModel = new SearchbarViewModel(this);
        }

        void InitCommand()
        {
            CommandOpenFiles = new RelayCommand<object>((param) => OnCommandOpenFiles(param));
        }

        void InitEvent()
        {

        }
        #endregion

        #region 이벤트
        private void OnCommandOpenFiles(object param)
        {

            Microsoft.Win32.OpenFileDialog Dialog = new Microsoft.Win32.OpenFileDialog();
            Dialog.DefaultExt = ".txt";
            Dialog.Filter = "JPG Files (*.jpg), PNG Files (*.png)|*.jpg;*.png|All Files (*.*)|*.*";
            Dialog.Multiselect = true;
            bool? result = Dialog.ShowDialog();

            if (result == true)
            {

            }


        }
        #endregion

    }
}

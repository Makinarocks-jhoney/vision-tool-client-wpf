using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vision_tool_client_wpf.Settings
{
    class GlobalSettingInformation
    {
        public LoginInformation LoginInformation { get; set; } = new LoginInformation();
        public MinIOConfig MinIOConfig { get; set; } = new MinIOConfig();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vision_tool_client_wpf.Settings
{
    internal class LoginInformation
    {
        string urlKeycloak { get; set; } = "http://192.168.10.17:5173";
        bool modife { get; set; } = true;


        bool login { get; set; } = false;
        string username { get; set; } = "";
        string fullname { get; set; } = "";
    }
}

using System.Configuration;
using System.Data;
using System.Windows;

namespace HRMonitor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static int LoggedInDoctorId { get; set; } = 0;
    }
}

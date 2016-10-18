using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ePSXeQuitter
{
    public partial class ePSXeQuitterNotifyIcon : Component
    {
        public ePSXeQuitterNotifyIcon()
        {
            InitializeComponent();

            toolStripMenuItemExit.Click += toolStripMenuItemExit_Click;
            /*
            notifyIcon.MouseDoubleClick += notifyIcon_MouseDoubleClick;
            */
        }

        void toolStripMenuItemExit_Click(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        /*
        private void notifyIcon_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            menuWindow.Show();
        }
        */

        /*
        public NotifyIconWrapper(IContainer container)
        {
            container.Add(this);

            InitializeComponent();
        }
        */
    }
}

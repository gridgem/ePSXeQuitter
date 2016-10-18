using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ePSXeQuitter
{
    class ePSXeQuitterViewModel
    {
        public ObservableCollection<MenuItem> MenuItems { get; set; }
        public ePSXeQuitterViewModel()
        {
            MenuItems = new ObservableCollection<MenuItem>();
        }
    }

    class MenuItem
    {
        private string path;
        private string content;
        public event EventHandler MouseLeftButtonDownEvent;

        public string Path
        {
            get { return path; }
            set { path = value; }
        }

        public string Content
        {
            get { return content; }
            set { content = value; }
        }

        public void FireCommand()
        {
            MouseLeftButtonDownEvent?.Invoke(this, EventArgs.Empty);
        }
    }
}

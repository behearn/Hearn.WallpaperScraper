using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Hearn.WallpaperScraper
{
    class Command : ICommand
    {
        public event EventHandler CanExecuteChanged;

        Action<object> _executeAction;

        public Command(Action<object> executeAction)
        {
            _executeAction = executeAction;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _executeAction(parameter);
        }
    }
}

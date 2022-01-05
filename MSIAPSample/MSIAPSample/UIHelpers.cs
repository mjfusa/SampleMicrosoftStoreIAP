using MSIAPHelper;
using System;
using Windows.UI.Popups;
using WinRT;

namespace MSIAPSample
{
    public class UIHelpers
    {
        public static async void ShowError(string errorMsg)
        {
            var okCommand = new UICommand("OK", cmd => { return; });
            MessageDialog md = new MessageDialog($"{errorMsg}");

            IInitializeWithWindow initWindow = ((object)md).As<IInitializeWithWindow>();
            var hwnd = (long)System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            initWindow.Initialize(hwnd);

            md.Title = "An error occurred";
            md.Options = MessageDialogOptions.None;
            md.Commands.Add(okCommand);
            await md.ShowAsync();

        }

    }
}

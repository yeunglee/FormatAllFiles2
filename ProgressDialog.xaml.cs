using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace FormatAllFiles2
{
    /// <summary>
    /// A modal WPF progress dialog that blocks VS operations while formatting files.
    /// The dialog cannot be closed by the user — it closes automatically when processing completes.
    /// </summary>
    public partial class ProgressDialog : Window
    {
        #region Win32 API — Hide the close button (X)

        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x00080000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        #endregion

        /// <summary>
        /// Creates a new progress dialog.
        /// </summary>
        /// <param name="title">Window title.</param>
        /// <param name="totalSteps">Total number of items to process (for the progress bar maximum).</param>
        public ProgressDialog(string title, int totalSteps)
        {
            InitializeComponent();
            Title = title;
            FileProgressBar.Maximum = totalSteps;
            SourceInitialized += OnSourceInitialized;
        }

        /// <summary>
        /// Hides the close button (X) by removing the system menu style from the window.
        /// Called once the window handle is available.
        /// </summary>
        private void OnSourceInitialized(object sender, EventArgs e)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int style = GetWindowLong(hwnd, GWL_STYLE);
            SetWindowLong(hwnd, GWL_STYLE, style & ~WS_SYSMENU);
        }

        /// <summary>
        /// Updates the progress bar and status text.
        /// Must be called on the UI thread.
        /// </summary>
        /// <param name="current">Current step (0-based).</param>
        /// <param name="total">Total steps.</param>
        /// <param name="message">Main progress message (e.g. "Formatting files... (3/25)").</param>
        /// <param name="detail">Detail text showing the current file path.</param>
        public void UpdateProgress(int current, int total, string message, string detail)
        {
            MessageText.Text = message;
            FileProgressBar.Maximum = total;
            FileProgressBar.Value = current;
            DetailText.Text = detail ?? string.Empty;
        }

        /// <summary>
        /// Prevents the user from closing the dialog via the system close button or Alt+F4.
        /// The dialog is only closed programmatically when processing completes.
        /// </summary>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Only allow the dialog to close programmatically (when _allowClose is true).
            if (!_allowClose)
            {
                e.Cancel = true;
            }
        }

        private bool _allowClose;

        /// <summary>
        /// Closes the dialog. Only this method can close the window;
        /// user-initiated close attempts (X button, Alt+F4) are blocked.
        /// </summary>
        public void CloseDialog()
        {
            _allowClose = true;
            Close();
        }
    }
}

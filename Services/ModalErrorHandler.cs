namespace VehicleServiceCenter.Services
{
    /// <summary>
    /// Modal Error Handler.
    /// </summary>
    public class ModalErrorHandler : IErrorHandler
    {
        SemaphoreSlim _semaphore = new(1, 1);

        /// <summary>
        /// Handle error in UI.
        /// </summary>
        /// <param name="ex">Exception.</param>
        public void HandleError(Exception ex)
        {
            DisplayAlert(ex).FireAndForgetSafeAsync();
        }
        
        /// <summary>
        /// Display an error message to the user.
        /// </summary>
        /// <param name="message">Error message to display.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        public async Task DisplayError(string message)
        {
            try
            {
                await _semaphore.WaitAsync();
                
                if (OperatingSystem.IsWindows())
                {
                    await AppShell.DisplaySnackbarAsync(message);
                    return;
                }

#if ANDROID || IOS || MACCATALYST
                if (Shell.Current is Shell shell)
                {
                    await shell.DisplayAlert("Error", message, "OK");
                }
#endif
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        /// <summary>
        /// Display a message to the user with a custom title.
        /// </summary>
        /// <param name="message">Message to display.</param>
        /// <param name="title">Title of the message.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        public async Task DisplayMessage(string message, string title)
        {
            try
            {
                await _semaphore.WaitAsync();
                
                if (OperatingSystem.IsWindows())
                {
                    await AppShell.DisplaySnackbarAsync(message);
                    return;
                }

#if ANDROID || IOS || MACCATALYST
                if (Shell.Current is Shell shell)
                {
                    await shell.DisplayAlert(title, message, "OK");
                }
#endif
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Display a success message to the user.
        /// </summary>
        /// <param name="message">Success message to display.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        public async Task DisplaySuccess(string message)
        {
            try
            {
                await _semaphore.WaitAsync();
                
                if (OperatingSystem.IsWindows())
                {
                    await AppShell.DisplaySnackbarAsync(message);
                    return;
                }

#if ANDROID || IOS || MACCATALYST
                if (Shell.Current is Shell shell)
                {
                    await shell.DisplayAlert("Success", message, "OK");
                }
#endif
            }
            finally
            {
                _semaphore.Release();
            }
        }

        async Task DisplayAlert(Exception ex)
        {
            try
            {
                await _semaphore.WaitAsync();
                
                if (OperatingSystem.IsWindows())
                {
                    await AppShell.DisplaySnackbarAsync(ex.Message);
                    return;
                }

#if ANDROID || IOS || MACCATALYST
                if (Shell.Current is Shell shell)
                {
                    await shell.DisplayAlert("Error", ex.Message, "OK");
                }
#endif
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
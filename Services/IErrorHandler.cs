namespace VehicleServiceCenter.Services
{
    /// <summary>
    /// Error Handler Service.
    /// </summary>
    public interface IErrorHandler
    {
        /// <summary>
        /// Handle error in UI.
        /// </summary>
        /// <param name="ex">Exception being thrown.</param>
        void HandleError(Exception ex);
        
        /// <summary>
        /// Display an error message to the user.
        /// </summary>
        /// <param name="message">Error message to display.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        Task DisplayError(string message);
        
        /// <summary>
        /// Display a message to the user with a custom title.
        /// </summary>
        /// <param name="message">Message to display.</param>
        /// <param name="title">Title of the message.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        Task DisplayMessage(string message, string title);
        
        /// <summary>
        /// Display a success message to the user.
        /// </summary>
        /// <param name="message">Success message to display.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        Task DisplaySuccess(string message);
    }
}
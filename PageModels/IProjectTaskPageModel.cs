using CommunityToolkit.Mvvm.Input;
using VehicleServiceCenter.Models;

namespace VehicleServiceCenter.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}
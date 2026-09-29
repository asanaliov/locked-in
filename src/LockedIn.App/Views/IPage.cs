namespace LockedIn.App.Views;

/// <summary>A page of the window. It reads its data once when shown; the window reloads it when the theme changes.</summary>
internal interface IPage
{
    Task LoadAsync();
}

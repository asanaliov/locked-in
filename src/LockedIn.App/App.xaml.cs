using System.Windows;

namespace LockedIn.App;

/// <summary>Stays alive with no window open: closing the window leaves tracking running in the tray.</summary>
public partial class App : Application;

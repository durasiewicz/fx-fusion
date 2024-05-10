using System.Windows.Input;
using Avalonia.Interactivity;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;

namespace FxFusion.Behaviors;
public class KeyDownBehavior : Behavior<Window>
{
    public static readonly StyledProperty<ICommand> CommandProperty =
        AvaloniaProperty.Register<KeyDownBehavior, ICommand>(nameof(Command));

    public ICommand Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.AddHandler(InputElement.KeyDownEvent, KeyDown, RoutingStrategies.Tunnel);
    }

    protected override void OnDetaching()
    {
        AssociatedObject.RemoveHandler(InputElement.KeyDownEvent, KeyDown);
        base.OnDetaching();
    }

    private void KeyDown(object sender, KeyEventArgs e)
    {
        if (Command?.CanExecute(e) == true)
        {
            Command.Execute(e);
        }
    }
}
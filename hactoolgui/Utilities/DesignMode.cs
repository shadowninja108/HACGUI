using System.ComponentModel;
using System.Windows;

/// <summary>
/// WPF Design Mode helper class.
/// Sourced from https://stackoverflow.com/questions/17701545/howto-avoid-a-object-reference-not-set-to-an-instance-of-an-object-exception-i
/// </summary>
public static class DesignMode
{
    private static bool? _isInDesignMode;

    /// <summary>
    /// Gets a value indicating whether the control is in design mode (running in Blend
    /// or Visual Studio).
    /// </summary>
    public static bool IsInDesignMode
    {
        get
        {
            if (!_isInDesignMode.HasValue)
            {
                var prop = DesignerProperties.IsInDesignModeProperty;
                _isInDesignMode
                    = (bool)DependencyPropertyDescriptor
                    .FromProperty(prop, typeof(FrameworkElement))
                    .Metadata.DefaultValue;
            }

            return _isInDesignMode.Value;
        }
    }
}
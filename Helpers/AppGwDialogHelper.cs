using System.Collections.ObjectModel;
using AzureDesktop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AzureDesktop.Helpers;

public static class AppGwDialogHelper
{
    private static readonly Dictionary<string, string[]> ChoiceFields = new()
    {
        ["Protocol"] = ["Http", "Https"],
        ["Cookie Affinity"] = ["Enabled", "Disabled"],
        ["Rule Type"] = ["Basic", "PathBasedRouting"],
        ["Require SNI"] = ["True", "False"],
        ["Pick Host From Backend"] = ["True", "False"],
    };

    public static async Task<bool> ShowAddDialogAsync(
        XamlRoot xamlRoot,
        List<(string Field, string Placeholder)> fields,
        Func<Dictionary<string, string>, bool> addFunc,
        Func<string, Task> saveFunc,
        Action renderFunc)
    {
        if (fields.Count == 0) return false;

        var values = new Dictionary<string, string>();
        foreach (var (field, _) in fields) values[field] = "";

        var editedValues = await ShowFieldDialogAsync(xamlRoot, "Add New", fields, values, isEdit: false);
        if (editedValues is not null)
        {
            var name = editedValues.GetValueOrDefault("Name") ?? "";
            try
            {
                var added = addFunc(editedValues);
                if (added)
                {
                    await saveFunc($"Added '{name}'.");
                }
            }
            catch { }

            renderFunc();
            return true;
        }
        return false;
    }

    public static async Task<bool> ShowEditDialogAsync(
        XamlRoot xamlRoot,
        List<(string Field, string Placeholder)> fields,
        string originalName,
        Dictionary<string, string> values,
        Func<string, Dictionary<string, string>, bool> editFunc,
        Func<string, Task> saveFunc,
        Action renderFunc)
    {
        if (fields.Count == 0) return false;

        var editedValues = await ShowFieldDialogAsync(xamlRoot, $"Edit: {originalName}", fields, values, isEdit: true);
        if (editedValues is not null)
        {
            try
            {
                var edited = editFunc(originalName, editedValues);
                if (edited)
                {
                    await saveFunc($"Updated '{originalName}'.");
                }
            }
            catch { }

            renderFunc();
            return true;
        }
        return false;
    }

    private static async Task<Dictionary<string, string>?> ShowFieldDialogAsync(
        XamlRoot xamlRoot,
        string title,
        List<(string Field, string Placeholder)> fields,
        Dictionary<string, string> values,
        bool isEdit)
    {
        var stack = new StackPanel { Spacing = 12, MinWidth = 400 };
        var controls = new Dictionary<string, object>();

        foreach (var (field, placeholder) in fields)
        {
            var isNameOnEdit = field == "Name" && isEdit;
            var fieldStack = new StackPanel { Spacing = 4 };
            fieldStack.Children.Add(new TextBlock
            {
                Text = field,
                FontSize = 12,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            });

            var currentValue = values.GetValueOrDefault(field) ?? "";

            if (ChoiceFields.TryGetValue(field, out var options) && !isNameOnEdit)
            {
                if (options.Length <= 3)
                {
                    var radioPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };
                    var groupName = $"Radio_{field.Replace(" ", "")}";
                    foreach (var opt in options)
                    {
                        var radio = new RadioButton
                        {
                            Content = opt,
                            GroupName = groupName,
                            IsChecked = opt.Equals(currentValue, StringComparison.OrdinalIgnoreCase),
                        };
                        radioPanel.Children.Add(radio);
                    }

                    if (!radioPanel.Children.OfType<RadioButton>().Any(r => r.IsChecked == true) && radioPanel.Children.Count > 0)
                    {
                        ((RadioButton)radioPanel.Children[0]).IsChecked = true;
                    }

                    controls[field] = radioPanel;
                    fieldStack.Children.Add(radioPanel);
                }
                else
                {
                    var combo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
                    foreach (var opt in options)
                    {
                        combo.Items.Add(opt);
                    }

                    combo.SelectedItem = options.FirstOrDefault(o => o.Equals(currentValue, StringComparison.OrdinalIgnoreCase));
                    if (combo.SelectedItem is null && options.Length > 0)
                    {
                        combo.SelectedIndex = 0;
                    }

                    controls[field] = combo;
                    fieldStack.Children.Add(combo);
                }
            }
            else
            {
                var textBox = new TextBox
                {
                    PlaceholderText = placeholder,
                    Text = currentValue,
                    IsEnabled = !isNameOnEdit,
                };
                controls[field] = textBox;
                fieldStack.Children.Add(textBox);
            }

            stack.Children.Add(fieldStack);
        }

        var dialog = new ContentDialog
        {
            Title = title,
            Content = stack,
            PrimaryButtonText = isEdit ? "Save" : "Add",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot,
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            var editedValues = new Dictionary<string, string>();
            foreach (var (field, _) in fields)
            {
                editedValues[field] = controls[field] switch
                {
                    TextBox tb => tb.Text,
                    ComboBox cb => cb.SelectedItem?.ToString() ?? "",
                    StackPanel rp => rp.Children.OfType<RadioButton>().FirstOrDefault(r => r.IsChecked == true)?.Content?.ToString() ?? "",
                    _ => "",
                };
            }

            return editedValues;
        }

        return null;
    }
}

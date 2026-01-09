using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace VodafoneNumberChecker.Controls
{
    public partial class FilterBadge : UserControl
    {
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(FilterBadge), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register(nameof(IsChecked), typeof(bool?), typeof(FilterBadge), 
                new PropertyMetadata(null, OnIsCheckedChanged));

        public static readonly DependencyProperty CountProperty =
            DependencyProperty.Register(nameof(Count), typeof(int), typeof(FilterBadge), new PropertyMetadata(0));

        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register(nameof(BackgroundColor), typeof(Brush), typeof(FilterBadge), 
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(245, 245, 245))));

        public static readonly DependencyProperty BorderColorProperty =
            DependencyProperty.Register(nameof(BorderColor), typeof(Brush), typeof(FilterBadge), 
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(200, 200, 200))));

        public static readonly DependencyProperty TextColorProperty =
            DependencyProperty.Register(nameof(TextColor), typeof(Brush), typeof(FilterBadge), 
                new PropertyMetadata(new SolidColorBrush(Colors.Black)));

        public static readonly DependencyProperty CountBackgroundColorProperty =
            DependencyProperty.Register(nameof(CountBackgroundColor), typeof(Brush), typeof(FilterBadge), 
                new PropertyMetadata(new SolidColorBrush(Colors.White)));

        public static readonly DependencyProperty CountBorderColorProperty =
            DependencyProperty.Register(nameof(CountBorderColor), typeof(Brush), typeof(FilterBadge), 
                new PropertyMetadata(new SolidColorBrush(Colors.LightGray)));

        public static readonly DependencyProperty CountTextColorProperty =
            DependencyProperty.Register(nameof(CountTextColor), typeof(Brush), typeof(FilterBadge), 
                new PropertyMetadata(new SolidColorBrush(Colors.Black)));

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public bool? IsChecked
        {
            get => (bool?)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

        public int Count
        {
            get => (int)GetValue(CountProperty);
            set => SetValue(CountProperty, value);
        }

        public Brush BackgroundColor
        {
            get => (Brush)GetValue(BackgroundColorProperty);
            set => SetValue(BackgroundColorProperty, value);
        }

        public Brush BorderColor
        {
            get => (Brush)GetValue(BorderColorProperty);
            set => SetValue(BorderColorProperty, value);
        }

        public Brush TextColor
        {
            get => (Brush)GetValue(TextColorProperty);
            set => SetValue(TextColorProperty, value);
        }

        public Brush CountBackgroundColor
        {
            get => (Brush)GetValue(CountBackgroundColorProperty);
            set => SetValue(CountBackgroundColorProperty, value);
        }

        public Brush CountBorderColor
        {
            get => (Brush)GetValue(CountBorderColorProperty);
            set => SetValue(CountBorderColorProperty, value);
        }

        public Brush CountTextColor
        {
            get => (Brush)GetValue(CountTextColorProperty);
            set => SetValue(CountTextColorProperty, value);
        }

        public FilterBadge()
        {
            InitializeComponent();
            Loaded += (s, e) => 
            {
                // Initialize checkbox to show null as unchecked
                if (checkBox != null)
                {
                    checkBox.IsChecked = IsChecked ?? false;
                }
                UpdateVisualState();
            };
        }

        private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FilterBadge)d;
            // Sync checkbox with property value when property changes from ViewModel
            if (control.checkBox != null)
            {
                var newValue = e.NewValue as bool?;
                var displayValue = newValue ?? false; // Show null as unchecked
                // Only update if different to avoid circular updates
                if (control.checkBox.IsChecked != displayValue)
                {
                    control.checkBox.IsChecked = displayValue;
                }
            }
            control.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            if (mainBorder == null) return;

            if (IsChecked.HasValue && IsChecked.Value)
            {
                // Active state - use full background color with stronger border
                mainBorder.Background = BackgroundColor;
                mainBorder.BorderBrush = BorderColor;
                mainBorder.BorderThickness = new Thickness(2);
                mainBorder.Opacity = 1.0;
            }
            else
            {
                // Unchecked state (null or false) - very light, appears as unchecked
                var bgColor = BackgroundColor as SolidColorBrush;
                if (bgColor != null)
                {
                    var color = bgColor.Color;
                    mainBorder.Background = new SolidColorBrush(Color.FromArgb(50, color.R, color.G, color.B));
                }
                else
                {
                    mainBorder.Background = new SolidColorBrush(Color.FromArgb(50, 245, 245, 245));
                }
                mainBorder.BorderBrush = new SolidColorBrush(Colors.LightGray);
                mainBorder.BorderThickness = new Thickness(1);
                mainBorder.Opacity = 0.6;
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            
            if (e.Property == IsCheckedProperty)
            {
                // Sync checkbox display (null shows as unchecked)
                if (checkBox != null)
                {
                    var newValue = e.NewValue as bool?;
                    var displayValue = newValue ?? false; // Show null as unchecked
                    // Only update if different to avoid circular updates
                    if (checkBox.IsChecked != displayValue)
                    {
                        checkBox.IsChecked = displayValue;
                    }
                }
                UpdateVisualState();
            }
            else if (e.Property == BackgroundColorProperty || 
                     e.Property == BorderColorProperty)
            {
                UpdateVisualState();
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // Update the property - the binding will propagate to ViewModel
            IsChecked = true;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            // When unchecked, set to null (not filtered) instead of false
            IsChecked = null;
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Toggle checkbox when clicking anywhere on the badge
            // Cycle through: null (unchecked) -> true (checked) -> null (unchecked)
            if (!IsChecked.HasValue || (IsChecked.HasValue && !IsChecked.Value))
            {
                IsChecked = true;
            }
            else if (IsChecked.HasValue && IsChecked.Value)
            {
                IsChecked = null; // Go back to null (unchecked)
            }
            // Sync checkbox display
            if (checkBox != null)
            {
                checkBox.IsChecked = IsChecked ?? false; // Show null as unchecked
            }
            UpdateVisualState();
        }
    }
}

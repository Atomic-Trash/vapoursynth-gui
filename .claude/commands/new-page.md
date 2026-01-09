Generate a new page with MVVM structure following project patterns.

## Arguments
- `<PageName>` - Name for the new page (e.g., "Settings", "Plugins") - required

## Instructions

1. **Validate Name**
   - Ensure name is PascalCase
   - Check it doesn't conflict with existing pages
   - Name should not end with "Page" (it will be added automatically)

2. **Create Page XAML**
   Create `src/gui/VapourSynthPortable/Pages/<PageName>Page.xaml`:

   ```xml
   <Page x:Class="VapourSynthPortable.Pages.<PageName>Page"
         xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
         xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
         xmlns:vm="clr-namespace:VapourSynthPortable.ViewModels"
         Title="<PageName>">

       <Page.DataContext>
           <vm:<PageName>ViewModel />
       </Page.DataContext>

       <Grid Background="{StaticResource BackgroundBrush}">
           <Grid.RowDefinitions>
               <RowDefinition Height="Auto"/>
               <RowDefinition Height="*"/>
           </Grid.RowDefinitions>

           <!-- Header -->
           <Border Grid.Row="0" Background="{StaticResource SurfaceBrush}" Padding="16">
               <TextBlock Text="<PageName>"
                          Style="{StaticResource HeaderTextStyle}"
                          FontSize="24" FontWeight="SemiBold"/>
           </Border>

           <!-- Content -->
           <Grid Grid.Row="1" Margin="16">
               <TextBlock Text="<PageName> page content goes here"
                          Foreground="{StaticResource TextSecondaryBrush}"/>
           </Grid>
       </Grid>
   </Page>
   ```

3. **Create Page Code-Behind**
   Create `src/gui/VapourSynthPortable/Pages/<PageName>Page.xaml.cs`:

   ```csharp
   using System.Windows.Controls;

   namespace VapourSynthPortable.Pages;

   public partial class <PageName>Page : Page
   {
       public <PageName>Page()
       {
           InitializeComponent();
       }
   }
   ```

4. **Create ViewModel**
   Create `src/gui/VapourSynthPortable/ViewModels/<PageName>ViewModel.cs`:

   ```csharp
   using CommunityToolkit.Mvvm.ComponentModel;
   using CommunityToolkit.Mvvm.Input;
   using Serilog;

   namespace VapourSynthPortable.ViewModels;

   public partial class <PageName>ViewModel : ObservableObject
   {
       private readonly ILogger _logger = Log.ForContext<<PageName>ViewModel>();

       [ObservableProperty]
       private string _statusText = "Ready";

       public <PageName>ViewModel()
       {
           _logger.Information("<PageName>ViewModel initialized");
       }
   }
   ```

5. **Register ViewModel in DI**
   Update `src/gui/VapourSynthPortable/App.xaml.cs`:
   - Add to ConfigureServices: `services.AddTransient<<PageName>ViewModel>();`

6. **Add Navigation**
   Update `src/gui/VapourSynthPortable/MainWindow.xaml`:
   - Add RadioButton in navigation menu
   - Add Page element in content grid (initially collapsed)

   Update `src/gui/VapourSynthPortable/MainWindow.xaml.cs`:
   - Add page field and instantiation
   - Add visibility toggle in navigation handler

7. **Summary Report**
   ```
   Created new page: <PageName>
   ═══════════════════════════════════════

   Files created:
   ✓ Pages/<PageName>Page.xaml
   ✓ Pages/<PageName>Page.xaml.cs
   ✓ ViewModels/<PageName>ViewModel.cs

   Files updated:
   ✓ App.xaml.cs (DI registration)
   ✓ MainWindow.xaml (navigation + page)
   ✓ MainWindow.xaml.cs (navigation logic)

   Next steps:
   1. Add your page content to <PageName>Page.xaml
   2. Add properties and commands to <PageName>ViewModel.cs
   3. Build and test navigation
   ```

## Example

```bash
# Create a Settings page
/new-page Settings

# Create a Plugins management page
/new-page Plugins
```

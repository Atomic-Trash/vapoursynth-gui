Generate a new service with proper patterns following project conventions.

## Arguments
- `<ServiceName>` - Name for the new service (e.g., "Cache", "Notification") - required
- `--singleton` - Register as singleton instead of transient (optional)

## Instructions

1. **Validate Name**
   - Ensure name is PascalCase
   - Check it doesn't conflict with existing services
   - Name should not end with "Service" (it will be added automatically)

2. **Create Service Class**
   Create `src/gui/VapourSynthPortable/Services/<ServiceName>Service.cs`:

   ```csharp
   using Serilog;

   namespace VapourSynthPortable.Services;

   /// <summary>
   /// Provides <ServiceName> functionality for the application.
   /// </summary>
   public class <ServiceName>Service : IDisposable
   {
       private readonly ILogger _logger = Log.ForContext<<ServiceName>Service>();
       private bool _disposed;

       public <ServiceName>Service()
       {
           _logger.Information("<ServiceName>Service initialized");
       }

       // TODO: Add your service methods here
       // Example:
       // public async Task<Result> DoSomethingAsync(CancellationToken ct = default)
       // {
       //     _logger.Debug("Doing something...");
       //     // Implementation
       // }

       public void Dispose()
       {
           Dispose(true);
           GC.SuppressFinalize(this);
       }

       protected virtual void Dispose(bool disposing)
       {
           if (_disposed) return;

           if (disposing)
           {
               // Dispose managed resources
               _logger.Debug("<ServiceName>Service disposed");
           }

           _disposed = true;
       }
   }
   ```

3. **Register in DI Container**
   Update `src/gui/VapourSynthPortable/App.xaml.cs`:

   For transient (default):
   ```csharp
   services.AddTransient<<ServiceName>Service>();
   ```

   For singleton (--singleton flag):
   ```csharp
   services.AddSingleton<<ServiceName>Service>();
   ```

4. **Add Usage Example**
   Show how to inject and use the service in a ViewModel:

   ```csharp
   public partial class SomeViewModel : ObservableObject
   {
       private readonly <ServiceName>Service _<serviceName>Service;

       public SomeViewModel(<ServiceName>Service <serviceName>Service)
       {
           _<serviceName>Service = <serviceName>Service;
       }

       [RelayCommand]
       private async Task DoWorkAsync()
       {
           // Use the service
           // await _<serviceName>Service.DoSomethingAsync();
       }
   }
   ```

5. **Summary Report**
   ```
   Created new service: <ServiceName>Service
   ═══════════════════════════════════════════

   Files created:
   ✓ Services/<ServiceName>Service.cs

   Files updated:
   ✓ App.xaml.cs (DI registration as <transient|singleton>)

   Next steps:
   1. Add your service methods to <ServiceName>Service.cs
   2. Inject the service into ViewModels that need it
   3. Add tests to VapourSynthPortable.Tests

   Usage example:
   ──────────────
   public class MyViewModel
   {
       private readonly <ServiceName>Service _service;

       public MyViewModel(<ServiceName>Service service)
       {
           _service = service;
       }
   }
   ```

## Service Patterns in This Project

### Common Service Features
- **Logging**: Use `Serilog.Log.ForContext<T>()` for typed logging
- **Async**: Use async/await with CancellationToken support
- **Events**: Use EventHandler or custom delegates for callbacks
- **Disposal**: Implement IDisposable for resource cleanup

### Existing Services Reference
| Service | Type | Purpose |
|---------|------|---------|
| MediaPoolService | Singleton | Centralized media management |
| ThumbnailService | Singleton | Thumbnail generation and caching |
| FrameCacheService | Singleton | Frame caching for preview |
| FFmpegService | Transient | FFmpeg encoding operations |
| VapourSynthService | Transient | VapourSynth script execution |
| SettingsService | Singleton | Application settings |
| LoggingService | Singleton | Logging infrastructure |
| ProjectService | Transient | Project save/load |

## Examples

```bash
# Create a transient service
/new-service Notification

# Create a singleton service (for shared state)
/new-service --singleton Cache

# Create a processing service
/new-service Transcoding
```

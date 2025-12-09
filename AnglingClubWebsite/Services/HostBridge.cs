// TODO Ang to Blazor Migration - whole file only needed during migration
using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;

public class HostBridge : IAsyncDisposable
{
  private readonly IJSRuntime _js;
  private readonly NavigationManager _nav;
  private DotNetObjectReference<HostBridge>? _objRef;

  public event Action<string>? NavigateRequested;
  public event Action<string?, object?>? AuthUpdated;

  public HostBridge(IJSRuntime js, NavigationManager nav)
  {
    _js = js;
    _nav = nav;
  }

  public async Task InitializeAsync()
  {
    _objRef = DotNetObjectReference.Create(this);
    await _js.InvokeVoidAsync("blazorHost.start", _objRef);
  }

  [JSInvokable]
  public Task HandleNavigate(string path)
  {
    Console.WriteLine($"HostBridge: Navigate to {path}");
    // Normal Blazor navigation (embedded layout will still apply)
    if(string.IsNullOrWhiteSpace(path))
        path = "/";

    if (!path.StartsWith("/"))
      path = "/" + path;

    // Look at the current URI to see if we're in "embedded" mode
    var currentUri = new Uri(_nav.Uri);
    var query = currentUri.Query;      // e.g. "?embedded=true" or ""

    var target = string.IsNullOrEmpty(query)
        ? path
        : $"{path}{query}";           // "/news?embedded=true"

    _nav.NavigateTo(target, forceLoad: false);
    NavigateRequested?.Invoke(path);
    return Task.CompletedTask;
  }

  [JSInvokable]
  public Task HandleAuth(string? token, object? user)
  {
    AuthUpdated?.Invoke(token, user);
    // You can also stash token in a service here
    return Task.CompletedTask;
  }

  public async ValueTask DisposeAsync()
  {
    _objRef?.Dispose();
    await Task.CompletedTask;
  }
}

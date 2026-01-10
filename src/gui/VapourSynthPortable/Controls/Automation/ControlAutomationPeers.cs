using System.Collections.Generic;
using System.Windows.Automation.Peers;

namespace VapourSynthPortable.Controls.Automation;

/// <summary>
/// AutomationPeer for VideoPlayerControl to enable UI Automation accessibility.
/// Returns empty children list to prevent HwndHost from blocking UI Automation tree traversal.
/// </summary>
public class VideoPlayerControlAutomationPeer : FrameworkElementAutomationPeer
{
    public VideoPlayerControlAutomationPeer(VideoPlayerControl owner) : base(owner) { }

    protected override string GetClassNameCore() => nameof(VideoPlayerControl);

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;

    protected override string GetLocalizedControlTypeCore() => "Video Player";

    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrEmpty(name) ? "Video Player" : name;
    }

    protected override bool IsContentElementCore() => true;

    protected override bool IsControlElementCore() => true;

    /// <summary>
    /// Returns an empty list to prevent UI Automation from traversing into the HwndHost
    /// which can cause timeouts and hangs during automation tree enumeration.
    /// </summary>
    protected override List<AutomationPeer> GetChildrenCore() => new();
}

/// <summary>
/// AutomationPeer for ScopesControl to enable UI Automation accessibility.
/// Returns empty children list to prevent potential UI Automation tree traversal issues.
/// </summary>
public class ScopesControlAutomationPeer : FrameworkElementAutomationPeer
{
    public ScopesControlAutomationPeer(ScopesControl owner) : base(owner) { }

    protected override string GetClassNameCore() => nameof(ScopesControl);

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;

    protected override string GetLocalizedControlTypeCore() => "Video Scopes";

    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrEmpty(name) ? "Video Scopes" : name;
    }

    protected override bool IsContentElementCore() => true;

    protected override bool IsControlElementCore() => true;

    /// <summary>
    /// Returns an empty list to prevent potential UI Automation tree traversal issues.
    /// </summary>
    protected override List<AutomationPeer> GetChildrenCore() => new();
}

/// <summary>
/// AutomationPeer for ColorWheelControl to enable UI Automation accessibility.
/// Returns empty children list to prevent potential UI Automation tree traversal issues.
/// </summary>
public class ColorWheelControlAutomationPeer : FrameworkElementAutomationPeer
{
    private readonly ColorWheelControl _owner;

    public ColorWheelControlAutomationPeer(ColorWheelControl owner) : base(owner)
    {
        _owner = owner;
    }

    protected override string GetClassNameCore() => nameof(ColorWheelControl);

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;

    protected override string GetLocalizedControlTypeCore() => "Color Wheel";

    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        if (!string.IsNullOrEmpty(name)) return name;

        // Use the Title property if available
        return _owner.Title ?? "Color Wheel";
    }

    protected override bool IsContentElementCore() => true;

    protected override bool IsControlElementCore() => true;

    /// <summary>
    /// Returns an empty list to prevent potential UI Automation tree traversal issues.
    /// </summary>
    protected override List<AutomationPeer> GetChildrenCore() => new();
}

/// <summary>
/// AutomationPeer for CurvesControl to enable UI Automation accessibility.
/// Returns empty children list to prevent potential UI Automation tree traversal issues.
/// </summary>
public class CurvesControlAutomationPeer : FrameworkElementAutomationPeer
{
    public CurvesControlAutomationPeer(CurvesControl owner) : base(owner) { }

    protected override string GetClassNameCore() => nameof(CurvesControl);

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;

    protected override string GetLocalizedControlTypeCore() => "Curves Editor";

    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrEmpty(name) ? "Curves Editor" : name;
    }

    protected override bool IsContentElementCore() => true;

    protected override bool IsControlElementCore() => true;

    /// <summary>
    /// Returns an empty list to prevent potential UI Automation tree traversal issues.
    /// </summary>
    protected override List<AutomationPeer> GetChildrenCore() => new();
}

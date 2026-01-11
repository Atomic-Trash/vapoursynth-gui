using VapourSynthPortable.Models;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.Tests.Services;

public class EffectServiceTests
{
    #region AvailableEffects Tests

    [Fact]
    public void AvailableEffects_ReturnsNonEmptyList()
    {
        // Arrange
        var service = new EffectService();

        // Act
        var effects = service.AvailableEffects;

        // Assert
        Assert.NotNull(effects);
        Assert.NotEmpty(effects);
    }

    [Fact]
    public void AvailableEffects_ContainsResizeEffects()
    {
        // Arrange
        var service = new EffectService();

        // Act
        var effects = service.AvailableEffects;

        // Assert
        Assert.Contains(effects, e => e.Name.Contains("Resize") && e.Category == "Resize");
    }

    [Fact]
    public void AvailableEffects_ContainsDenoiseEffects()
    {
        // Arrange
        var service = new EffectService();

        // Act
        var effects = service.AvailableEffects;

        // Assert
        Assert.Contains(effects, e => e.Category == "Denoise");
    }

    [Fact]
    public void AvailableEffects_ContainsTransformEffects()
    {
        // Arrange
        var service = new EffectService();

        // Act
        var effects = service.AvailableEffects;

        // Assert
        Assert.Contains(effects, e => e.Name == "Crop" && e.Category == "Transform");
        Assert.Contains(effects, e => e.Name == "Flip Horizontal");
        Assert.Contains(effects, e => e.Name == "Flip Vertical");
    }

    #endregion

    #region EffectsByCategory Tests

    [Fact]
    public void EffectsByCategory_GroupsEffectsCorrectly()
    {
        // Arrange
        var service = new EffectService();

        // Act
        var groups = service.EffectsByCategory.ToList();

        // Assert
        Assert.NotEmpty(groups);
        Assert.Contains(groups, g => g.Key == "Resize");
        Assert.Contains(groups, g => g.Key == "Transform");
        Assert.Contains(groups, g => g.Key == "Denoise");
    }

    [Fact]
    public void EffectsByCategory_AllEffectsInGroups()
    {
        // Arrange
        var service = new EffectService();

        // Act
        var totalInGroups = service.EffectsByCategory.Sum(g => g.Count());
        var totalEffects = service.AvailableEffects.Count;

        // Assert
        Assert.Equal(totalEffects, totalInGroups);
    }

    #endregion

    #region GetEffectDefinition Tests

    [Fact]
    public void GetEffectDefinition_ReturnsDefinitionByName()
    {
        // Arrange
        var service = new EffectService();

        // Act
        var effect = service.GetEffectDefinition("Crop");

        // Assert
        Assert.NotNull(effect);
        Assert.Equal("Crop", effect.Name);
        Assert.Equal("Transform", effect.Category);
    }

    [Fact]
    public void GetEffectDefinition_ReturnsNullForUnknownEffect()
    {
        // Arrange
        var service = new EffectService();

        // Act
        var effect = service.GetEffectDefinition("NonExistentEffect");

        // Assert
        Assert.Null(effect);
    }

    [Theory]
    [InlineData("Resize (Lanczos)", "resize", "Lanczos")]
    [InlineData("BM3D Denoise", "bm3d", "VAggregate")]
    [InlineData("Flip Horizontal", "std", "FlipHorizontal")]
    public void GetEffectDefinition_HasCorrectVsMetadata(string effectName, string expectedNamespace, string expectedFunction)
    {
        // Arrange
        var service = new EffectService();

        // Act
        var effect = service.GetEffectDefinition(effectName);

        // Assert
        Assert.NotNull(effect);
        Assert.Equal(expectedNamespace, effect.VsNamespace);
        Assert.Equal(expectedFunction, effect.VsFunction);
    }

    #endregion

    #region CreateEffect Tests

    [Fact]
    public void CreateEffect_ByName_ReturnsTimelineEffect()
    {
        // Arrange
        var service = new EffectService();

        // Act
        var effect = service.CreateEffect("Crop");

        // Assert
        Assert.NotNull(effect);
        Assert.Equal("Crop", effect.Name);
    }

    [Fact]
    public void CreateEffect_ByName_InitializesParameters()
    {
        // Arrange
        var service = new EffectService();

        // Act
        var effect = service.CreateEffect("Resize (Lanczos)");

        // Assert
        Assert.NotNull(effect);
        Assert.NotEmpty(effect.Parameters);
        Assert.Contains(effect.Parameters, p => p.Name == "width");
        Assert.Contains(effect.Parameters, p => p.Name == "height");
    }

    [Fact]
    public void CreateEffect_ByName_ThrowsForUnknownEffect()
    {
        // Arrange
        var service = new EffectService();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.CreateEffect("NonExistentEffect"));
    }

    [Fact]
    public void CreateEffect_ByDefinition_ReturnsTimelineEffect()
    {
        // Arrange
        var service = new EffectService();
        var definition = service.GetEffectDefinition("Crop")!;

        // Act
        var effect = service.CreateEffect(definition);

        // Assert
        Assert.NotNull(effect);
        Assert.Equal("Crop", effect.Name);
    }

    #endregion

    #region Preset Tests

    [Fact]
    public void SavePreset_StoresPreset()
    {
        // Arrange
        var service = new EffectService();
        var effect = service.CreateEffect("BM3D Denoise");
        effect.Parameters.First(p => p.Name == "sigma").Value = 8.0;

        // Act
        service.SavePreset("Custom BM3D", effect);
        var presets = service.GetPresetsForEffect("BM3D Denoise");

        // Assert
        Assert.Contains(presets, p => p.Name == "Custom BM3D");
    }

    [Fact]
    public void ApplyPreset_SetsEffectParameters()
    {
        // Arrange
        var service = new EffectService();
        var effect = service.CreateEffect("BM3D Denoise");

        // Act - Apply built-in preset
        service.ApplyPreset("BM3D - Strong", effect);

        // Assert
        var sigmaParam = effect.Parameters.First(p => p.Name == "sigma");
        Assert.Equal(10.0, sigmaParam.Value);
    }

    [Fact]
    public void GetPresetsForEffect_ReturnsMatchingPresets()
    {
        // Arrange
        var service = new EffectService();

        // Act
        var bm3dPresets = service.GetPresetsForEffect("BM3D Denoise").ToList();
        var resizePresets = service.GetPresetsForEffect("Resize (Lanczos)").ToList();

        // Assert
        Assert.NotEmpty(bm3dPresets);
        Assert.All(bm3dPresets, p => Assert.Equal("BM3D Denoise", p.EffectName));
        Assert.NotEmpty(resizePresets);
    }

    [Fact]
    public void BuiltInPresets_Include1080p()
    {
        // Arrange
        var service = new EffectService();

        // Act
        var presets = service.GetPresetsForEffect("Resize (Lanczos)");

        // Assert
        Assert.Contains(presets, p => p.Name == "1080p");
    }

    #endregion

    #region GenerateEffectChainCode Tests

    [Fact]
    public void GenerateEffectChainCode_EmptyEffects_ReturnsEmptyString()
    {
        // Arrange
        var service = new EffectService();
        var clip = new TimelineClip { Id = 1 };

        // Act
        var code = service.GenerateEffectChainCode(clip, "input");

        // Assert
        Assert.Equal("", code);
    }

    [Fact]
    public void GenerateEffectChainCode_WithEffect_GeneratesCode()
    {
        // Arrange
        var service = new EffectService();
        var clip = new TimelineClip { Id = 1 };
        var effect = service.CreateEffect("Crop");
        effect.Parameters.First(p => p.Name == "left").Value = 10;
        effect.IsEnabled = true;
        clip.Effects.Add(effect);

        // Act
        var code = service.GenerateEffectChainCode(clip, "input");

        // Assert
        Assert.NotEmpty(code);
        Assert.Contains("fx_1_0", code);
    }

    [Fact]
    public void GenerateEffectChainCode_DisabledEffect_ExcludesFromCode()
    {
        // Arrange
        var service = new EffectService();
        var clip = new TimelineClip { Id = 1 };
        var effect = service.CreateEffect("Crop");
        effect.IsEnabled = false;
        clip.Effects.Add(effect);

        // Act
        var code = service.GenerateEffectChainCode(clip, "input");

        // Assert
        Assert.Equal("", code);
    }

    #endregion

    #region GetFinalOutputVar Tests

    [Fact]
    public void GetFinalOutputVar_NoEffects_ReturnsInputVar()
    {
        // Arrange
        var service = new EffectService();
        var clip = new TimelineClip { Id = 1 };

        // Act
        var result = service.GetFinalOutputVar(clip, "input");

        // Assert
        Assert.Equal("input", result);
    }

    [Fact]
    public void GetFinalOutputVar_WithEffects_ReturnsFinalVar()
    {
        // Arrange
        var service = new EffectService();
        var clip = new TimelineClip { Id = 1 };
        var effect = service.CreateEffect("Crop");
        effect.IsEnabled = true;
        clip.Effects.Add(effect);

        // Act
        var result = service.GetFinalOutputVar(clip, "input");

        // Assert
        Assert.Equal("fx_1_0", result);
    }

    #endregion

    #region GenerateColorGradeCode Tests

    [Fact]
    public void GenerateColorGradeCode_NoAdjustments_ReturnsAssignment()
    {
        // Arrange
        var service = new EffectService();
        var grade = new ColorGrade();

        // Act
        var code = service.GenerateColorGradeCode(grade, "input", "output");

        // Assert
        Assert.Contains("output = input", code);
    }

    [Fact]
    public void GenerateColorGradeCode_WithExposure_GeneratesCode()
    {
        // Arrange
        var service = new EffectService();
        var grade = new ColorGrade { Exposure = 1.0 };

        // Act
        var code = service.GenerateColorGradeCode(grade, "input", "output");

        // Assert
        Assert.Contains("_exp", code);
    }

    [Fact]
    public void GenerateColorGradeCode_WithTemperature_GeneratesCode()
    {
        // Arrange
        var service = new EffectService();
        var grade = new ColorGrade { Temperature = 20 };

        // Act
        var code = service.GenerateColorGradeCode(grade, "input", "output");

        // Assert
        Assert.Contains("Temperature", code);
    }

    #endregion

    #region GenerateTimelineScript Tests

    [Fact]
    public void GenerateTimelineScript_EmptyTimeline_GeneratesHeader()
    {
        // Arrange
        var service = new EffectService();
        var timeline = new Timeline();

        // Act
        var script = service.GenerateTimelineScript(timeline);

        // Assert
        Assert.Contains("import vapoursynth as vs", script);
        Assert.Contains("core = vs.core", script);
    }

    [Fact]
    public void GenerateTimelineScript_WithOutputPath_IncludesMetadata()
    {
        // Arrange
        var service = new EffectService();
        var timeline = new Timeline();

        // Act
        var script = service.GenerateTimelineScript(timeline, @"C:\output\video.mp4");

        // Assert
        Assert.Contains("# Output:", script);
        Assert.Contains("video.mp4", script);
    }

    #endregion

    #region EffectDefinition Tests

    [Fact]
    public void EffectDefinition_HasRequiredProperties()
    {
        // Arrange
        var service = new EffectService();
        var effect = service.GetEffectDefinition("Crop")!;

        // Assert
        Assert.NotEmpty(effect.Name);
        Assert.NotEmpty(effect.Category);
        Assert.NotEmpty(effect.VsNamespace);
        Assert.NotEmpty(effect.VsFunction);
    }

    [Fact]
    public void EffectDefinition_ResizeHasWidthHeightParams()
    {
        // Arrange
        var service = new EffectService();
        var effect = service.GetEffectDefinition("Resize (Lanczos)")!;

        // Assert
        Assert.Contains(effect.ParameterDefinitions, p => p.Name == "width");
        Assert.Contains(effect.ParameterDefinitions, p => p.Name == "height");
    }

    #endregion

    #region EffectPreset Tests

    [Fact]
    public void EffectPreset_HasCorrectDefaults()
    {
        // Arrange & Act
        var preset = new EffectPreset();

        // Assert
        Assert.Equal("", preset.Name);
        Assert.Equal("", preset.EffectName);
        Assert.NotNull(preset.Parameters);
    }

    #endregion
}

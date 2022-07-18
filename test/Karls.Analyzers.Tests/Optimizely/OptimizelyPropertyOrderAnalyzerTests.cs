using Karls.Analyzers.Optimizely;
using Roslynator.Testing.CSharp;

namespace Karls.Analyzers.Tests.Optimizely;

public class OptimizelyPropertyOrderAnalyzerTests : OptimizelyAnalyzerTestBase<OptimizelyPropertyOrderAnalyzer, OptimizelyPropertyOrderCodeFixProvider> {
    public override CSharpTestOptions Options => CSharpTestOptions.Default
        .WithParseOptions(CSharpTestOptions.Default.ParseOptions.WithLanguageVersion(LanguageVersion.CSharp10));

    public DiagnosticDescriptor Descriptor { get; } = DiagnosticRules.OptimizelyPropertyOrderShouldMatchSourceOrder;

    [Fact]
    public async Task ShouldReportWhenClassIsContentTypeAndPropertiesAreInWrongOrderAsync() {
        await VerifyDiagnosticAsync(@"
using System.ComponentModel.DataAnnotations;

[ContentType]
public class Block {
    [|[Display(Order = 2)]
    public virtual string Prop2 { get; set; }|]

    [Display(Order = 1)]
    public virtual string Prop1 { get; set; }
}

".ToDiagnosticsData(Descriptor, OptimizelySetupCode));
    }

    [Fact]
    public async Task RealExampleWithTonsOfAttributeArgumentsAndStuffAsync() {
        await VerifyDiagnosticAsync(@"
using System.ComponentModel.DataAnnotations;

[ContentType(
    DisplayName = ""Button block"",
    GUID = ""65878547-af59-4a12-8fc2-47d2d1b86d65"",
    Description = ""A block with a button."")]
public class ButtonBlock : BlockData {
    [|[CultureSpecific]
    [Display(
      Name = ""Link"",
      Description = ""The button link."",
      GroupName = SystemTabNames.Content,
      Order = 2)]
    public virtual Url Link { get; set; }|]

    [CultureSpecific]
    [Display(
      Name = ""Text"",
      Description = ""The button text."",
      GroupName = SystemTabNames.Content,
      Order = 1)]
    public virtual string StupidComponent { get; set; } = string.Empty;
}

".ToDiagnosticsData(Descriptor, OptimizelySetupCode));
    }

    [Fact]
    public async Task ShouldNotReportWhenClassIsContentTypeAndPropertiesAreInOrderAsync() {
        await VerifyNoDiagnosticAsync(@"
using System.ComponentModel.DataAnnotations;

[ContentType]
public class Block {
    [Display(Order = 1)]
    public virtual string Prop1 { get; set; }

    [Display(Order = 2)]
    public virtual string Prop2 { get; set; }
}

".ToDiagnosticsData(Descriptor, OptimizelySetupCode));
    }

    [Fact]
    public async Task ShouldNotReportWhenClassIsNotContentTypeAndPropertiesAreInWrongOrderAsync() {
        await VerifyNoDiagnosticAsync(@"
using System.ComponentModel.DataAnnotations;

public class Block {
    [Display(Order = 2)]
    public virtual string Prop2 { get; set; }

    [Display(Order = 1)]
    public virtual string Prop1 { get; set; }
}

".ToDiagnosticsData(Descriptor, OptimizelySetupCode));
    }

    [Fact]
    public async Task ShouldReorderPropertiesWithCodeFixAsync() {
        await VerifyDiagnosticAndFixAsync(@"
using System.ComponentModel.DataAnnotations;

[ContentType]
public class Block {
    [|[Display(Order = 2)]
    public virtual string Prop2 { get; set; }|]

    [Display(Order = 1)]
    public virtual string Prop1 { get; set; }

    [Display(Order = 3)]
    public virtual string Prop3 { get; set; }
}

".ToDiagnosticsData(Descriptor, OptimizelySetupCode), @"
using System.ComponentModel.DataAnnotations;

[ContentType]
public class Block {
    [Display(Order = 1)]
    public virtual string Prop1 { get; set; }

    [Display(Order = 2)]
    public virtual string Prop2 { get; set; }

    [Display(Order = 3)]
    public virtual string Prop3 { get; set; }
}

".ToExpectedTestState());
    }
}

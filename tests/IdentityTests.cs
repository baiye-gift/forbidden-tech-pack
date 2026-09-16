using ForbiddenTechnologyPack.Core;

internal static class IdentityTests {
    public static void Run() {
        AssertEx.Equal("Baiye.ForbiddenTechnologyPack", ModIdentity.StaticId, "static id");
        AssertEx.Equal("BaiyeForbiddenProtoMatter", ModIdentity.ProtoMatterId, "element id");
        AssertEx.Equal("BaiyeMatterAnalyzer", ModIdentity.MatterAnalyzerId, "analyzer id");
        AssertEx.Equal("BaiyeMassCrusher", ModIdentity.MassCrusherId, "crusher id");
        AssertEx.Equal("BaiyeMatterCompiler", ModIdentity.MatterCompilerId, "compiler id");
    }
}

namespace ME.ECSEditor {

    using ME.ECS;

    [UnityEditor.InitializeOnLoadAttribute]
    public static class DefinesGenerator {

        static DefinesGenerator() {
            
            InitializerEditor.getAdditionalDefines += () => {

                var item = new InitializerBase.DefineInfo(true, "WORLD_MARKERS_MANAGED", "Managed markers support added.", () => {
                    #if WORLD_MARKERS_MANAGED
                    return true;
                    #else
                    return false;
                    #endif
                }, true, InitializerBase.ConfigurationType.DebugAndRelease, InitializerBase.CodeSize.Light, InitializerBase.RuntimeSpeed.Light);

                return new InitializerBase.DefineInfo[] { item };
                    
            };
            
        }
    
    }

}
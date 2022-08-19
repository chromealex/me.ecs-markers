#if !WORLD_MARKERS_MANAGED

namespace ME.ECS {

    using ME.ECS.Collections.V3;
    
    public static class MarkersCounter {

        public static int counter;

    }

    public static class MarkerInfo<T> {

        public static int id;

    }

    public struct MarkersStorage : IPlugin {

        public static int key;

        public int GetKey() => MarkersStorage.key;

        public MemArrayAllocator<bool> exists;
        public MemArrayAllocator<UnsafeData> data;
        
        public void Initialize(int key, ref ME.ECS.Collections.V3.MemoryAllocator allocator) {

            MarkersStorage.key = key;

        }

        public void RemoveMarkers(ref MemoryAllocator allocator) {

            for (int i = 0; i < this.exists.Length; ++i) {

                if (this.exists[in allocator, i] == true) {
                    
                    this.data[in allocator, i].Dispose(ref allocator);
                    this.exists[in allocator, i] = false;
                    
                }
                
            }
            
        }

        public bool Add<TMarker>(ref MemoryAllocator allocator, TMarker markerData) where TMarker : unmanaged, IMarker {

            var id = this.GetId<TMarker>();
            this.exists.Resize(ref allocator, id + 1);
            this.data.Resize(ref allocator, id + 1);

            if (this.exists[in allocator, id] == true) {

                this.data[in allocator, id].Dispose(ref allocator);
                this.data[in allocator, id] = new UnsafeData().Set(ref allocator, markerData);
                return false;
                
            }
            
            this.exists[in allocator, id] = true;
            this.data[in allocator, id] = new UnsafeData().Set(ref allocator, markerData);

            return true;

        }

        public bool Get<TMarker>(ref MemoryAllocator allocator, out TMarker markerData) where TMarker : unmanaged, IMarker {

            var id = this.GetId<TMarker>();
            this.exists.Resize(ref allocator, id + 1);
            this.data.Resize(ref allocator, id + 1);

            if (this.exists[in allocator, id] == true) {

                markerData = this.data[in allocator, id].Get<TMarker>(ref allocator);
                return true;
                
            }
            
            markerData = default;
            return false;

        }

        public bool Remove<TMarker>(ref MemoryAllocator allocator) where TMarker : unmanaged, IMarker {

            var id = this.GetId<TMarker>();
            this.exists.Resize(ref allocator, id + 1);
            this.data.Resize(ref allocator, id + 1);

            if (this.exists[in allocator, id] == true) {

                this.data[in allocator, id].Dispose(ref allocator);
                this.exists[in allocator, id] = false;
                return true;
                
            }
            
            return false;

        }

        public bool Has<TMarker>(ref MemoryAllocator allocator) where TMarker : unmanaged, IMarker {

            var id = this.GetId<TMarker>();
            this.exists.Resize(ref allocator, id + 1);
            return this.exists[in allocator, id];
            
        }

        private int GetId<T>() {

            if (MarkerInfo<T>.id == 0) {

                MarkerInfo<T>.id = ++MarkersCounter.counter;

            }

            return MarkerInfo<T>.id;

        }

    }

    public static class WorldMarkersExtension {

        public static bool AddMarker<TMarker>(this World world, TMarker markerData) where TMarker : unmanaged, IMarker {

            E.IS_NOT_LOGIC_STEP(world);

            ref var state = ref world.GetNoStateData();
            ref var storage = ref state.pluginsStorage.Get<MarkersStorage>(ref state.allocator, MarkersStorage.key);
            return storage.Add(ref state.allocator, markerData);
            
        }

        public static bool GetMarker<TMarker>(this World world, out TMarker marker) where TMarker : unmanaged, IMarker {
            
            ref var state = ref world.GetNoStateData();
            ref var storage = ref state.pluginsStorage.Get<MarkersStorage>(ref state.allocator, MarkersStorage.key);
            return storage.Get(ref state.allocator, out marker);
            
        }

        public static bool HasMarker<TMarker>(this World world) where TMarker : unmanaged, IMarker {
            
            ref var state = ref world.GetNoStateData();
            ref var storage = ref state.pluginsStorage.Get<MarkersStorage>(ref state.allocator, MarkersStorage.key);
            return storage.Has<TMarker>(ref state.allocator);

        }

        public static bool RemoveMarker<TMarker>(this World world) where TMarker : unmanaged, IMarker {
            
            E.IS_NOT_LOGIC_STEP(world);

            ref var state = ref world.GetNoStateData();
            ref var storage = ref state.pluginsStorage.Get<MarkersStorage>(ref state.allocator, MarkersStorage.key);
            return storage.Remove<TMarker>(ref state.allocator);
            
        }
        
    }
    
    #if ECS_COMPILE_IL2CPP_OPTIONS
    [Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.NullChecks, false),
     Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false),
     Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute(Unity.IL2CPP.CompilerServices.Option.DivideByZeroChecks, false)]
    #endif
    public static class WorldInitializer {

        private static bool initialized = false;
        
        #if UNITY_EDITOR
        [UnityEditor.InitializeOnLoad]
        private static class EditorInitializer {
            static EditorInitializer() => WorldInitializer.Initialize();
        }
        #endif

        [UnityEngine.RuntimeInitializeOnLoadMethodAttribute(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize() {
            
            if (WorldInitializer.initialized == false) {
                
                WorldStaticCallbacks.RegisterCallbacks(OnInit, OnDispose);
                WorldStaticCallbacks.RegisterCallbacks(OnWorldStep);

                WorldInitializer.initialized = true;
            }
            
        }

        private static void OnWorldStep(World world, WorldCallbackStep step) {

            if (step == WorldCallbackStep.UpdateVisualPreStageEnd) {

                E.IS_NOT_LOGIC_STEP(world);

                #if CHECKPOINT_COLLECTOR
                if (this.checkpointCollector != null) this.checkpointCollector.Checkpoint("RemoveMarkers", WorldStep.None);
                #endif

                #if UNITY_EDITOR
                UnityEngine.Profiling.Profiler.BeginSample($"Remove Markers");
                #endif

                world.GetNoStateData().pluginsStorage.GetOrCreate<MarkersStorage>(ref world.GetNoStateData().allocator).RemoveMarkers(ref world.GetNoStateData().allocator);

                #if UNITY_EDITOR
                UnityEngine.Profiling.Profiler.EndSample();
                #endif

                #if CHECKPOINT_COLLECTOR
                if (this.checkpointCollector != null) this.checkpointCollector.Checkpoint("RemoveMarkers", WorldStep.None);
                #endif

            }
            
        }

        private static void OnDispose(World world) {
            
        }

        private static void OnInit(World world) {

            world.GetNoStateData().pluginsStorage.GetOrCreate<MarkersStorage>(ref world.GetNoStateData().allocator);

        }

    }

}
#endif
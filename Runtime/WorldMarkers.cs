#if WORLD_MARKERS_MANAGED
using System.Collections.Generic;

namespace ME.ECS {

    using ME.ECS.Collections;

    public static class MarkersStorage {
        
        private static class MarkersDirectCache<TMarker> where TMarker : struct, IMarker {

            internal static BufferArray<TMarker> data = new BufferArray<TMarker>();
            internal static BufferArray<bool> exists = new BufferArray<bool>();

        }

        private static HashSet<BufferArray<bool>> allExistMarkers;

        public static void OnSpawnMarkers() {
            
            allExistMarkers = PoolHashSet<BufferArray<bool>>.Spawn(World.WORLDS_CAPACITY);

        }

        public static void OnRecycleMarkers() {
            
            PoolHashSet<BufferArray<bool>>.Recycle(ref allExistMarkers);

        }

        public static void RemoveMarkers(World world) {

            foreach (var item in allExistMarkers) {

                item.arr[world.id] = false;
                
            }

        }

        public static bool AddMarker<TMarker>(World world, TMarker markerData) where TMarker : struct, IMarker {

            ref var exists = ref MarkersStorage.MarkersDirectCache<TMarker>.exists;
            ref var cache = ref MarkersStorage.MarkersDirectCache<TMarker>.data;

            if (ArrayUtils.WillResize(world.id, ref exists) == true) {

                allExistMarkers.Remove(exists);

            }
            
            ArrayUtils.Resize(world.id, ref exists);
            ArrayUtils.Resize(world.id, ref cache);
            
            if (allExistMarkers.Contains(exists) == false) {

                allExistMarkers.Add(exists);

            }

            if (exists.arr[world.id] == true) {

                cache.arr[world.id] = markerData;
                return false;

            }

            exists.arr[world.id] = true;
            cache.arr[world.id] = markerData;

            return true;

        }

        public static bool GetMarker<TMarker>(World world, out TMarker marker) where TMarker : struct, IMarker {
            
            ref var exists = ref MarkersStorage.MarkersDirectCache<TMarker>.exists;
            if (world.id >= 0 && world.id < exists.Length && exists.arr[world.id] == true) {

                ref var cache = ref MarkersStorage.MarkersDirectCache<TMarker>.data;
                marker = cache.arr[world.id];
                return true;

            }

            marker = default;
            return false;

        }

        public static bool HasMarker<TMarker>(World world) where TMarker : struct, IMarker {
            
            ref var exists = ref MarkersStorage.MarkersDirectCache<TMarker>.exists;
            return world.id >= 0 && world.id < exists.Length && exists.arr[world.id] == true;

        }

        public static bool RemoveMarker<TMarker>(World world) where TMarker : struct, IMarker {
            
            ref var exists = ref MarkersStorage.MarkersDirectCache<TMarker>.exists;
            if (world.id >= 0 && world.id < exists.Length && exists.arr[world.id] == true) {

                ref var cache = ref MarkersStorage.MarkersDirectCache<TMarker>.data;
                cache.arr[world.id] = default;
                exists.arr[world.id] = false;
                return true;

            }

            return false;

        }

    }
    
    public static class WorldMarkersExtension {
        
        public static bool AddMarker<TMarker>(this World world, TMarker markerData) where TMarker : IMarker {

            E.IS_NOT_LOGIC_STEP(world);

            return MarkersStorage.AddMarker<TMarker>(world, markerData);

        }

        public static bool GetMarker<TMarker>(this World world, out TMarker marker) where TMarker : IMarker {

            return MarkersStorage.GetMarker(world, out marker);

        }

        public static bool HasMarker<TMarker>(this World world) where TMarker : IMarker {

            return MarkersStorage.HasMarker<TMarker>(world);

        }

        public static bool RemoveMarker<TMarker>(this World world) where TMarker : IMarker {
            
            E.IS_NOT_LOGIC_STEP(world);

            return MarkersStorage.RemoveMarker<TMarker>(world);

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

                MarkersStorage.RemoveMarkers();
                
                #if UNITY_EDITOR
                UnityEngine.Profiling.Profiler.EndSample();
                #endif

                #if CHECKPOINT_COLLECTOR
                if (this.checkpointCollector != null) this.checkpointCollector.Checkpoint("RemoveMarkers", WorldStep.None);
                #endif

            }
            
        }

        private static void OnDispose(World world) {
            
            MarkersStorage.OnRecycleMarkers();

        }

        private static void OnInit(World world) {

            MarkersStorage.OnSpawnMarkers();

        }

    }

}
#endif
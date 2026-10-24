using System;
using System.Collections.Generic;

using UnityEngine;

namespace MaterialPainter2
{
    /// <summary>Every paintable brush. List order is the order they appear in the Elements tab.</summary>
    public static class BrushRegistry
    {
        private static readonly List<Brush> brushes = new List<Brush>();
        private static readonly Dictionary<MaterialBrush, Brush> byId = new Dictionary<MaterialBrush, Brush>();

        public static IReadOnlyList<Brush> All => brushes;

        static BrushRegistry()
        {
            Func<Func<AssetManager, Material>, Func<Material>> fromAssets =
                pick => () => BrushMaterials.Clone(pick(ScriptableSingleton<AssetManager>.Instance));

            Register(new WaterBrush(MaterialBrush.Water, "Water", "icon_water", WaterBody.BodyType.WATER, WaterBody.defaultWaterColor));
            Register(new WaterBrush(MaterialBrush.Lava, "Lava", "icon_lava", WaterBody.BodyType.LAVA, Color.white));
            Register(new GlassBrush(MaterialBrush.Glass, "Glass", "icon_glass"));
            RegisterTerrain(MaterialBrush.TriplanarTerrainGrass, "Grass", "icon_terrain_grass", "Grass");
            RegisterTerrain(MaterialBrush.TriplanarTerrainDirt, "Dirt", "icon_terrain_dirt", "Dirt");
            RegisterTerrain(MaterialBrush.TriplanarTerrainStone, "Stone", "icon_terrain_stone", "Rock");
            RegisterTerrain(MaterialBrush.TriplanarTerrainSnow, "Snow", "icon_terrain_snow", "Snow");
            RegisterTerrain(MaterialBrush.TriplanarTerrainSand, "Sand", "icon_terrain_sand", "Sand");
            RegisterTerrain(MaterialBrush.TriplanarTerrainIce, "Ice", "icon_terrain_ice", "Ice");
            RegisterTerrain(MaterialBrush.TriplanarTerrainLavarock, "Lava Rock", "icon_terrain_lavarock", "Lava Rock");
            RegisterTerrain(MaterialBrush.TriplanarTerrainAsh, "Ash", "icon_terrain_ash", "Ash");
            RegisterTerrain(MaterialBrush.TriplanarTerrainCrackedSoil, "Cracked Soil", "icon_terrain_cracked_soil", "Cracked Dirt");
            RegisterTerrain(MaterialBrush.TriplanarTerrainDeadGrass, "Dead Grass", "icon_terrain_dead_grass", "Dead Grass");
            Register(new SharedMaterialBrush(MaterialBrush.TriplanarTerrainBlueprint, "Blueprint", "icon_terrain_blueprint",
                () => BrushMaterials.CreateTerrain("", "TriplanarTerrainBlueprint", new Color(0.35f, 0.65f, 1f, 1f), 2f)));

            RegisterWild(MaterialBrush.Terrain, "Terrain", fromAssets(a => a.terrainMaterial));
            RegisterWild(MaterialBrush.UndergroundTerrain, "Underground Terrain", fromAssets(a => a.undergroundViewTerrainMaterial));
            RegisterWild(MaterialBrush.DataView, "Data View", fromAssets(a => a.dataViewMaterial));
            RegisterWild(MaterialBrush.Selected, "Selected", fromAssets(a => a.selectedObjectMaterial));
            RegisterWild(MaterialBrush.Deleted, "Deleted", fromAssets(a => a.deletedObjectMaterial));
            RegisterWild(MaterialBrush.SceneGhost, "Scene Ghost", fromAssets(a => a.sceneViewGhostMaterial));
            RegisterWild(MaterialBrush.DecoGlow, "Deco Glow", fromAssets(a => a.decoVisibilityHighlightOverlayMaterial));
            RegisterWild(MaterialBrush.CollisionGlow, "Collision Glow", fromAssets(a => a.collidingObjectsHighlightOverlayMaterial));
            RegisterWild(MaterialBrush.RideLight, "Ride Light", fromAssets(a => a.rideLightMaterial));
            RegisterWild(MaterialBrush.CoasterStats, "Coaster Stats", fromAssets(a => a.trackedRideStatVisualizerMaterial));
            RegisterWild(MaterialBrush.Waterfall, "Waterfall", BrushMaterials.CreateWaterfall, "icon_effect_waterfall");
            RegisterWild(MaterialBrush.BlackHoleCore, "Black Hole", BrushMaterials.CreateBlackHoleCore, "icon_effect_black_hole_core");
            RegisterWild(MaterialBrush.BlackHoleDisc, "Accretion Disc", BrushMaterials.CreateBlackHoleDisc, "icon_effect_black_hole_disc");

            Register(new InvisibleBrush(MaterialBrush.Invisible, "Invisible", "icon_invisible"));
            Register(new ImageBrush(MaterialBrush.Image, "Image", "icon_video2"));
            Register(new VideoBrush(MaterialBrush.Video, "Video", "icon_video1"));
        }

        public static Brush Get(MaterialBrush id)
        {
            // Invisible used to save its window-open preview state under its own id.
            if (id == MaterialBrush.InvisiblePreview)
                id = MaterialBrush.Invisible;

            return byId.TryGetValue(id, out Brush brush) ? brush : null;
        }

        public static bool IsVisible(MaterialBrush id)
        {
            Brush brush = Get(id);
            return brush == null || !brush.Wild || MP2._setting_go_wild;
        }

        public static void ReleaseCaches()
        {
            foreach (Brush brush in brushes)
                brush.ReleaseCache();
        }

        private static void Register(Brush brush)
        {
            brushes.Add(brush);
            byId[brush.Id] = brush;
        }

        private static void RegisterTerrain(MaterialBrush id, string name, string icon, string terrainTypeName)
        {
            Register(new SharedMaterialBrush(id, name, icon,
                () => BrushMaterials.CreateTerrain(terrainTypeName, id.ToString(), Color.white, 1f)));
        }

        private static void RegisterWild(MaterialBrush id, string name, Func<Material> createMaterial, string icon = "tex_question")
        {
            Register(new SharedMaterialBrush(id, name, icon, createMaterial, wild: true));
        }
    }
}

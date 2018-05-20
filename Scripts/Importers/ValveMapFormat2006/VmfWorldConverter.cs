﻿#if UNITY_EDITOR || RUNTIME_CSG

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Sabresaurus.SabreCSG.Importers.ValveMapFormat2006
{
    /// <summary>
    /// Converts a Hammer Map to SabreCSG Brushes.
    /// </summary>
    public static class VmfWorldConverter
    {
        /// <summary>
        /// Imports the specified world into the SabreCSG model.
        /// </summary>
        /// <param name="model">The model to import into.</param>
        /// <param name="world">The world to be imported.</param>
        /// <param name="scale">The scale modifier.</param>
        public static void Import(CSGModel model, VmfWorld world, int scale = 32)
        {
            try
            {
                model.BeginUpdate();

                // group all the brushes together.
                GroupBrush groupBrush = new GameObject("Source Engine Map").AddComponent<GroupBrush>();
                groupBrush.transform.SetParent(model.transform);

                // iterate through all world solids.
                for (int i = 0; i < world.Solids.Count; i++)
                {
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.DisplayProgressBar("SabreCSG: Importing Source Engine Map", "Converting Hammer Solids To SabreCSG Brushes (" + (i + 1) + " / " + world.Solids.Count + ")...", i / (float)world.Solids.Count);
#endif
                    VmfSolid solid = world.Solids[i];

                    // don't add triggers to the scene.
                    if (solid.Sides.Count > 0 && IsSpecialMaterial(solid.Sides[0].Material))
                        continue;

                    // build a very large cube brush.
                    var go = model.CreateBrush(PrimitiveBrushType.Cube, Vector3.zero);
                    var pr = go.GetComponent<PrimitiveBrush>();
                    BrushUtility.Resize(pr, new Vector3(8192, 8192, 8192));

                    // clip all the sides out of the brush.
                    for (int j = solid.Sides.Count; j-- > 0;)
                    {
                        VmfSolidSide side = solid.Sides[j];
                        Plane clip = new Plane(pr.transform.InverseTransformPoint(new Vector3(side.Plane.P1.X, side.Plane.P1.Z, side.Plane.P1.Y) / scale), pr.transform.InverseTransformPoint(new Vector3(side.Plane.P2.X, side.Plane.P2.Z, side.Plane.P2.Y) / scale), pr.transform.InverseTransformPoint(new Vector3(side.Plane.P3.X, side.Plane.P3.Z, side.Plane.P3.Y) / scale));
                        ClipUtility.ApplyClipPlane(pr, clip, false);

                        // find the polygons associated with the clipping plane.
                        // the normal is unique and can never occur twice as that wouldn't allow the solid to be convex.
                        var polygons = pr.GetPolygons().Where(p => p.Plane.normal == clip.normal);
                        foreach (var polygon in polygons)
                        {
                            // detect excluded polygons.
                            if (IsExcludedMaterial(side.Material))
                                polygon.UserExcludeFromFinal = true;
                            // detect collision-only brushes.
                            if (IsInvisibleMaterial(side.Material))
                                pr.IsVisible = false;
                            // try finding the material in the project.
                            polygon.Material = FindMaterial(side.Material);
                            // calculate the texture coordinates.
                            int w = 256;
                            int h = 256;
                            if (polygon.Material != null && polygon.Material.mainTexture != null)
                            {
                                w = polygon.Material.mainTexture.width;
                                h = polygon.Material.mainTexture.height;
                            }
                            CalculateTextureCoordinates(polygon, w, h, side.UAxis, side.VAxis, scale);
                        }
                    }

                    // add the brush to the group.
                    pr.transform.SetParent(groupBrush.transform);
                }

                // iterate through all entities.
                for (int e = 0; e < world.Entities.Count; e++)
                {
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.DisplayProgressBar("SabreCSG: Importing Source Engine Map", "Converting Hammer Entities To SabreCSG Brushes (" + (e + 1) + " / " + world.Entities.Count + ")...", e / (float)world.Entities.Count);
#endif
                    VmfEntity entity = world.Entities[e];

                    // skip entities that sabrecsg can't handle.
                    switch (entity.ClassName)
                    {
                        case "func_areaportal":
                        case "func_areaportalwindow":
                        case "func_capturezone":
                        case "func_changeclass":
                        case "func_combine_ball_spawner":
                        case "func_dustcloud":
                        case "func_dustmotes":
                        case "func_nobuild":
                        case "func_nogrenades":
                        case "func_occluder":
                        case "func_precipitation":
                        case "func_proprespawnzone":
                        case "func_regenerate":
                        case "func_respawnroom":
                        case "func_smokevolume":
                        case "func_viscluster":
                            continue;
                    }

                    // iterate through all entity solids.
                    for (int i = 0; i < entity.Solids.Count; i++)
                    {
                        VmfSolid solid = entity.Solids[i];

                        // don't add triggers to the scene.
                        if (solid.Sides.Count > 0 && IsSpecialMaterial(solid.Sides[0].Material))
                            continue;

                        // build a very large cube brush.
                        var go = model.CreateBrush(PrimitiveBrushType.Cube, Vector3.zero);
                        var pr = go.GetComponent<PrimitiveBrush>();
                        BrushUtility.Resize(pr, new Vector3(8192, 8192, 8192));

                        // clip all the sides out of the brush.
                        for (int j = solid.Sides.Count; j-- > 0;)
                        {
                            VmfSolidSide side = solid.Sides[j];
                            Plane clip = new Plane(pr.transform.InverseTransformPoint(new Vector3(side.Plane.P1.X, side.Plane.P1.Z, side.Plane.P1.Y) / scale), pr.transform.InverseTransformPoint(new Vector3(side.Plane.P2.X, side.Plane.P2.Z, side.Plane.P2.Y) / scale), pr.transform.InverseTransformPoint(new Vector3(side.Plane.P3.X, side.Plane.P3.Z, side.Plane.P3.Y) / scale));
                            ClipUtility.ApplyClipPlane(pr, clip, false);

                            // find the polygons associated with the clipping plane.
                            // the normal is unique and can never occur twice as that wouldn't allow the solid to be convex.
                            var polygons = pr.GetPolygons().Where(p => p.Plane.normal == clip.normal);
                            foreach (var polygon in polygons)
                            {
                                // detect excluded polygons.
                                if (IsExcludedMaterial(side.Material))
                                    polygon.UserExcludeFromFinal = true;
                                // detect collision-only brushes.
                                if (IsInvisibleMaterial(side.Material))
                                    pr.IsVisible = false;
                                // try finding the material in the project.
                                polygon.Material = FindMaterial(side.Material);
                                // calculate the texture coordinates.
                                int w = 256;
                                int h = 256;
                                if (polygon.Material != null && polygon.Material.mainTexture != null)
                                {
                                    w = polygon.Material.mainTexture.width;
                                    h = polygon.Material.mainTexture.height;
                                }
                                CalculateTextureCoordinates(polygon, w, h, side.UAxis, side.VAxis, scale);
                            }
                        }

                        // detail brushes that do not affect the CSG world.
                        if (entity.ClassName == "func_detail")
                            pr.IsNoCSG = true;
                        // collision only brushes.
                        if (entity.ClassName == "func_vehicleclip")
                            pr.IsVisible = false;

                        // add the brush to the group.
                        pr.transform.SetParent(groupBrush.transform);
                    }
                }

#if UNITY_EDITOR
                UnityEditor.EditorUtility.ClearProgressBar();
#endif
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                model.EndUpdate();
            }
        }

        // shoutouts to Stefan Hajnoczi for your map importer giving me a clue on how to do this.
        private static void CalculateTextureCoordinates(Polygon polygon, int textureWidth, int textureHeight, VmfAxis UAxis, VmfAxis VAxis, int scale)
        {
            // calculate texture coordinates.
            for (int i = 0; i < polygon.Vertices.Length; i++)
            {
                float U, V;

                Plane uplane = new Plane(new Vector3(UAxis.Vector.X, UAxis.Vector.Z, UAxis.Vector.Y), UAxis.Translation);
                Plane vplane = new Plane(new Vector3(VAxis.Vector.X, VAxis.Vector.Z, VAxis.Vector.Y), VAxis.Translation);

                U = Vector3.Dot(uplane.normal, polygon.Vertices[i].Position);
                U = (U / textureWidth) / (UAxis.Scale / scale);
                U = U + (UAxis.Translation / textureWidth);

                V = Vector3.Dot(vplane.normal, polygon.Vertices[i].Position);
                V = (V / textureHeight) / (VAxis.Scale / scale);
                V = V + (VAxis.Translation / textureHeight);

                polygon.Vertices[i].UV.x = U;
                polygon.Vertices[i].UV.y = 1.0f - V;
            }
        }

        /// <summary>
        /// Determines whether the specified name is an excluded material.
        /// </summary>
        /// <param name="name">The name of the material.</param>
        /// <returns><c>true</c> if the specified name is an excluded material; otherwise, <c>false</c>.</returns>
        private static bool IsExcludedMaterial(string name)
        {
            switch (name)
            {
                case "TOOLS/TOOLSNODRAW":
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether the specified name is an invisible material.
        /// </summary>
        /// <param name="name">The name of the material.</param>
        /// <returns><c>true</c> if the specified name is an invisible material; otherwise, <c>false</c>.</returns>
        private static bool IsInvisibleMaterial(string name)
        {
            switch (name)
            {
                case "TOOLS/TOOLSCLIP":
                case "TOOLS/TOOLSNPCCLIP":
                case "TOOLS/TOOLSPLAYERCLIP":
                case "TOOLS/TOOLSGRENDADECLIP":
                case "TOOLS/TOOLSSTAIRS":
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether the specified name is a special material, these brush will not be
        /// imported into SabreCSG.
        /// </summary>
        /// <param name="name">The name of the material.</param>
        /// <returns><c>true</c> if the specified name is a special material; otherwise, <c>false</c>.</returns>
        private static bool IsSpecialMaterial(string name)
        {
            switch (name)
            {
                case "TOOLS/TOOLSTRIGGER":
                case "TOOLS/TOOLSBLOCK_LOS":
                case "TOOLS/TOOLSBLOCKBULLETS":
                case "TOOLS/TOOLSBLOCKBULLETS2":
                case "TOOLS/TOOLSBLOCKSBULLETSFORCEFIELD": // did the wiki have a typo or is BLOCKS truly plural?
                case "TOOLS/TOOLSBLOCKLIGHT":
                case "TOOLS/TOOLSCLIMBVERSUS":
                case "TOOLS/TOOLSHINT":
                case "TOOLS/TOOLSINVISIBLE":
                case "TOOLS/TOOLSINVISIBLENONSOLID":
                case "TOOLS/TOOLSINVISIBLELADDER":
                case "TOOLS/TOOLSINVISMETAL":
                case "TOOLS/TOOLSNODRAWROOF":
                case "TOOLS/TOOLSNODRAWWOOD":
                case "TOOLS/TOOLSNODRAWPORTALABLE":
                case "TOOLS/TOOLSSKIP":
                case "TOOLS/TOOLSFOG":
                case "TOOLS/TOOLSSKYBOX":
                case "TOOLS/TOOLS2DSKYBOX":
                case "TOOLS/TOOLSSKYFOG":
                case "TOOLS/TOOLSFOGVOLUME":
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Attempts to find a material in the project by name.
        /// </summary>
        /// <param name="name">The material name to search for.</param>
        /// <returns>The material if found or null.</returns>
        private static Material FindMaterial(string name)
        {
#if UNITY_EDITOR
            // first try finding the fully qualified texture name with '/' replaced by '.' so 'BRICK.BRICKWALL052D'.
            name = name.Replace("/", ".");
            string texture = "";
            string guid = UnityEditor.AssetDatabase.FindAssets("t:Material " + name).FirstOrDefault();
            if (guid == null)
            {
                // if it couldn't be found try a simplified name like 'BRICKWALL052D'.
                texture = name;
                if (name.Contains('.'))
                    texture = name.Substring(name.LastIndexOf('.') + 1);
                guid = UnityEditor.AssetDatabase.FindAssets("t:Material " + texture).FirstOrDefault();
            }
            // if a material could be found using either option:
            if (guid != null)
            {
                // load the material.
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                return UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
            }
            else { Debug.Log("SabreCSG: Tried to find material '" + name + "' and also as '" + texture + "' but it couldn't be found in the project."); }
#endif
            return null;
        }
    }
}

#endif
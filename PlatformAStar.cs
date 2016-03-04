﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using MoreLinq;

using PlatformGraph = System.Collections.Generic.Dictionary<awkwardsimulator.Platform, System.Collections.Generic.HashSet<awkwardsimulator.Platform>>;
using StateNode = awkwardsimulator.AStarNode<awkwardsimulator.Platform, awkwardsimulator.Platform>;

namespace awkwardsimulator
{
    public class PlatformAStar
    {
        private PlatformGraph platformGraph;
        public PlatformGraph PlatformGraph { get { return platformGraph; } }

        private List<Platform> Platforms { get { return platformGraph.Keys.ToList(); } }

        public PlatformAStar(List<Platform> platforms) {
            this.platformGraph = BuildPlatformGraph (platforms);
        }

        public GameObject NextPlatform(GameObject start, GameObject end) {
            var path = PlatformPath (start, end);

            var next = (path.Count > 1) ? path [1] : path [0];

            return next;
        }

        public List<GameObject> PlatformPath(GameObject start, GameObject end) {
            var startPlat = nearestPlatform (start.SurfaceCenter, Platforms);

            var endReachablePlatforms = Platforms.FindAll (p => reachable (p, end));

//            Debug.WriteLine ("all platforms: "+ PlatListStr (Platforms));
//            Debug.WriteLine ("end reachable: "+ PlatListStr (endReachablePlatforms));

            var endPlat = nearestPlatform (end.Center, endReachablePlatforms);

            return PlatformPath (startPlat, endPlat).Concat(end).ToList<GameObject>();
        }

        private List<Platform> PlatformPath(Platform start, Platform end) {
            int maxIters = 20;

            var paths = new SortedDictionary<double, StateNode>();

            Func<StateNode, double> heuristic = p => Vector2.Distance (p.Value.SurfaceCenter, end.SurfaceCenter);

            var root = new StateNode (null, start, start);
            paths.Add(heuristic(root), root);
            var best = root;

            for (int i = 0; i < maxIters && best.Value != end && paths.Count > 0; i++) {
                best.Children = PlatformGraph [best.Value].ToDictionary (x => x, x => new StateNode(best, x, x));

                foreach (var c in best.Children) {
                    var h = heuristic (c.Value);
                    if (!paths.ContainsKey(h)) {
                        paths.Add (h, c.Value);
                    }
                }

                best = paths.First().Value;
                paths.Remove (paths.First().Key);
            }

            Debug.WriteLine ("Best Platform: {0}", best);

            return best.ToPath().Select(tup => tup.Item2).ToList();
        }

        private Platform nearestPlatform(Vector2 point, List<Platform> platforms) {
            // Don't return platforms above the given point
            var lowerPlats = platforms.ToList ()
                .FindAll (plat => plat.Y <= point.Y); // Sometimes the Y value waivers, so give a 1pt margin of error

            if (lowerPlats.Count == 0) {
                lowerPlats = platforms.ToList ();
            }

            return lowerPlats.MinBy(plat => {

//                var delta = Vector2.Subtract(nearestPoint(point, plat.Surface), point);
                var delta = Vector2.Subtract(plat.Center, point);


                var scaledDelta = delta * new Vector2(2, 1); // weight X distance more than Y distance

                return scaledDelta.Length();
            });
        }

        private static Vector2 nearestPoint(Vector2 point, List<Vector2> list) {
            return list.MinBy (p => Vector2.Distance (point, p));
        }

        private static PlatformGraph BuildPlatformGraph(List<Platform> platforms) {
            PlatformGraph platGraph = new PlatformGraph ();

            foreach (var plat1 in platforms) {
                HashSet<Platform> hs = new HashSet<Platform> ();
                foreach (var plat2 in platforms) {
                    if (plat1 != plat2 && reachable (plat1, plat2)) {
                        hs.Add (plat2);
                    }
                }
                platGraph.Add(plat1, hs);
            }

            return platGraph;                
        }

        public static string PlatListStr<T>(List<T> platforms) where T : GameObject {
            return string.Join (", ", platforms.Select (x => x.Coords));
        }

        public static string PlatGraphStr(PlatformGraph platGraph) {
            string s = "";
            foreach (var entry in platGraph) {
                s += String.Format ("{0}[{1}]    ", entry.Key.Coords, PlatListStr(entry.Value.ToList()));
            }
            return s;
        }

        private static bool reachable(GameObject go1, GameObject go2) {
            int maxX = 20, maxY = 15;

            return go1.Corners.SelectMany ( a =>
                go2.Corners.Select( b => Vector2.Subtract (a, b)) )
                .Any (d => Math.Abs (d.X) <= maxX && Math.Abs (d.Y) <= maxY);
        }
    }
}


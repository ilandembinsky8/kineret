

using System.Collections.Generic;
using UnityEngine;
using System.Linq;
namespace GISTech.GISTerrainLoader
{
    public class GISTerrainLoaderRoofGenerator
    {
        [Header("Roof Settings")]
        public RoofType roofType = RoofType.Flat;

        private GISTerrainLoaderVectorEntity VE;
        private GISTerrainLoaderBuildingData BuildingData;
        private GISTerrainLoaderMeshData md;
        private float height;
        List<Material> Materials = new List<Material>();
        public GISTerrainLoaderRoofGenerator(GISTerrainLoaderMeshData m_md, GISTerrainLoaderBuildingData m_BuildingData, GISTerrainLoaderVectorEntity m_VE, List<Material> m_Materials, float m_height)
        {
            height = m_height;
            BuildingData = m_BuildingData;
            md = m_md;
            VE = m_VE;
            Materials = m_Materials;
        }
        public void Run()
        {
            GenerateRoofMesh(md, height);
        }


        private void GenerateRoofMesh(GISTerrainLoaderMeshData md, float maxHeight)
        {
            // Generate roof geometry based on type
            switch (BuildingData.building_SO._roofType)
            {
                case RoofType.Flat:
                    // Simple flat roof using original shape
                    break;

                case RoofType.Gable:

                    //GenerateGabledRoof(md, maxHeight);
                    GenerateGableRoof(BuildingData.SpacePoints, BuildingData.building_SO.roofHeight);
                    break;

                case RoofType.Hip:
                    GenerateHipRoof(BuildingData.SpacePoints[0], BuildingData.building_SO.roofHeight);
                    break;
            }

        }

        public void GenerateHipRoof(List<Vector3> footprint, float roofHeight)
        {
            var roofMesh = GenerateHipRoofMesh(BuildingData.SpacePoints[0], BuildingData.building_SO.roofHeight);

            GameObject roofObject = new GameObject("HipRoof");
            roofObject.AddComponent<MeshFilter>().mesh = roofMesh;
            Material roofMaterial = Materials[0];

            if (BuildingData.building_SO.RoofTexture != null)
            {
                roofMaterial.mainTexture = BuildingData.building_SO.RoofTexture;
                roofMaterial.mainTextureScale = BuildingData.building_SO.rooftextureScale;
                roofMaterial.mainTextureOffset = BuildingData.building_SO.rooftextureOffset;
            }

            roofObject.AddComponent<MeshRenderer>().material = roofMaterial;
            roofObject.transform.position += Vector3.up * BuildingData.MaxHeight;
            roofObject.transform.SetParent(VE.GameObject.transform, false);
        }
        public Mesh GenerateHipRoofMesh(List<Vector3> footprint, float roofHeight)
        {
            // Compute centroid of the footprint.
            Vector3 centroid = GetCenter(footprint);


            // Define the roof peak (raise the centroid by roofHeight).
            Vector3 roofPeak = centroid + Vector3.up * roofHeight;

            // Create a new list of vertices: use the footprint vertices first...
            List<Vector3> vertices = new List<Vector3>(footprint);
            // ...then add the peak vertex.
            int peakIndex = vertices.Count;
            vertices.Add(roofPeak);

            // Create triangles: for each edge of the footprint, create one triangle from
            // (current vertex, next vertex, roof peak)
            List<int> triangles = new List<int>();
            for (int i = 0; i < footprint.Count; i++)
            {
                int next = (i + 1) % footprint.Count;
                triangles.Add(i);
                triangles.Add(next);
                triangles.Add(peakIndex);
            }

            // Build the mesh.
            Mesh roofMesh = new Mesh();
            roofMesh.vertices = vertices.ToArray();
            roofMesh.triangles = triangles.ToArray();
            roofMesh.RecalculateNormals();

            // Uniform UV mapping: project vertices onto the XZ plane.
            // First, find the bounding box for the roof vertices in X and Z.
            float minX = float.MaxValue, minZ = float.MaxValue;
            float maxX = float.MinValue, maxZ = float.MinValue;
            foreach (Vector3 v in vertices)
            {
                if (v.x < minX) minX = v.x;
                if (v.x > maxX) maxX = v.x;
                if (v.z < minZ) minZ = v.z;
                if (v.z > maxZ) maxZ = v.z;
            }
            float rangeX = maxX - minX;
            float rangeZ = maxZ - minZ;

            Vector2[] uvs = new Vector2[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
            {
                // Remap x and z to a 0–1 range.
                float u = (vertices[i].x - minX) / (rangeX > 0 ? rangeX : 1);
                float v = (vertices[i].z - minZ) / (rangeZ > 0 ? rangeZ : 1);
                uvs[i] = new Vector2(u, v);
            }
            roofMesh.uv = uvs;

            return roofMesh;
        }


        public void GenerateGableRoof(List<Vector3> footprint, float roofHeight)
        {
            var roofMesh = GenerateGableRoofMesh(BuildingData.SpacePoints[0], BuildingData.building_SO.roofHeight);

            GameObject roofObject = new GameObject("GableRoof");
            roofObject.AddComponent<MeshFilter>().mesh = roofMesh;
            Material roofMaterial = Materials[0];

            if (BuildingData.building_SO.RoofTexture != null)
            {
                roofMaterial.mainTexture = BuildingData.building_SO.RoofTexture;
                roofMaterial.mainTextureScale = BuildingData.building_SO.rooftextureScale;
                roofMaterial.mainTextureOffset = BuildingData.building_SO.rooftextureOffset;
            }

            roofObject.AddComponent<MeshRenderer>().material = roofMaterial;
            roofObject.transform.position += Vector3.up * BuildingData.MaxHeight;
            roofObject.transform.SetParent(VE.GameObject.transform, false);
        }
        public Mesh GenerateGableRoofMesh(List<Vector3> footprint, float roofHeight)
        {
            // Calculate the lengths of all edges in the footprint
            List<float> edgeLengths = new List<float>();
            for (int i = 0; i < footprint.Count; i++)
            {
                int next = (i + 1) % footprint.Count;
                edgeLengths.Add(Vector3.Distance(footprint[i], footprint[next]));
            }

            // Find indices of the two longest edges (assuming opposite edges in a rectangle)
            var sortedEdgeIndices = edgeLengths
                .Select((length, index) => new { Index = index, Length = length })
                .OrderByDescending(x => x.Length)
                .Select(x => x.Index)
                .ToList();

            int longEdge1Index = sortedEdgeIndices[0];
            int longEdge2Index = (longEdge1Index + 2) % footprint.Count; // Opposite edge in a rectangle

            // Compute midpoints of the longest edges and elevate them
            Vector3 mid1 = (footprint[longEdge1Index] + footprint[(longEdge1Index + 1) % footprint.Count]) / 2f;
            Vector3 ridgeStart = mid1 + Vector3.up * roofHeight;

            Vector3 mid2 = (footprint[longEdge2Index] + footprint[(longEdge2Index + 1) % footprint.Count]) / 2f;
            Vector3 ridgeEnd = mid2 + Vector3.up * roofHeight;

            // Create vertices list including ridge points
            List<Vector3> vertices = new List<Vector3>(footprint);
            int ridgeStartIndex = vertices.Count;
            vertices.Add(ridgeStart);
            int ridgeEndIndex = vertices.Count;
            vertices.Add(ridgeEnd);

            List<int> triangles = new List<int>();

            for (int i = 0; i < footprint.Count; i++)
            {
                int current = i;
                int next = (i + 1) % footprint.Count;

                // Check if current edge is a long edge
                bool isLongEdge = (i == longEdge1Index) || (i == longEdge2Index);

                if (isLongEdge)
                {
                    // Connect to both ridge points
                    int thisRidgeIndex = (i == longEdge1Index) ? ridgeStartIndex : ridgeEndIndex;
                    int otherRidgeIndex = (thisRidgeIndex == ridgeStartIndex) ? ridgeEndIndex : ridgeStartIndex;

                    // Triangle from current edge to its ridge point
                    triangles.Add(current);
                    triangles.Add(next);
                    triangles.Add(thisRidgeIndex);

                    // Triangle connecting next vertex to both ridge points
                    triangles.Add(next);
                    triangles.Add(otherRidgeIndex);
                    triangles.Add(thisRidgeIndex);
                }
                else
                {
                    // Connect gable edge to the nearest ridge point
                    int adjacentLongEdge = -1;
                    for (int j = 0; j < footprint.Count; j++)
                    {
                        if (j == longEdge1Index || j == longEdge2Index)
                        {
                            int edgeNext = (j + 1) % footprint.Count;
                            if (footprint[j] == footprint[next] || footprint[edgeNext] == footprint[next])
                            {
                                adjacentLongEdge = j;
                                break;
                            }
                        }
                    }

                    if (adjacentLongEdge != -1)
                    {
                        int ridgePointIndex = (adjacentLongEdge == longEdge1Index) ? ridgeStartIndex : ridgeEndIndex;
                        triangles.Add(current);
                        triangles.Add(next);
                        triangles.Add(ridgePointIndex);
                    }
                }
            }

            // Build the mesh
            Mesh roofMesh = new Mesh();
            roofMesh.vertices = vertices.ToArray();
            roofMesh.triangles = triangles.ToArray();
            roofMesh.RecalculateNormals();

            // UV mapping
            float minX = vertices.Min(v => v.x);
            float maxX = vertices.Max(v => v.x);
            float minZ = vertices.Min(v => v.z);
            float maxZ = vertices.Max(v => v.z);

            Vector2[] uvs = new Vector2[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
            {
                float u = (vertices[i].x - minX) / (maxX - minX);
                float v = (vertices[i].z - minZ) / (maxZ - minZ);
                uvs[i] = new Vector2(u, v);
            }
            roofMesh.uv = uvs;

            return roofMesh;
        }
        public Vector3 GetCenter(IEnumerable<Vector3> points)
        {
            int count = 0;
            Vector3 sum = Vector3.zero;

            foreach (Vector3 point in points)
            {
                sum += new Vector3(point.x, point.y, point.z);

                count++;
            }

            if (count == 0)
            {
                Debug.LogError("Cannot compute center: No points provided.");
                return Vector3.zero;
            }

            return sum / count;
        }







        public void GenerateGableRoof(List<List<Vector3>> footprints, float roofHeight)
        {
            var roofMesh = GenerateGableRoofMesh(BuildingData.SpacePoints, BuildingData.building_SO.roofHeight);

            GameObject roofObject = new GameObject("GableRoof");
            roofObject.AddComponent<MeshFilter>().mesh = roofMesh;
            Material roofMaterial = Materials[0];
            if (BuildingData.building_SO.RoofTexture != null)
            {
                roofMaterial.mainTexture = BuildingData.building_SO.RoofTexture;
                roofMaterial.mainTextureScale = BuildingData.building_SO.rooftextureScale;
                roofMaterial.mainTextureOffset = BuildingData.building_SO.rooftextureOffset;
            }

            roofObject.AddComponent<MeshRenderer>().material = roofMaterial;
            roofObject.transform.position += Vector3.up * BuildingData.MaxHeight;
            roofObject.transform.SetParent(VE.GameObject.transform, false);
        }


        public static Mesh GenerateGableRoofMesh(List<List<Vector3>> footprint, float roofHeight)
        {
            if (footprint.Count == 0) return null;
            List<Vector3> outerContour = footprint[0];
            List<List<Vector3>> innerContours = footprint.Skip(1).ToList();

            List<Vector3> allVertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            Dictionary<Vector3, int> pointIndexMap = new Dictionary<Vector3, int>();

            // Add base vertices
            foreach (var v in outerContour)
            {
                if (!pointIndexMap.ContainsKey(v))
                {
                    pointIndexMap[v] = allVertices.Count;
                    allVertices.Add(v);
                }
            }
            foreach (var hole in innerContours)
            {
                foreach (var v in hole)
                {
                    if (!pointIndexMap.ContainsKey(v))
                    {
                        pointIndexMap[v] = allVertices.Count;
                        allVertices.Add(v);
                    }
                }
            }

            // Calculate ridge points and roof slopes
            Vector3 ridgePoint = CalculateRidgePoint(outerContour, roofHeight);
            int ridgeIndex = allVertices.Count;
            allVertices.Add(ridgePoint);

            // Triangulate the outer contour and holes manually
            List<int> baseTriangles = TriangulatePolygon(outerContour, innerContours, pointIndexMap);
            triangles.AddRange(baseTriangles);

            // Add triangles connecting to ridge
            for (int i = 0; i < outerContour.Count; i++)
            {
                int next = (i + 1) % outerContour.Count;
                triangles.Add(pointIndexMap[outerContour[i]]);
                triangles.Add(pointIndexMap[outerContour[next]]);
                triangles.Add(ridgeIndex);
            }

            Mesh roofMesh = new Mesh();
            roofMesh.vertices = allVertices.ToArray();
            roofMesh.triangles = triangles.ToArray();
            roofMesh.RecalculateNormals();
            ApplyUVMapping(roofMesh, allVertices);

            return roofMesh;
        }

        private static Vector3 CalculateRidgePoint(List<Vector3> section, float roofHeight)
        {
            Vector3 min = section.Aggregate((a, b) => a.x + a.z < b.x + b.z ? a : b);
            Vector3 max = section.Aggregate((a, b) => a.x + a.z > b.x + b.z ? a : b);
            return (min + max) / 2f + Vector3.up * roofHeight;
        }

        private static List<int> TriangulatePolygon(List<Vector3> outer, List<List<Vector3>> holes, Dictionary<Vector3, int> indexMap)
        {
            List<int> triangles = new List<int>();
            List<Vector3> allPoints = new List<Vector3>(outer);
            foreach (var hole in holes)
            {
                allPoints.AddRange(hole);
            }

            for (int i = 1; i < allPoints.Count - 1; i++)
            {
                triangles.Add(indexMap[allPoints[0]]);
                triangles.Add(indexMap[allPoints[i]]);
                triangles.Add(indexMap[allPoints[i + 1]]);
            }
            return triangles;
        }

        private static void ApplyUVMapping(Mesh mesh, List<Vector3> vertices)
        {
            Vector2[] uvs = new Vector2[vertices.Count];
            float minX = vertices.Min(v => v.x);
            float maxX = vertices.Max(v => v.x);
            float minZ = vertices.Min(v => v.z);
            float maxZ = vertices.Max(v => v.z);

            for (int i = 0; i < vertices.Count; i++)
            {
                float u = (vertices[i].x - minX) / (maxX - minX);
                float v = (vertices[i].z - minZ) / (maxZ - minZ);
                uvs[i] = new Vector2(u, v);
            }
            mesh.uv = uvs;
        }
    }
}
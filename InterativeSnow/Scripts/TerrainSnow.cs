using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TerrainSnow : MonoBehaviour
{
    //You can change max but be aware that unity meshes normally only supports 65k verts.
    //If you want to get bigger meshes, be sure to change the mesh index format as it's commented below
    [Range(4, 128), Header("The size of the sample taken from the terrain")]
    public int SampleSize = 32;

    [Range(0f, 5f), Header("The average snow offset")]
    public float SnowOffset = 1f;


    [Range(0f, 1f), Header("How strong the edge falloff is")]
    public float EdgeFalloff = 1f;
    [Range(2, 100), Header("How strong the edge falloff is, Note Increment by 2")]
    public int EdgeFalloffStrength = 10;

    [Range(-10f, 10f), Header("Edge height offset for the snow")]
    public float EdgeHeightOffset = 0f;

    //Not used as a child if you want
    [Header("Assign the RT camera that is used within the scene")]
    public GameObject RT_Camera;
    [Header("The object that this snow will track")]
    public GameObject Tracking;

    //Can be removed if you add a manual update for it
    Vector3 LastPos = Vector3.forward;

    float SnowBounds;

    // Update is called once per frame
    void Update()
    {
        //Only really used in the editor, can be removed or defined if wanted
        if (LastPos != transform.position)
        {
            LastPos = transform.position;
            GetComponent<MeshFilter>().mesh = CreateSample();

            if (RT_Camera != null)
                CenterRtCam();
            else
                Debug.LogError("RT Camera has not been assigned!");
        }

        Track();
    }

    private void OnValidate()
    {
        GetComponent<MeshFilter>().mesh = CreateSample();
        if (RT_Camera != null)
            CenterRtCam();
        else
            Debug.LogError("RT Camera has not been assigned!");
    }


    private Mesh CreateSample()
    {
        Mesh m = new Mesh();

        //Support for bigger meshes, without this unity will cry if you go above 256 in sample size
        //m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;


        var tr = Terrain.activeTerrain;
        Vector3 vs = tr.terrainData.heightmapScale;
        Vector3 terrainSize = tr.terrainData.size;

        //Set bounds, snow mesh should only be quadratic. Same with world terrain
        SnowBounds = SampleSize * vs.x;

        Vector3 ctPos = new Vector3(transform.position.x / vs.x, 0f, transform.position.z / vs.z);
        Vector3 tPosMin = tr.GetPosition();
        Vector3 tPosMax = tPosMin + terrainSize;

        Vector3 start = new Vector3(
            Mathf.Clamp(transform.position.x, tPosMin.x, tPosMax.x),
            0f,
            Mathf.Clamp(transform.position.z, tPosMin.z, tPosMax.z)) - tPosMin;

        Vector3 end = new Vector3(
            Mathf.Clamp(start.x + SampleSize, tPosMin.x, tPosMax.x),
            0f,
            Mathf.Clamp(start.z + SampleSize, tPosMin.z, tPosMax.z));

        if(start == end)
        {
            Debug.LogWarning("Snow object is not above terrain");
        }

        //Vector2Int sSize = new Vector2Int((int)(end.x - start.x), (int)(end.z - start.z));

        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Color> colors = new List<Color>();
        List<Vector2> uvs = new List<Vector2>();
        int[,] vId = new int[SampleSize, SampleSize];
        List<int> ids = new List<int>();

        int cx, cz;
        float dx, dz;
        Vector3 v, norm;
        Color vCol;
        float halfSize = SampleSize * .5f;

        for (int x = 0; x < SampleSize; ++x)
        {
            for (int z = 0; z < SampleSize; ++z)
            {

                //Calculate color based on edges

                float xr = Mathf.Pow((x - halfSize + .5f) / halfSize, EdgeFalloffStrength);
                float zr = Mathf.Pow((z - halfSize + .5f) / halfSize, EdgeFalloffStrength);

                vCol = new Color(EdgeFalloff * Mathf.Abs(xr * .5f + zr * .5f), 0f, 0f, 1f);
                colors.Add(vCol);


                //Calculate Vertex position based on terrain & object position, might be minor inconsistencies..
                vId[x, z] = vertices.Count;

                dx = (x + ctPos.x);
                dz = (z + ctPos.z);
                cx = (int)dx;
                cz = (int)dz;

                v = new Vector3(
                    ((x * vs.x) - (dx - cx) * vs.x),
                    tr.terrainData.GetHeight(cx, cz) - transform.position.y,
                    ((z * vs.z) - (dz - cz) * vs.z));

                //Lower edges to hide beneath terrain
                v.y = v.y + ((vCol.r * -1) * EdgeHeightOffset);

                norm = tr.terrainData.GetInterpolatedNormal((x + ctPos.x) / terrainSize.x * vs.x, (z + ctPos.z) / terrainSize.z * vs.z);

                vertices.Add(v + norm * SnowOffset);
                normals.Add(norm);

                uvs.Add(new Vector2(((float)x / SampleSize), ((float)z / SampleSize)));
            }
        }

        for (int x = 0; x < SampleSize - 1; ++x)
        {
            for (int z = 0; z < SampleSize - 1; ++z)
            {
                ids.Add(vId[x, z]);
                ids.Add(vId[x + 1, z + 1]);
                ids.Add(vId[x + 1, z]);

                ids.Add(vId[x + 1, z + 1]);
                ids.Add(vId[x, z]);
                ids.Add(vId[x, z + 1]);
            }
        }

        m.vertices = vertices.ToArray();
        m.triangles = ids.ToArray();
        m.uv = uvs.ToArray();
        m.normals = normals.ToArray();
        m.colors = colors.ToArray();
        return m;

    }

    private void CenterRtCam()
    {
        //Can be replaced with dynamic check
        float camheight = 400f;

        var tr = Terrain.activeTerrain;
        Vector3 vs = tr.terrainData.heightmapScale;

        Vector3 pos = transform.position;
        Vector3 mid = pos + (Vector3.one * SnowBounds * .5f) - Vector3.one;

        RT_Camera.transform.position = new Vector3(mid.x, camheight, mid.z);
        var rtCam = RT_Camera.GetComponent<Camera>();

        rtCam.orthographicSize = (SnowBounds * .5f);
    }


    private void Track()
    {
        if(Tracking == null)
        {
            Debug.LogError("No tracker found! This snow won't be used without the RT camera above");
            return;
        }

        Vector3 tPos = Tracking.transform.position;
        Vector3 snowPos = transform.position;

        if (tPos.x > snowPos.x && tPos.x < (snowPos.x + SnowBounds)
            && tPos.z > snowPos.z && tPos.z < (snowPos.z + SnowBounds))
        {
            CenterRtCam();
        }
    }

}

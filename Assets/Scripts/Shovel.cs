using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Shovel : MonoBehaviour
{
    Tilemap ground;
    bool doneAlready = false;

    private void Start()
    {
        ground = GameObject.FindGameObjectWithTag("Ground").GetComponent<Tilemap>();
    }

    void LookForTile(Vector3Int gridPos)
    {
        List<Vector3Int> tilesFound = new();

        for (int row = -1; row < 2; row++)
        {
            for (int column = -1; column < 2; column++)
            {
                Vector3Int pos = gridPos + new Vector3Int(row, column);

                if (ground.GetTile(pos))
                {
                    tilesFound.Add(pos);

                    break;
                }
            }
        }

        Vector3Int closestPos = new(-100, -100);
        float closestMagnitude = 100;
        foreach (Vector3Int tilePos in tilesFound)
        {
            float mag = (tilePos - gridPos).magnitude;

            if (mag < closestMagnitude)
            {
                closestPos = tilePos;
                closestMagnitude = mag;
            }
        }

        ground.SetTile(closestPos, null);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (doneAlready) return;

        Vector3Int tilePos = ground.WorldToCell(transform.position);

        LookForTile(tilePos);

        doneAlready = true;
    }
}

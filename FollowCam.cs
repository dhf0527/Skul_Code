using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCam : MonoBehaviour
{
    [SerializeField] BoxCollider2D mapBorder;
    [SerializeField] private Transform target;
    [SerializeField] Camera cam;
    float minX;
    float maxX;
    float minY;
    float maxY;
    // Start is called before the first frame update
    void Start()
    {

        Bounds b = mapBorder.bounds;
        minX = b.min.x + (cam.orthographicSize * 1920 / 1080);
        maxX = b.max.x - (cam.orthographicSize * 1920 / 1080);
        minY = b.min.y + cam.orthographicSize;
        maxY = b.max.y - cam.orthographicSize;
    }

    // Update is called once per frame
    void Update()
    {
        target = FindObjectOfType<Player>().transform;
        if (target == null)
        {
            target = GameObject.FindWithTag("Player").transform;
        }

        Vector3 pos = target.position;
        pos.z = -10;
        pos.y += 2;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        transform.position = Vector3.Lerp(transform.position, pos, 5f*Time.deltaTime);
    }

}

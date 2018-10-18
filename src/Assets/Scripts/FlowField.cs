using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlowField : MonoBehaviour
{
    public GameObject prefab;
    public float size = 1f;
    public int resolution = 20;
    public float scale = 1f;
    public Vector3 offset = Vector3.zero;
    public int ParticlesCount = 100;
    private float chunkSize;
    private Vector3[,,] field;
    private Particle[] particles;
    private float xOffset = 0;

    private void Awake()
    {
        chunkSize = size / resolution;
        field = new Vector3[resolution, resolution, resolution];
        UpdateFlowField();
    }
    void Start()
    {
		particles = new Particle[ParticlesCount];
		for (int i = 0; i < ParticlesCount; i++)
		{
            Transform particle = Instantiate(prefab, NewPosition(),
                    					Quaternion.identity).transform;
            particle.parent = transform;
       		particles[i] = new Particle(particle);
		}
    }

    void Update()
    {
		for (int i = 0; i < ParticlesCount; i++)
		{
			Particle particle = particles[i];
			Vector3 position = particle.transform.position;

			int x = Mathf.FloorToInt((position.x - (transform.position.x - size * 0.5f)) / size * resolution);
			int y = Mathf.FloorToInt((position.y - (transform.position.y - size * 0.5f)) / size * resolution);
			int z = Mathf.FloorToInt((position.z - (transform.position.z - size * 0.5f)) / size * resolution);
			particle.ApplyForce(field[x, y, z] * 0.01f);
            particle.Update();
			if(particle.transform.position.x <= (transform.position.x - size * 0.5f) || particle.transform.position.x >= (transform.position.x + size * 0.5f)
				|| particle.transform.position.y <= (transform.position.y - size * 0.5f) || particle.transform.position.y >= (transform.position.y + size * 0.5f)
				|| particle.transform.position.z <= (transform.position.z - size * 0.5f) || particle.transform.position.z >= (transform.position.z + size * 0.5f))
			{
				particle.transform.position = NewPosition();
			}
		}

        UpdateFlowField();
    }

    void OnDrawGizmos()
    {
        chunkSize = size / resolution;
        Vector3 chunkOffset = Vector3.one * size * 0.5f * (1f - 1f / resolution);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, Vector3.one * size);
        Gizmos.color = Color.yellow;
        if (resolution > 1 && field != null)
        {
            Vector3 startPos = transform.position - chunkOffset;
            Vector3 endPos = transform.position + chunkOffset;

            for (int z = 0; z < resolution; z++)
            {
                float zPos = Mathf.Lerp(startPos.z, endPos.z, (float)z / (resolution - 1));
                for (int y = 0; y < resolution; y++)
                {
                    float yPos = Mathf.Lerp(startPos.y, endPos.y, (float)y / (resolution - 1));
                    for (int x = 0; x < resolution; x++)
                    {
                        float xPos = Mathf.Lerp(startPos.x, endPos.x, (float)x / (resolution - 1));
                        Vector3 pos = new Vector3(xPos, yPos, zPos);
                        Gizmos.DrawLine(pos, field[x, y, z] * chunkSize * 0.5f + pos);
                        // Gizmos.DrawWireCube(new Vector3(xPos, yPos, zPos), Vector3.one * chunkSize);
                    }
                }
            }
        }
    }

    private void UpdateFlowField()
    {
        for (int x = 0; x < resolution; x++)
        {
            Vector3[] ups = new Vector3[resolution];
            for (int z = 0; z < resolution; z++)
            {
                float yAngle = Mathf.PerlinNoise((float)x / resolution * scale + offset.x + xOffset, (float)z / resolution * scale + offset.z) * Mathf.PI * 4f;
                ups[z] = Quaternion.AngleAxis(Mathf.Rad2Deg * yAngle, Vector3.up) * Vector3.right;
            }
            for (int y = 0; y < resolution; y++)
            {
                float zAngle = Mathf.PerlinNoise((float)x / resolution * scale + offset.x + xOffset, (float)y / resolution * scale + offset.y) * Mathf.PI * 4f;
                Vector3 forward = Quaternion.AngleAxis(Mathf.Rad2Deg * zAngle, Vector3.forward) * Vector3.right;

                for (int z = 0; z < resolution; z++)
                {
                    field[x, y, z] = (forward + ups[z]) * 0.5f;
                }
            }
        }

        xOffset += 0.01f;
    }

    private Vector3 NewPosition()
    {
        return new Vector3(Random.Range(transform.position.x - size * 0.5f, transform.position.x + size * 0.5f),
                            Random.Range(transform.position.y - size * 0.5f, transform.position.y + size * 0.5f),
                            Random.Range(transform.position.z - size * 0.5f, transform.position.z + size * 0.5f));
    }
}


public class Particle
{
    public Transform transform;
    Vector3 acceleration;
    Vector3 velocity;
    public Particle(Transform transform)
    {
        this.transform = transform;
        this.acceleration = Vector3.zero;
        this.velocity = Vector3.zero;
    }

    public void ApplyForce(Vector3 force)
    {
        this.acceleration = force;
        this.velocity += acceleration;
        this.velocity = Vector3.ClampMagnitude(this.velocity, 0.1f);
    }

    public void Update()
    {
        this.transform.position += velocity;
    }
}
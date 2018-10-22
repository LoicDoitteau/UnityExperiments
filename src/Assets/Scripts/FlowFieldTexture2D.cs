using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FlowFieldTexture2D : MonoBehaviour
{
	public int width = 512;
    public int height = 512;
    public int resolution = 20;
    public float scale = 1f;
    public Vector2 offset = Vector2.zero;
    public int ParticlesCount = 100;
    private Vector2[,] field;
    private Particle2D[] particles;
    private float xOffset = 0;
    private Texture2D texture;
    private Color[] colors;

    void Start()
    {
		texture = new Texture2D(width, height);
        colors = texture.GetPixels().Select(c => Color.black).ToArray();
        texture.SetPixels(colors);
        texture.Apply();
        gameObject.AddComponent<SpriteRenderer>().sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));

        field = new Vector2[resolution, resolution];
		particles = new Particle2D[ParticlesCount];
		for (int i = 0; i < ParticlesCount; i++)
		{
       		particles[i] = new Particle2D(NewPosition());
		}
        UpdateFlowField();
    }

    void Update()
    {
        for (int i = 0; i < ParticlesCount; i++)
		{
			Particle2D particle = particles[i];
			Vector3 position = particle.position;
            int x = Mathf.FloorToInt(position.x * (resolution - 1));
            int y = Mathf.FloorToInt(position.y * (resolution - 1));
            particle.ApplyForce(field[x, y] * 0.1f);
            particle.Update();
			if(particle.position.x <= 0 || particle.position.x >=  1f
				|| particle.position.y <= 0 || particle.position.y >= 1f)
			{
				particle.position = NewPosition();
			}
            x = Mathf.FloorToInt(particle.position.x * (width - 1));
            y = Mathf.FloorToInt(particle.position.y * (height - 1));
            texture.SetPixel(x, y, Color.white);    
        }
        texture.Apply();
        // UpdateFlowField();
    }

    void OnDrawGizmos()
    {
        Vector2 size = Vector2.one * new Vector2(width, height) / 100f;
        Vector2 chunkSize = size / resolution;
        Vector3 chunkOffset = size * 0.5f * (1f - 1f / resolution);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, size);
        Gizmos.color = Color.yellow;
        if (resolution > 1 && field != null)
        {
            Vector3 startPos = transform.position - chunkOffset;
            Vector3 endPos = transform.position + chunkOffset;
            for (int y = 0; y < resolution; y++)
            {
                float yPos = Mathf.Lerp(startPos.y, endPos.y, (float)y / (resolution - 1));
                for (int x = 0; x < resolution; x++)
                {
                    float xPos = Mathf.Lerp(startPos.x, endPos.x, (float)x / (resolution - 1));
                    Vector3 pos = new Vector3(xPos, yPos, transform.position.z);
                    Gizmos.DrawLine(pos, (Vector3)(field[x, y] * chunkSize) * 0.5f + pos);
                }
            }
        }
    }

	private void UpdateFlowField()
    {
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                float zAngle = Mathf.PerlinNoise((float)x / resolution * scale + offset.x + xOffset, (float)y / resolution * scale + offset.y) * Mathf.PI * 4f;
                Vector2 force = Quaternion.AngleAxis(Mathf.Rad2Deg * zAngle, Vector3.forward) * Vector2.right;
                field[x, y] = force;
            }
        }
        xOffset += 0.01f;
    }

    private Vector2 NewPosition()
    {
        return new Vector2(Random.value,
                            Random.value);
    }
}

public class Particle2D
{
    public Vector2 position;
    Vector2 acceleration;
    Vector2 velocity;
    public Particle2D(Vector2 position)
    {
        this.position = position;
        this.acceleration = Vector2.zero;
        this.velocity = Vector2.zero;
    }

    public void ApplyForce(Vector2 force)
    {
        this.acceleration = force;
        this.velocity += acceleration;
        this.velocity = Vector2.ClampMagnitude(this.velocity, 0.001f);
    }

    public void Update()
    {
        this.position += velocity;
    }
}
